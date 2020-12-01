using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 1)]
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

        DescriptorSet spSet;
        DescriptorSet irSet;
        DescriptorSet brdfLUTSet;

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
            "papermill.ktx", "gcanyon_cube.ktx", "pisa_cube.ktx", "uffizi_cube.ktx"
        };

        public override void Init()
        {
            base.Init();

            scene = new Scene
            {
                new Node("Camera", new vec3(120.0f, 0.0f, -50))
                {
                    new Camera
                    {
                        Fov = glm.radians(60)
                    },
                },
            };

            camera = scene.GetComponent<Camera>(true);
            camera.Node.LookAt(new vec3(0.0f, 5.0f, -50), TransformSpace.WORLD);

            envMap = Texture.Create(kEnvMapSize, kEnvMapSize, VkImageViewType.ImageCube, 6, VkFormat.R16G16B16A16SFloat, 0, VkImageUsageFlags.Storage | VkImageUsageFlags.TransferSrc);
            irMap = Texture.Create(kIrradianceMapSize, kIrradianceMapSize, VkImageViewType.ImageCube, 6, VkFormat.R16G16B16A16SFloat, 1, VkImageUsageFlags.Storage);
            brdfLUT = Texture.Create(kBRDF_LUT_Size, kBRDF_LUT_Size, VkImageViewType.Image2D, 1, VkFormat.R16G16SFloat, 1, VkImageUsageFlags.Storage);

            {
                var model = GeometryUtil.CreateCubeModel(10, 10, 10);
                var node = scene.CreateChild("Sky");
               
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);

                skyMaterial = new Material("Shaders/Skybox.shader");
                staticModel.SetMaterial(skyMaterial);
            }

            {
                var node = scene.CreateChild("Mesh");
                node.EulerAngles = glm.radians(0, 180, 0);
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.ModelFile = "models/cerberus/cerberus.fbx";

                KtxTextureReader texReader = new KtxTextureReader
                {
                    Format = VkFormat.R8G8B8A8UNorm,
                };

                var colorMap = texReader.Load("models/cerberus/albedo.ktx");// VkFormat.R8g8b8a8Srgb);
                var normalMap = texReader.Load("models/cerberus/normal.ktx");
                texReader.Format = VkFormat.R8UNorm;
                var metallicMap = texReader.Load("models/cerberus/metallic.ktx");
                texReader.Format = VkFormat.R8UNorm;
                var roughnessMap = texReader.Load("models/cerberus/roughness.ktx");
                //var aoMap = Texture.LoadFromFile("models/cerberus/ao.ktx", VkFormat.R8Unorm);

                var mat = new Material("Shaders/LitPbr.shader");
                mat.SetTexture("albedoMap", colorMap);
                mat.SetTexture("normalMap", normalMap);
                mat.SetTexture("metallicMap", metallicMap);
                mat.SetTexture("roughnessMap", roughnessMap);
                //mat.SetTexture("aoMap", aoMap);

                //AddDebugImage(colorMap, normalMap, metallicMap, roughnessMap);

                staticModel.SetMaterial(mat);

                pbrMaterial = mat;
            }

            //todo:
            pbrMaterial.PipelineResourceSet[0].ResourceSet[2].Bind(envMap, irMap, brdfLUT);

            computeSampler = new Sampler(VkFilter.Linear, VkSamplerMipmapMode.Linear, VkSamplerAddressMode.ClampToBorder, 1, false, VkBorderColor.FloatTransparentBlack);
            brdfLUTSampler = new Sampler(VkFilter.Linear, VkSamplerMipmapMode.Linear, VkSamplerAddressMode.ClampToEdge, 1, false);

            SetCubeMap(cubeMaps[0]);

//             FrameGraph.AddDebugImage(envMap);
//             FrameGraph.AddDebugImage(irMap);
//             FrameGraph.AddDebugImage(brdfLUT);

            MainView.Attach(camera, scene);

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
            KtxTextureReader texReader = new KtxTextureReader
            {
                Format = VkFormat.R16G16B16A16SFloat,
            };

            cubeMap = texReader.Load("textures/hdr/" + cubemap);
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
                var specializationInfo = new SpecializationInfo(new VkSpecializationMapEntry(0, 0, sizeof(uint)));
                specializationInfo.Write(0, numMipTailLevels);
                pass.ComputeShader.SpecializationInfo = specializationInfo;
                DescriptorSetLayout resLayout = pass.GetResourceLayout(0);

                spSet = new DescriptorSet(resLayout);

                CommandBuffer commandBuffer = Graphics.BeginPrimaryCmd();

                // Copy base mipmap level into destination environment map.
                {

                    Span<VkImageMemoryBarrier> preCopyBarriers = stackalloc[]
                    {
                        new VkImageMemoryBarrier(cubeMap.image, 0, VkAccessFlags.TransferRead, VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.TransferSrcOptimal, VkImageAspectFlags.Color, 0, 1),
                        new VkImageMemoryBarrier(envMap.image, 0, VkAccessFlags.TransferWrite, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal),
                    };

                    Span<VkImageMemoryBarrier> postCopyBarriers = stackalloc[]
                    {
                        new VkImageMemoryBarrier(cubeMap.image, VkAccessFlags.TransferRead, VkAccessFlags.ShaderRead, VkImageLayout.TransferSrcOptimal, VkImageLayout.ShaderReadOnlyOptimal, VkImageAspectFlags.Color, 0, 1),
                        new VkImageMemoryBarrier(envMap.image, VkAccessFlags.TransferWrite, VkAccessFlags.ShaderWrite, VkImageLayout.TransferDstOptimal, VkImageLayout.General),
                    };

                    commandBuffer.PipelineBarrier(VkPipelineStageFlags.TopOfPipe, VkPipelineStageFlags.Transfer, preCopyBarriers);

                    VkImageCopy copyRegion = new VkImageCopy
                    {
                        extent = new VkExtent3D(envMap.width, envMap.height, 1)
                    };

                    copyRegion.srcSubresource.aspectMask = VkImageAspectFlags.Color;
                    copyRegion.srcSubresource.layerCount = envMap.layers;
                    copyRegion.dstSubresource = copyRegion.srcSubresource;

                    commandBuffer.CopyImage(cubeMap.image, VkImageLayout.TransferSrcOptimal, envMap.image, VkImageLayout.TransferDstOptimal, ref copyRegion);
                    commandBuffer.PipelineBarrier(VkPipelineStageFlags.Transfer, VkPipelineStageFlags.ComputeShader, postCopyBarriers);
                 
                    // Pre-filter rest of the mip-chain.
                    List<ImageView> envTextureMipTailViews = new List<ImageView>();
                     
                    var inputTexture = new VkDescriptorImageInfo(computeSampler, cubeMap.imageView, VkImageLayout.ShaderReadOnlyOptimal);
                    spSet.Bind(0, ref inputTexture);

                    Span<VkDescriptorImageInfo> envTextureMipTailDescriptors = stackalloc VkDescriptorImageInfo[(int)numMipTailLevels];
                    for (uint level = 0; level < numMipTailLevels; ++level)
                    {
                        var view = ImageView.Create(envMap.image, VkImageViewType.ImageCube, VkFormat.R16G16B16A16SFloat, VkImageAspectFlags.Color, level + 1, 1, 0, envMap.image.arrayLayers);
                        envTextureMipTailViews.Add(view);
                        envTextureMipTailDescriptors[(int)level] = new VkDescriptorImageInfo(VkSampler.Null, view, VkImageLayout.General);
                    }

                    spSet.Bind(1, envTextureMipTailDescriptors);
                    spSet.UpdateSets();

                    commandBuffer.BindComputePipeline(pass);
                    commandBuffer.BindComputeResourceSet(pass.PipelineLayout, 0, spSet);

                    float deltaRoughness = 1.0f / Math.Max((float)numMipTailLevels, 1.0f);
                    for (uint level = 1, size = kEnvMapSize / 2; level < kEnvMapLevels; ++level, size /= 2)
                    {
                        uint numGroups = Math.Max(1, size / 32);

                        var pushConstants = new SpecularFilterPushConstants { level = level - 1, roughness = level * deltaRoughness };
                        commandBuffer.PushConstants(pass.PipelineLayout, VkShaderStageFlags.Compute, 0, ref pushConstants);
                        commandBuffer.Dispatch(numGroups, numGroups, 6);
                    }
                  
                    var barrier = new VkImageMemoryBarrier(envMap.image, VkAccessFlags.ShaderWrite, 0, VkImageLayout.General, VkImageLayout.ShaderReadOnlyOptimal);
                    commandBuffer.PipelineBarrier(VkPipelineStageFlags.ComputeShader, VkPipelineStageFlags.BottomOfPipe, ref barrier);
                    
                }

                Graphics.EndPrimaryCmd(commandBuffer);
            }

            // Compute diffuse irradiance cubemap
            {
                Pass pass = shader.GetPass("IrMap");
                DescriptorSetLayout resLayout = pass.GetResourceLayout(0);
                irSet = new DescriptorSet(resLayout, cubeMap, irMap);

                CommandBuffer commandBuffer = Graphics.BeginPrimaryCmd();
                {
                    Span<VkImageMemoryBarrier> barriers = stackalloc [] { new VkImageMemoryBarrier(irMap.image, 0, VkAccessFlags.ShaderWrite, VkImageLayout.Undefined, VkImageLayout.General) };
                    commandBuffer.PipelineBarrier(VkPipelineStageFlags.TopOfPipe, VkPipelineStageFlags.ComputeShader, barriers);

                    commandBuffer.BindComputePipeline(pass);
                    commandBuffer.BindComputeResourceSet(pass.PipelineLayout, 0, irSet);
                    commandBuffer.Dispatch(kIrradianceMapSize / 32, kIrradianceMapSize / 32, 6);

                    Span<VkImageMemoryBarrier> postDispatchBarrier = stackalloc [] { new VkImageMemoryBarrier(irMap.image, VkAccessFlags.ShaderWrite, 0, VkImageLayout.General, VkImageLayout.ShaderReadOnlyOptimal) };
                    commandBuffer.PipelineBarrier(VkPipelineStageFlags.ComputeShader, VkPipelineStageFlags.BottomOfPipe, postDispatchBarrier);
                }

                Graphics.EndPrimaryCmd(commandBuffer);
            }

            // Compute Cook-Torrance BRDF 2D LUT for split-sum approximation.
            {
                var pass = shader.GetPass("BrdfLUT");
                DescriptorSetLayout resLayout = pass.GetResourceLayout(0);
                brdfLUTSet = new DescriptorSet(resLayout, brdfLUT);

                CommandBuffer commandBuffer = Graphics.BeginPrimaryCmd();
                {
                    Span<VkImageMemoryBarrier> barriers = stackalloc [] { new VkImageMemoryBarrier(brdfLUT.image, 0, VkAccessFlags.ShaderWrite, VkImageLayout.Undefined, VkImageLayout.General)};
                    commandBuffer.PipelineBarrier(VkPipelineStageFlags.TopOfPipe, VkPipelineStageFlags.ComputeShader, barriers);

                    commandBuffer.BindComputePipeline(pass);
                    commandBuffer.BindComputeResourceSet(pass.PipelineLayout, 0, brdfLUTSet);
                    commandBuffer.Dispatch(kBRDF_LUT_Size / 32, kBRDF_LUT_Size / 32, 6);

                    Span<VkImageMemoryBarrier> postDispatchBarrier = stackalloc [] { new VkImageMemoryBarrier(brdfLUT.image, VkAccessFlags.ShaderWrite, 0, VkImageLayout.General, VkImageLayout.ShaderReadOnlyOptimal) };
                    commandBuffer.PipelineBarrier(VkPipelineStageFlags.ComputeShader, VkPipelineStageFlags.BottomOfPipe, postDispatchBarrier);
                }

                Graphics.EndPrimaryCmd(commandBuffer);

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

    }

}
