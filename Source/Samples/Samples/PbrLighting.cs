using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

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

        Texture colorMap;
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

            cubeMap = Texture.LoadFromFile("textures/hdr/gcanyon_cube.ktx", Format.R16g16b16a16Sfloat);
            {
                var model = Resources.Load<Model>("Models/cube.obj");
                var node = scene.CreateChild("Sky");
                node.Scaling = new Vector3(30.0f);
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);

                var mat = new Material("Shaders/Skybox.shader");
                mat.SetTexture("samplerCubeMap", cubeMap);

                staticModel.SetMaterial(mat);
            }

            {
                var node = scene.CreateChild("Mesh");

                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel("models/cerberus/cerberus.fbx");

                colorMap = Texture.LoadFromFile("models/cerberus/albedo.ktx", Format.R8g8b8a8Unorm);
                var normalMap = Texture.LoadFromFile("models/cerberus/normal.ktx", Format.R8g8b8a8Unorm);
                var metallicMap = Texture.LoadFromFile("models/cerberus/metallic.ktx", Format.R8Unorm);
                var roughnessMap = Texture.LoadFromFile("models/cerberus/roughness.ktx", Format.R8Unorm);
                var aoMap = Texture.LoadFromFile("models/cerberus/ao.ktx", Format.R8Unorm);

                var mat = new Material("Shaders/Pbr.shader");
                mat.SetTexture("albedoMap", colorMap);
                mat.SetTexture("normalMap", normalMap);
                mat.SetTexture("metallicMap", metallicMap);
                mat.SetTexture("roughnessMap", roughnessMap);
                mat.SetTexture("aoMap", aoMap);

                staticModel.SetMaterial(mat);
            }

            envMap = Texture.Create(kEnvMapSize, kEnvMapSize, 6, Format.R16g16b16a16Sfloat, 0, ImageUsageFlags.Storage);
            irMap = Texture.Create(kIrradianceMapSize, kIrradianceMapSize, 6, Format.R16g16b16a16Sfloat, 1, ImageUsageFlags.Storage);
            brdfLUT = Texture.Create(kBRDF_LUT_Size, kBRDF_LUT_Size, 1, Format.R16g16Sfloat, 1, ImageUsageFlags.Storage);

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
            /*
            // Load & pre-process environment map.
            {
                Texture envTextureUnfiltered = createTexture(kEnvMapSize, kEnvMapSize, 6, VK_FORMAT_R16G16B16A16_SFLOAT, 0, VK_IMAGE_USAGE_STORAGE_BIT);

                // Load & convert equirectangular envuronment map to cubemap texture
                {
                    VkPipeline pipeline = createComputePipeline("shaders/spirv/equirect2cube_cs.spv", computePipelineLayout);

                    Texture envTextureEquirect = createTexture(Image::fromFile("environment.hdr"), VK_FORMAT_R32G32B32A32_SFLOAT, 1);

                    const VkDescriptorImageInfo inputTexture = { VK_NULL_HANDLE, envTextureEquirect.view, VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL };
                    const VkDescriptorImageInfo outputTexture = { VK_NULL_HANDLE, envTextureUnfiltered.view, VK_IMAGE_LAYOUT_GENERAL };
                    updateDescriptorSet(computeDescriptorSet, Binding_InputTexture, VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, { inputTexture });
                    updateDescriptorSet(computeDescriptorSet, Binding_OutputTexture, VK_DESCRIPTOR_TYPE_STORAGE_IMAGE, { outputTexture });

                    VkCommandBuffer commandBuffer = beginImmediateCommandBuffer();
                    {
                        const auto preDispatchBarrier = ImageMemoryBarrier(envTextureUnfiltered, 0, VK_ACCESS_SHADER_WRITE_BIT, VK_IMAGE_LAYOUT_UNDEFINED, VK_IMAGE_LAYOUT_GENERAL).mipLevels(0, 1);
                        pipelineBarrier(commandBuffer, VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, { preDispatchBarrier });

                        vkCmdBindPipeline(commandBuffer, VK_PIPELINE_BIND_POINT_COMPUTE, pipeline);
                        vkCmdBindDescriptorSets(commandBuffer, VK_PIPELINE_BIND_POINT_COMPUTE, computePipelineLayout, 0, 1, &computeDescriptorSet, 0, nullptr);
                        vkCmdDispatch(commandBuffer, kEnvMapSize / 32, kEnvMapSize / 32, 6);

                        const auto postDispatchBarrier = ImageMemoryBarrier(envTextureUnfiltered, VK_ACCESS_SHADER_WRITE_BIT, 0, VK_IMAGE_LAYOUT_GENERAL, VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL).mipLevels(0, 1);
                        pipelineBarrier(commandBuffer, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT, { postDispatchBarrier });
                    }
                    executeImmediateCommandBuffer(commandBuffer);

                    vkDestroyPipeline(m_device, pipeline, nullptr);
                    destroyTexture(envTextureEquirect);

                    generateMipmaps(envTextureUnfiltered);
                }
            */

            { 
                // Compute pre-filtered specular environment map.
                {
                    uint numMipTailLevels = (uint)kEnvMapLevels - 1;
                    /*
                    VkPipeline pipeline;
                    {
                        const VkSpecializationMapEntry specializationMap = { 0, 0, sizeof(uint32_t) };
                        const uint specializationData[] = { numMipTailLevels };

                        const VkSpecializationInfo specializationInfo = { 1, &specializationMap, sizeof(specializationData), specializationData };
                        pipeline = createComputePipeline("shaders/spirv/spmap_cs.spv", computePipelineLayout, &specializationInfo);
                    }*/

                    Shader shader = Resources.Load<Shader>("shaders/spmap.shader");
                    ResourceLayout resLayout = shader.Main.GetResourceLayout(0);
                    spResSet = new ResourceSet(resLayout, cubeMap, envMap);
                }

                CommandBuffer commandBuffer = Graphics.BeginWorkCommandBuffer();

                // Copy base mipmap level into destination environment map.
                {
                /*
                        const std::vector<ImageMemoryBarrier> preCopyBarriers = {
                    ImageMemoryBarrier(envTextureUnfiltered, 0, VK_ACCESS_TRANSFER_READ_BIT, VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL, VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL).mipLevels(0, 1),
                    ImageMemoryBarrier(m_envTexture, 0, VK_ACCESS_TRANSFER_WRITE_BIT, VK_IMAGE_LAYOUT_UNDEFINED, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL),
                };
                        const std::vector<ImageMemoryBarrier> postCopyBarriers = {
                    ImageMemoryBarrier(envTextureUnfiltered, VK_ACCESS_TRANSFER_READ_BIT, VK_ACCESS_SHADER_READ_BIT, VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL).mipLevels(0, 1),
                    ImageMemoryBarrier(m_envTexture, VK_ACCESS_TRANSFER_WRITE_BIT, VK_ACCESS_SHADER_WRITE_BIT, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, VK_IMAGE_LAYOUT_GENERAL),
                };

                        pipelineBarrier(commandBuffer, VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT, VK_PIPELINE_STAGE_TRANSFER_BIT, preCopyBarriers);

                        VkImageCopy copyRegion = { };
                        copyRegion.extent = { m_envTexture.width, m_envTexture.height, 1 };
                        copyRegion.srcSubresource.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
                        copyRegion.srcSubresource.layerCount = m_envTexture.layers;
                        copyRegion.dstSubresource = copyRegion.srcSubresource;
                        vkCmdCopyImage(commandBuffer,
                            envTextureUnfiltered.image.resource, VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                            m_envTexture.image.resource, VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                            1, &copyRegion);

                        pipelineBarrier(commandBuffer, VK_PIPELINE_STAGE_TRANSFER_BIT, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, postCopyBarriers);
                    }

                    // Pre-filter rest of the mip-chain.
                    std::vector<VkImageView> envTextureMipTailViews;
                    {
                        std::vector<VkDescriptorImageInfo> envTextureMipTailDescriptors;
                        const VkDescriptorImageInfo inputTexture = { VK_NULL_HANDLE, envTextureUnfiltered.view, VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL };
                        updateDescriptorSet(computeDescriptorSet, Binding_InputTexture, VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, { inputTexture });

                        for (uint32_t level = 1; level < kEnvMapLevels; ++level)
                        {
                            envTextureMipTailViews.push_back(createTextureView(m_envTexture, VK_FORMAT_R16G16B16A16_SFLOAT, VK_IMAGE_ASPECT_COLOR_BIT, level, 1));
                            envTextureMipTailDescriptors.push_back(VkDescriptorImageInfo{ VK_NULL_HANDLE, envTextureMipTailViews[level - 1], VK_IMAGE_LAYOUT_GENERAL });
                    }
                    updateDescriptorSet(computeDescriptorSet, Binding_OutputMipTail, VK_DESCRIPTOR_TYPE_STORAGE_IMAGE, envTextureMipTailDescriptors);

                    vkCmdBindPipeline(commandBuffer, VK_PIPELINE_BIND_POINT_COMPUTE, pipeline);
                    vkCmdBindDescriptorSets(commandBuffer, VK_PIPELINE_BIND_POINT_COMPUTE, computePipelineLayout, 0, 1, &computeDescriptorSet, 0, nullptr);

                    const float deltaRoughness = 1.0f / std::max(float(numMipTailLevels), 1.0f);
                    for (uint32_t level = 1, size = kEnvMapSize / 2; level < kEnvMapLevels; ++level, size /= 2)
                    {
                        const uint32_t numGroups = std::max<uint32_t>(1, size / 32);

                        const SpecularFilterPushConstants pushConstants = { level - 1, level * deltaRoughness };
                        vkCmdPushConstants(commandBuffer, computePipelineLayout, VK_SHADER_STAGE_COMPUTE_BIT, 0, sizeof(SpecularFilterPushConstants), &pushConstants);
                        vkCmdDispatch(commandBuffer, numGroups, numGroups, 6);
                    }

                    const auto barrier = ImageMemoryBarrier(m_envTexture, VK_ACCESS_SHADER_WRITE_BIT, 0, VK_IMAGE_LAYOUT_GENERAL, VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
                    pipelineBarrier(commandBuffer, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT, { barrier });
            */
                }

                Graphics.EndWorkCommandBuffer(commandBuffer);
                //executeImmediateCommandBuffer(commandBuffer);

                //for (VkImageView mipTailView : envTextureMipTailViews)
                //{
                //    vkDestroyImageView(m_device, mipTailView, nullptr);
                //}
                //vkDestroyPipeline(m_device, pipeline, nullptr);
                //destroyTexture(envTextureUnfiltered);
            }

            // Compute diffuse irradiance cubemap
            {
                Shader shader = Resources.Load<Shader>("shaders/irmap.shader");
                ResourceLayout resLayout = shader.Main.GetResourceLayout(0);
                irResSet = new ResourceSet(resLayout, cubeMap, irMap);

//                 const VkDescriptorImageInfo inputTexture = { VK_NULL_HANDLE, m_envTexture.view, VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL };
//                 const VkDescriptorImageInfo outputTexture = { VK_NULL_HANDLE, m_irmapTexture.view, VK_IMAGE_LAYOUT_GENERAL };
//                 updateDescriptorSet(computeDescriptorSet, Binding_InputTexture, VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, { inputTexture });
//                 updateDescriptorSet(computeDescriptorSet, Binding_OutputTexture, VK_DESCRIPTOR_TYPE_STORAGE_IMAGE, { outputTexture });

                CommandBuffer commandBuffer = Graphics.BeginWorkCommandBuffer();
                {
                    System.Span<ImageMemoryBarrier> barriers = stackalloc []{ new ImageMemoryBarrier(irMap, 0, AccessFlags.ShaderWrite, ImageLayout.Undefined, ImageLayout.General) };
                    commandBuffer.PipelineBarrier(PipelineStageFlags.TopOfPipe, PipelineStageFlags.ComputeShader, barriers);
                    //const auto preDispatchBarrier = ImageMemoryBarrier(m_irmapTexture, 0, VK_ACCESS_SHADER_WRITE_BIT, VK_IMAGE_LAYOUT_UNDEFINED, VK_IMAGE_LAYOUT_GENERAL);
                    //pipelineBarrier(commandBuffer, VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, { preDispatchBarrier });

                    commandBuffer.BindComputePipeline(shader.Main);
                    commandBuffer.BindComputeResourceSet(shader.Main.PipelineLayout, 0, irResSet, null);
                    commandBuffer.Dispatch(kIrradianceMapSize / 32, kIrradianceMapSize / 32, 6);

                    System.Span<ImageMemoryBarrier> postDispatchBarrier = stackalloc [] { new ImageMemoryBarrier(irMap, AccessFlags.ShaderWrite, 0, ImageLayout.General, ImageLayout.ShaderReadOnlyOptimal) };
                    commandBuffer.PipelineBarrier(PipelineStageFlags.ComputeShader, PipelineStageFlags.BottomOfPipe, postDispatchBarrier);
                    //const auto postDispatchBarrier = ImageMemoryBarrier(m_irmapTexture, VK_ACCESS_SHADER_WRITE_BIT, 0, VK_IMAGE_LAYOUT_GENERAL, VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
                    //pipelineBarrier(commandBuffer, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT, { postDispatchBarrier });
                }
                Graphics.EndWorkCommandBuffer(commandBuffer);
                //vkDestroyPipeline(m_device, pipeline, nullptr);
            }

            // Compute Cook-Torrance BRDF 2D LUT for split-sum approximation.
            {
                Shader shader = Resources.Load<Shader>("shaders/brdf.shader");

                ResourceLayout resLayout = shader.Main.GetResourceLayout(0);
                brdfResSet = new ResourceSet(resLayout, brdfLUT);

                CommandBuffer commandBuffer = Graphics.BeginWorkCommandBuffer();
                {
                    System.Span<ImageMemoryBarrier> barriers = stackalloc [] { new ImageMemoryBarrier(brdfLUT, 0, AccessFlags.ShaderWrite, ImageLayout.Undefined, ImageLayout.General)};
                    commandBuffer.PipelineBarrier(PipelineStageFlags.TopOfPipe, PipelineStageFlags.ComputeShader, barriers);

                    commandBuffer.BindComputePipeline(shader.Main);
                    commandBuffer.BindComputeResourceSet(shader.Main.PipelineLayout, 0, brdfResSet, null);
                    commandBuffer.Dispatch(kBRDF_LUT_Size / 32, kBRDF_LUT_Size / 32, 6);

                    System.Span<ImageMemoryBarrier> postDispatchBarrier = stackalloc [] { new ImageMemoryBarrier(brdfLUT, AccessFlags.ShaderWrite, 0, ImageLayout.General, ImageLayout.ShaderReadOnlyOptimal) };
                    commandBuffer.PipelineBarrier(PipelineStageFlags.ComputeShader, PipelineStageFlags.BottomOfPipe, postDispatchBarrier);
                }

                Graphics.EndWorkCommandBuffer(commandBuffer);

            }
        }

        bool debugOpen = true;

        void OnDebugGUI()
        {

            var io = ImGuiNET.ImGui.GetIO();
            {
                Vector2 window_pos = new Vector2(10, io.DisplaySize.Y - 10);
                Vector2 window_pos_pivot = new Vector2(0.0f, 1.0f);
                ImGuiNET.ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
                ImGuiNET.ImGui.SetNextWindowBgAlpha(0.5f); // Transparent background
            }

            if (ImGuiNET.ImGui.Begin("Debugger", ref debugOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
            {
                ImGUI.Image(brdfLUT, new Vector2(200, 200));
                ImGUI.Image(envMap, new Vector2(200, 200));
                ImGUI.Image(irMap, new Vector2(200, 200));
            }

            ImGui.End();


        }



    }

}
