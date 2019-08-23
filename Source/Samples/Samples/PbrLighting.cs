using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 9)]
    public class PbrLighting : Sample
    {
        const int kEnvMapSize = 1024;
        const int kIrradianceMapSize = 32;
        const int kBRDF_LUT_Size = 256;

        int kEnvMapLevels = NumMipmapLevels(kEnvMapSize, kEnvMapSize);

        Texture cubeMap;

        Texture brdfLUT;
        Texture envMap;
        Texture irMap;

        ResourceSet spSet;
        ResourceSet irSet;
        ResourceSet brdfLUTSet;

        Material skyMaterial;
        Material pbrMaterial;
        struct SpecularFilterPushConstants
        {
            public uint level;
            public float roughness;
        };

        Sampler computeSampler;
        Sampler brdfLUTSampler;

        string[] cubeMaps =
        {
            "gcanyon_cube.ktx", "papermill.ktx", "pisa_cube.ktx", "uffizi_cube.ktx"
        };

        public override void Init()
        {
            base.Init();

            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(80.0f, 0.0f, -150);
            cameraNode.Rotate(Quaternion.FromEuler(0, MathUtil.Radians(-45), 0), TransformSpace.LOCAL);
            camera = cameraNode.CreateComponent<Camera>();
            camera.Fov = MathUtil.Radians(60);

            envMap = Texture.Create(kEnvMapSize, kEnvMapSize, 6, Format.R16g16b16a16Sfloat, 0, ImageUsageFlags.Storage);
            irMap = Texture.Create(kIrradianceMapSize, kIrradianceMapSize, 6, Format.R16g16b16a16Sfloat, 1, ImageUsageFlags.Storage);
            brdfLUT = Texture.Create(kBRDF_LUT_Size, kBRDF_LUT_Size, 1, Format.R16g16Sfloat, 1, ImageUsageFlags.Storage);


            {
                var model = GeometricPrimitive.CreateCubeModel(10, 10, 10);
                var node = scene.CreateChild("Sky");
               
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);

                skyMaterial = new Material("Shaders/Skybox.shader");
                staticModel.SetMaterial(skyMaterial);
            }

            {
                var node = scene.CreateChild("Mesh");
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel("models/cerberus/cerberus.fbx");

                var colorMap = Texture.LoadFromFile("models/cerberus/albedo.ktx", Format.R8g8b8a8Unorm);// Format.R8g8b8a8Srgb);
                var normalMap = Texture.LoadFromFile("models/cerberus/normal.ktx", Format.R8g8b8a8Unorm);
                var metallicMap = Texture.LoadFromFile("models/cerberus/metallic.ktx", Format.R8Unorm);
                var roughnessMap = Texture.LoadFromFile("models/cerberus/roughness.ktx", Format.R8Unorm);
                //var aoMap = Texture.LoadFromFile("models/cerberus/ao.ktx", Format.R8Unorm);

                var mat = new Material("Shaders/LitPbr.shader");
                mat.SetTexture("albedoMap", colorMap);
                mat.SetTexture("normalMap", normalMap);
                mat.SetTexture("metallicMap", metallicMap);
                mat.SetTexture("roughnessMap", roughnessMap);
                //mat.SetTexture("aoMap", aoMap);

                staticModel.SetMaterial(mat);

                pbrMaterial = mat;
            }

            pbrMaterial.ResourceSet[2]
                .Bind(0, envMap)
                .Bind(1, irMap)
                .Bind(2, brdfLUT).UpdateSets();

            computeSampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.ClampToBorder, false, BorderColor.FloatTransparentBlack);
            brdfLUTSampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.ClampToEdge, false);

            SetCubeMap(cubeMaps[0]);

            Renderer.MainView.Attach(camera, scene);

            //(this).Subscribe((GUIEvent e) => OnDebugGUI());
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

        void SetCubeMap(string cubemap)
        {
            cubeMap = Texture.LoadFromFile("textures/hdr/" + cubemap, Format.R16g16b16a16Sfloat);
            skyMaterial.SetTexture("EnvMap", cubeMap);
            Preprocess();
        }

        void Preprocess()
        {
            Shader shader = Resources.Load<Shader>("shaders/brdf.shader");

            {
                Pass pass = shader.GetPass("SpMap");

                uint numMipTailLevels = (uint)kEnvMapLevels - 1;

                // Compute pre-filtered specular environment map.            
                var specializationInfo = new SpecializationInfo(new SpecializationMapEntry(0, 0, sizeof(uint)));
                specializationInfo.Write(0, numMipTailLevels);
                pass.ComputeShader.SpecializationInfo = specializationInfo;
                ResourceLayout resLayout = pass.GetResourceLayout(0);

                spSet = new ResourceSet(resLayout);

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
                     
                    var inputTexture = new DescriptorImageInfo(computeSampler, cubeMap.imageView, ImageLayout.ShaderReadOnlyOptimal);
                    spSet.Bind(0, ref inputTexture);

                    Span<DescriptorImageInfo> envTextureMipTailDescriptors = stackalloc DescriptorImageInfo[(int)numMipTailLevels];
                    for (uint level = 0; level < numMipTailLevels; ++level)
                    {
                        var view = ImageView.Create(envMap, Format.R16g16b16a16Sfloat, ImageAspectFlags.Color, level + 1, 1);
                        envTextureMipTailViews.Add(view);
                        envTextureMipTailDescriptors[(int)level] = new DescriptorImageInfo(null, view, ImageLayout.General);
                    }

                    spSet.Bind(1, envTextureMipTailDescriptors);
                    spSet.UpdateSets();

                    commandBuffer.BindComputePipeline(pass);
                    commandBuffer.BindComputeResourceSet(pass.PipelineLayout, 0, spSet, null);

                    float deltaRoughness = 1.0f / Math.Max((float)numMipTailLevels, 1.0f);
                    for (uint level = 1, size = kEnvMapSize / 2; level < kEnvMapLevels; ++level, size /= 2)
                    {
                        uint numGroups = Math.Max(1, size / 32);

                        var pushConstants = new SpecularFilterPushConstants { level = level - 1, roughness = level * deltaRoughness };
                        commandBuffer.PushConstants(pass.PipelineLayout, ShaderStage.Compute, 0, ref pushConstants);
                        commandBuffer.Dispatch(numGroups, numGroups, 6);
                    }
                  
                    var barrier = new ImageMemoryBarrier(envMap, AccessFlags.ShaderWrite, 0, ImageLayout.General, ImageLayout.ShaderReadOnlyOptimal);
                    commandBuffer.PipelineBarrier(PipelineStageFlags.ComputeShader, PipelineStageFlags.BottomOfPipe, ref barrier);

                }

                Graphics.EndWorkCommandBuffer(commandBuffer);
            }

            // Compute diffuse irradiance cubemap
            {
                Pass pass = shader.GetPass("IrMap");
                ResourceLayout resLayout = pass.GetResourceLayout(0);
                irSet = new ResourceSet(resLayout, cubeMap, irMap);

                CommandBuffer commandBuffer = Graphics.BeginWorkCommandBuffer();
                {
                    Span<ImageMemoryBarrier> barriers = stackalloc [] { new ImageMemoryBarrier(irMap, 0, AccessFlags.ShaderWrite, ImageLayout.Undefined, ImageLayout.General) };
                    commandBuffer.PipelineBarrier(PipelineStageFlags.TopOfPipe, PipelineStageFlags.ComputeShader, barriers);

                    commandBuffer.BindComputePipeline(pass);
                    commandBuffer.BindComputeResourceSet(pass.PipelineLayout, 0, irSet, null);
                    commandBuffer.Dispatch(kIrradianceMapSize / 32, kIrradianceMapSize / 32, 6);

                    Span<ImageMemoryBarrier> postDispatchBarrier = stackalloc [] { new ImageMemoryBarrier(irMap, AccessFlags.ShaderWrite, 0, ImageLayout.General, ImageLayout.ShaderReadOnlyOptimal) };
                    commandBuffer.PipelineBarrier(PipelineStageFlags.ComputeShader, PipelineStageFlags.BottomOfPipe, postDispatchBarrier);
                }

                Graphics.EndWorkCommandBuffer(commandBuffer);
            }

            // Compute Cook-Torrance BRDF 2D LUT for split-sum approximation.
            {
                var pass = shader.GetPass("BrdfLUT");
                ResourceLayout resLayout = pass.GetResourceLayout(0);
                brdfLUTSet = new ResourceSet(resLayout, brdfLUT);

                CommandBuffer commandBuffer = Graphics.BeginWorkCommandBuffer();
                {
                    Span<ImageMemoryBarrier> barriers = stackalloc [] { new ImageMemoryBarrier(brdfLUT, 0, AccessFlags.ShaderWrite, ImageLayout.Undefined, ImageLayout.General)};
                    commandBuffer.PipelineBarrier(PipelineStageFlags.TopOfPipe, PipelineStageFlags.ComputeShader, barriers);

                    commandBuffer.BindComputePipeline(pass);
                    commandBuffer.BindComputeResourceSet(pass.PipelineLayout, 0, brdfLUTSet, null);
                    commandBuffer.Dispatch(kBRDF_LUT_Size / 32, kBRDF_LUT_Size / 32, 6);

                    Span<ImageMemoryBarrier> postDispatchBarrier = stackalloc [] { new ImageMemoryBarrier(brdfLUT, AccessFlags.ShaderWrite, 0, ImageLayout.General, ImageLayout.ShaderReadOnlyOptimal) };
                    commandBuffer.PipelineBarrier(PipelineStageFlags.ComputeShader, PipelineStageFlags.BottomOfPipe, postDispatchBarrier);
                }

                Graphics.EndWorkCommandBuffer(commandBuffer);

            }
        }

        int selected = 0;
        public override void OnGUI()
        {
            base.OnGUI();

            if (ImGui.Begin("HUD"))
            {
                ImGui.Text("Selected cube map:");
                if (ImGui.Combo("CubeMap", ref selected, cubeMaps, cubeMaps.Length))
                {
                    SetCubeMap(cubeMaps[selected]);
                }

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
            }

            ImGui.End();

        }



    }

}
