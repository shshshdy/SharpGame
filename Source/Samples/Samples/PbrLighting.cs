using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = -9)]
    public class PbrLighting : Sample
    {
        const int kEnvMapSize = 1024;
        const int kIrradianceMapSize = 32;
        const int kBRDF_LUT_Size = 256;
        const ulong kUniformBufferSize = 64 * 1024;

        int kEnvMapLevels = NumMipmapLevels(kEnvMapSize, kEnvMapSize);

        Texture cubeMap;
        Texture brdfLUT;
        Texture envMap;
        Texture irMap;

        ResourceSet brdfResSet;
        ResourceSet irResSet;
        ResourceSet spResSet;

        Material pbrMaterial;
        struct SpecularFilterPushConstants
        {
            public uint level;
            public float roughness;
        };

        Sampler computeSampler;
        Sampler brdfSampler;

        public override void Init()
        {
            base.Init();

            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(80.0f, 0.0f, -150);
            cameraNode.Rotate(Quaternion.FromEuler(0, MathUtil.DegreesToRadians(-45), 0), TransformSpace.LOCAL);
            camera = cameraNode.CreateComponent<Camera>();
            camera.Fov = MathUtil.DegreesToRadians(60);
            camera.AspectRatio = (float)Graphics.Width / Graphics.Height;

            envMap = Texture.Create(kEnvMapSize, kEnvMapSize, 6, Format.R16g16b16a16Sfloat, 0, ImageUsageFlags.Storage);
            irMap = Texture.Create(kIrradianceMapSize, kIrradianceMapSize, 6, Format.R16g16b16a16Sfloat, 1, ImageUsageFlags.Storage);
            brdfLUT = Texture.Create(kBRDF_LUT_Size, kBRDF_LUT_Size, 1, Format.R16g16Sfloat, 1, ImageUsageFlags.Storage);

            cubeMap = Texture.LoadFromFile("textures/hdr/gcanyon_cube.ktx", Format.R16g16b16a16Sfloat);
            {
                var model = GeometricPrimitive.CreateCubeModel(10, 10, 10);// Resources.Load<Model>("Models/skybox.obj");
                var node = scene.CreateChild("Sky");
                node.Scaling = new Vector3(30.0f);
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);

                var mat = new Material("Shaders/Skybox.shader");
                mat.SetTexture("samplerCubeMap", envMap);

                staticModel.SetMaterial(mat);
            }

            {
                var node = scene.CreateChild("Mesh");
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel("models/cerberus/cerberus.fbx");

                var colorMap = Texture.LoadFromFile("models/cerberus/albedo.ktx", Format.R8g8b8a8Srgb);
                var normalMap = Texture.LoadFromFile("models/cerberus/normal.ktx", Format.R8g8b8a8Unorm);
                var metallicMap = Texture.LoadFromFile("models/cerberus/metallic.ktx", Format.R8Unorm);
                var roughnessMap = Texture.LoadFromFile("models/cerberus/roughness.ktx", Format.R8Unorm);
                var aoMap = Texture.LoadFromFile("models/cerberus/ao.ktx", Format.R8Unorm);

                var mat = new Material("Shaders/LitPbr.shader");
                mat.SetTexture("albedoMap", colorMap);
                mat.SetTexture("normalMap", normalMap);
                mat.SetTexture("metallicMap", metallicMap);
                mat.SetTexture("roughnessMap", roughnessMap);
                mat.SetTexture("aoMap", aoMap);

                staticModel.SetMaterial(mat);

                pbrMaterial = mat;
            }

            pbrMaterial.ResourceSet[2]
                .Bind(0, envMap)
                .Bind(1, irMap)
                .Bind(2, brdfLUT).UpdateSets();

            computeSampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.ClampToBorder, true, BorderColor.FloatTransparentBlack);
            brdfSampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.ClampToEdge, false);
            Preprocess();

            Renderer.MainView.Attach(camera, scene);

            (this).Subscribe((GUIEvent e) => OnDebugGUI());
        }

        static int NumMipmapLevels(int width, int height)
        {
           int levels = 1;
            while (((width | height) >> levels) != 0)
            {
                ++levels;
            }
            return levels;
        }

        void Preprocess()
        {
            {
                Shader shader = Resources.Load<Shader>("shaders/spmap.shader");
                Pass pass = shader.Main;
                uint numMipTailLevels = (uint)kEnvMapLevels - 1;

                // Compute pre-filtered specular environment map.
                {
                  
                    var specializationInfo = new SpecializationInfo(new SpecializationMapEntry(0, 0, sizeof(uint)));
                    specializationInfo.Write(0, numMipTailLevels);
                    pass.ComputeShader.SpecializationInfo = specializationInfo;
                    ResourceLayout resLayout = pass.GetResourceLayout(0);
                    spResSet = new ResourceSet(resLayout);//, cubeMap, envMap);
                }

                CommandBuffer commandBuffer = Graphics.BeginWorkCommandBuffer();

                // Copy base mipmap level into destination environment map.
                {

                    Span<ImageMemoryBarrier> preCopyBarriers = stackalloc[]
                    {
                        new ImageMemoryBarrier(cubeMap, 0, AccessFlags.TransferRead, ImageLayout.ShaderReadOnlyOptimal, ImageLayout.TransferSrcOptimal, ImageAspectFlags.Color, 0, 1),
                        new ImageMemoryBarrier(envMap, 0, AccessFlags.TransferWrite, ImageLayout.Undefined, ImageLayout.TransferDstOptimal),
                    };

                    Span<ImageMemoryBarrier> postCopyBarriers = stackalloc[]
                    {
                        new ImageMemoryBarrier(cubeMap, AccessFlags.TransferRead, AccessFlags.ShaderRead, ImageLayout.TransferSrcOptimal, ImageLayout.ShaderReadOnlyOptimal, ImageAspectFlags.Color, 0, 1),
                        new ImageMemoryBarrier(envMap, AccessFlags.TransferWrite, AccessFlags.ShaderWrite, ImageLayout.TransferDstOptimal, ImageLayout.General),
                    };

                    commandBuffer.PipelineBarrier(PipelineStageFlags.TopOfPipe, PipelineStageFlags.Transfer, preCopyBarriers);

                    ImageCopy copyRegion = new ImageCopy
                    {
                        extent = new Extent3D { width = envMap.width, height = envMap.height, depth = 1 }
                    };

                    copyRegion.srcSubresource.aspectMask = ImageAspectFlags.Color;
                    copyRegion.srcSubresource.layerCount = envMap.layers;
                    copyRegion.dstSubresource = copyRegion.srcSubresource;

                    commandBuffer.CopyImage(cubeMap.image, ImageLayout.TransferSrcOptimal, envMap.image, ImageLayout.TransferDstOptimal, ref copyRegion);
                    commandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, PipelineStageFlags.ComputeShader, postCopyBarriers);

                    // Pre-filter rest of the mip-chain.
                    List<ImageView> envTextureMipTailViews = new List<ImageView>();
                     
                    DescriptorImageInfo inputTexture = new DescriptorImageInfo
                    (
                        computeSampler,
                        cubeMap.imageView,
                        ImageLayout.ShaderReadOnlyOptimal
                    );

                    Span<DescriptorImageInfo> info = stackalloc[] { inputTexture };
                    spResSet.Bind(0, info);

                    Span<DescriptorImageInfo> envTextureMipTailDescriptors = stackalloc DescriptorImageInfo[(int)numMipTailLevels];
                    for (uint level = 0; level < numMipTailLevels; ++level)
                    {
                        var view = ImageView.Create(envMap, Format.R16g16b16a16Sfloat, ImageAspectFlags.Color, level, 1);
                        envTextureMipTailViews.Add(view);
                        envTextureMipTailDescriptors[(int)level] = new DescriptorImageInfo(null, view, ImageLayout.General);
                    }
                    spResSet.Bind(1, envTextureMipTailDescriptors);
                    spResSet.UpdateSets();

                    commandBuffer.BindComputePipeline(pass);
                    commandBuffer.BindComputeResourceSet(pass.PipelineLayout, 0, spResSet, null);

                    float deltaRoughness = 1.0f / Math.Max((float)numMipTailLevels, 1.0f);
                    for (uint level = 1, size = kEnvMapSize / 2; level < kEnvMapLevels; ++level, size /= 2)
                    {
                        uint numGroups = Math.Max(1, size / 32);

                        var pushConstants = new SpecularFilterPushConstants { level = level - 1, roughness = level * deltaRoughness };
                        commandBuffer.PushConstants(pass.PipelineLayout, ShaderStage.Compute, 0, ref pushConstants);
                        commandBuffer.Dispatch(numGroups, numGroups, 6);
                    }
                  
                    Span<ImageMemoryBarrier> barrier = stackalloc[] { new ImageMemoryBarrier(envMap, AccessFlags.ShaderWrite, 0, ImageLayout.General, ImageLayout.ShaderReadOnlyOptimal) };
                    commandBuffer.PipelineBarrier(PipelineStageFlags.ComputeShader, PipelineStageFlags.BottomOfPipe, barrier);

                }

                Graphics.EndWorkCommandBuffer(commandBuffer);
            }

            // Compute diffuse irradiance cubemap
            {
                Shader shader = Resources.Load<Shader>("shaders/irmap.shader");
                ResourceLayout resLayout = shader.Main.GetResourceLayout(0);
                irResSet = new ResourceSet(resLayout, cubeMap, irMap);

                CommandBuffer commandBuffer = Graphics.BeginWorkCommandBuffer();
                {
                    Span<ImageMemoryBarrier> barriers = stackalloc [] { new ImageMemoryBarrier(irMap, 0, AccessFlags.ShaderWrite, ImageLayout.Undefined, ImageLayout.General) };
                    commandBuffer.PipelineBarrier(PipelineStageFlags.TopOfPipe, PipelineStageFlags.ComputeShader, barriers);

                    commandBuffer.BindComputePipeline(shader.Main);
                    commandBuffer.BindComputeResourceSet(shader.Main.PipelineLayout, 0, irResSet, null);
                    commandBuffer.Dispatch(kIrradianceMapSize / 32, kIrradianceMapSize / 32, 6);

                    Span<ImageMemoryBarrier> postDispatchBarrier = stackalloc [] { new ImageMemoryBarrier(irMap, AccessFlags.ShaderWrite, 0, ImageLayout.General, ImageLayout.ShaderReadOnlyOptimal) };
                    commandBuffer.PipelineBarrier(PipelineStageFlags.ComputeShader, PipelineStageFlags.BottomOfPipe, postDispatchBarrier);
                }

                Graphics.EndWorkCommandBuffer(commandBuffer);
            }

            // Compute Cook-Torrance BRDF 2D LUT for split-sum approximation.
            {
                Shader shader = Resources.Load<Shader>("shaders/brdf.shader");

                ResourceLayout resLayout = shader.Main.GetResourceLayout(0);
                brdfResSet = new ResourceSet(resLayout, brdfLUT);

                CommandBuffer commandBuffer = Graphics.BeginWorkCommandBuffer();
                {
                    Span<ImageMemoryBarrier> barriers = stackalloc [] { new ImageMemoryBarrier(brdfLUT, 0, AccessFlags.ShaderWrite, ImageLayout.Undefined, ImageLayout.General)};
                    commandBuffer.PipelineBarrier(PipelineStageFlags.TopOfPipe, PipelineStageFlags.ComputeShader, barriers);

                    commandBuffer.BindComputePipeline(shader.Main);
                    commandBuffer.BindComputeResourceSet(shader.Main.PipelineLayout, 0, brdfResSet, null);
                    commandBuffer.Dispatch(kBRDF_LUT_Size / 32, kBRDF_LUT_Size / 32, 6);

                    Span<ImageMemoryBarrier> postDispatchBarrier = stackalloc [] { new ImageMemoryBarrier(brdfLUT, AccessFlags.ShaderWrite, 0, ImageLayout.General, ImageLayout.ShaderReadOnlyOptimal) };
                    commandBuffer.PipelineBarrier(PipelineStageFlags.ComputeShader, PipelineStageFlags.BottomOfPipe, postDispatchBarrier);
                }

                Graphics.EndWorkCommandBuffer(commandBuffer);

            }
        }

        bool debugOpen = true;

        void OnDebugGUI()
        {

            var io = ImGui.GetIO();
            {
                Vector2 window_pos = new Vector2(10, io.DisplaySize.Y - 10);
                Vector2 window_pos_pivot = new Vector2(0.0f, 1.0f);
                ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
                ImGui.SetNextWindowBgAlpha(0.5f); // Transparent background
            }

            if (ImGui.Begin("Debugger", ref debugOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
            {
                ImGUI.Image(brdfLUT, new Vector2(200, 200));
                //ImGUI.Image(envMap, new Vector2(200, 200));
                //ImGUI.Image(irMap, new Vector2(200, 200));
            }

            ImGui.End();


        }



    }

}
