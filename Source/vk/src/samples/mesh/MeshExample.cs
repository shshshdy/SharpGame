﻿using Assimp;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vulkan;
using Veldrid.Sdl2;
using static Vulkan.VulkanNative;
using Veldrid;

namespace SharpGame
{
    public unsafe class MeshExample : Application
    {
        private const uint VERTEX_BUFFER_BIND_ID = 0;
        bool wireframe = false;
        Texture2D textures_colorMap = new Texture2D();

        VkPipelineVertexInputStateCreateInfo vertices_inputState;
        NativeList<VkVertexInputBindingDescription> vertices_bindingDescriptions = new NativeList<VkVertexInputBindingDescription>();
        NativeList<VkVertexInputAttributeDescription> vertices_attributeDescriptions = new NativeList<VkVertexInputAttributeDescription>();

        // Vertex layout used in this example
        // This must fit input locations of the vertex shader used to render the model
        struct Vertex
        {
            public Vector3 pos;
            public Vector3 normal;
            public Vector2 uv;
            public Vector3 color;
            public const uint PositionOffset = 0;
            public const uint NormalOffset = 12;
            public const uint UvOffset = 24;
            public const uint ColorOffset = 32;
        };

        // Contains all Vulkan resources required to represent vertex and index buffers for a model
        // This is for demonstration and learning purposes, the other examples use a model loader class for easy access
        VkBuffer model_vertices_buffer;
        VkDeviceMemory model_vertices_memory;

        int model_indices_count;
        VkBuffer model_indices_buffer;
        VkDeviceMemory model_indices_memory;

        // Destroys all Vulkan resources created for this model
        void destroyModel()
        {
            Device.DestroyBuffer(model_vertices_buffer);
            Device.FreeMemory(model_vertices_memory);
            Device.DestroyBuffer(model_indices_buffer);
            Device.FreeMemory(model_indices_memory);
        }

        GraphicsBuffer uniformBuffers_scene = new GraphicsBuffer();

        struct UboVS
        {
            public System.Numerics.Matrix4x4 projection;
            public System.Numerics.Matrix4x4 model;
            public Vector4 lightPos;
        }

        UboVS uboVS = new UboVS() { lightPos = new Vector4(25.0f, 5.0f, 5.0f, 1.0f) };
        VkPipeline pipelines_solid;
        VkPipeline pipelines_wireframe;

        VkPipelineLayout pipelineLayout;
        VkDescriptorSet descriptorSet;
        VkDescriptorSetLayout descriptorSetLayout;

        public MeshExample()
        {
            zoom = -5.5f;
            zoomSpeed = 20.5f;
            rotationSpeed = 0.5f;
            rotation = new Vector3(-0.5f, -112.75f, 0.0f);
            cameraPos = new Vector3(0.1f, 1.1f, 0.0f);
            Title = "Vulkan Example - Model rendering";
        }

        public void Dispose()
        {
            // Clean up used Vulkan resources 
            // Note : Inherited destructor cleans up resources stored in base class
            Device.DestroyPipeline(pipelines_solid);
            if (pipelines_wireframe != 0)
            {
                Device.DestroyPipeline(pipelines_wireframe);
            }

            Device.DestroyPipelineLayout(pipelineLayout);
            Device.DestroyDescriptorSetLayout(descriptorSetLayout);

            destroyModel();

            textures_colorMap.Dispose();
            uniformBuffers_scene.Dispose();
        }
        /*
        protected override void getEnabledFeatures()
        {
            // Fill mode non solid is required for wireframe display
            if (DeviceFeatures.fillModeNonSolid == 1)
            {
                var features = enabledFeatures;
                features.fillModeNonSolid = True;
                enabledFeatures = features;
            };
        }
        */
        void reBuildCommandBuffers()
        {/*
            if (!checkCommandBuffers())
            {
                destroyCommandBuffers();
                createCommandBuffers();
            }*/
            buildCommandBuffers();
        }

        protected override void buildCommandBuffers()
        {
            VkCommandBufferBeginInfo cmdBufInfo = Builder.CommandBufferBeginInfo();

            FixedArray2<VkClearValue> clearValues = new FixedArray2<VkClearValue>();
            clearValues.First.color = defaultClearColor;
            clearValues.Second.depthStencil = new VkClearDepthStencilValue() { depth = 1.0f, stencil = 0 };

            VkRenderPassBeginInfo renderPassBeginInfo = Builder.RenderPassBeginInfo();
            renderPassBeginInfo.renderPass = Graphics.renderPass;
            renderPassBeginInfo.renderArea.offset.x = 0;
            renderPassBeginInfo.renderArea.offset.y = 0;
            renderPassBeginInfo.renderArea.extent.width = width;
            renderPassBeginInfo.renderArea.extent.height = height;
            renderPassBeginInfo.clearValueCount = 2;
            renderPassBeginInfo.pClearValues = &clearValues.First;

            for (int i = 0; i < Graphics.drawCmdBuffers.Count; ++i)
            {
                // Set target frame buffer
                renderPassBeginInfo.framebuffer = Graphics.frameBuffers[i];

                Util.CheckResult(vkBeginCommandBuffer(Graphics.drawCmdBuffers[i], &cmdBufInfo));

                vkCmdBeginRenderPass(Graphics.drawCmdBuffers[i], &renderPassBeginInfo, VkSubpassContents.Inline);

                VkViewport viewport = Builder.Viewport((float)width, (float)height, 0.0f, 1.0f);
                vkCmdSetViewport(Graphics.drawCmdBuffers[i], 0, 1, &viewport);

                VkRect2D scissor = Builder.Rect2D(0, 0, width, height);
                vkCmdSetScissor(Graphics.drawCmdBuffers[i], 0, 1, &scissor);

                vkCmdBindDescriptorSets(Graphics.drawCmdBuffers[i], VkPipelineBindPoint.Graphics, pipelineLayout, 0, 1, ref descriptorSet, 0, null);
                vkCmdBindPipeline(Graphics.drawCmdBuffers[i], VkPipelineBindPoint.Graphics, wireframe ? pipelines_wireframe : pipelines_solid);

                ulong offsets = 0;
                // Bind mesh vertex buffer
                vkCmdBindVertexBuffers(Graphics.drawCmdBuffers[i], VERTEX_BUFFER_BIND_ID, 1, ref model_vertices_buffer, ref offsets);
                // Bind mesh index buffer
                vkCmdBindIndexBuffer(Graphics.drawCmdBuffers[i], model_indices_buffer, 0, VkIndexType.Uint32);
                // Render mesh vertex buffer using it's indices
                vkCmdDrawIndexed(Graphics.drawCmdBuffers[i], (uint)model_indices_count, 1, 0, 0, 0);

                vkCmdEndRenderPass(Graphics.drawCmdBuffers[i]);

                Util.CheckResult(vkEndCommandBuffer(Graphics.drawCmdBuffers[i]));
            }
        }

        // Load a model from file using the ASSIMP model loader and generate all resources required to render the model
        void loadModel(string filename)
        {
            // Load the model from file using ASSIMP

            // Flags for loading the mesh
            PostProcessSteps assimpFlags = PostProcessSteps.FlipWindingOrder | PostProcessSteps.Triangulate | PostProcessSteps.PreTransformVertices;

            var scene = new AssimpContext().ImportFile(filename, assimpFlags);

            // Generate vertex buffer from ASSIMP scene data
            float scale = 1.0f;
            NativeList<Vertex> vertexBuffer = new NativeList<Vertex>();

            // Iterate through all meshes in the file and extract the vertex components
            for (int m = 0; m < scene.MeshCount; m++)
            {
                for (int v = 0; v < scene.Meshes[(int)m].VertexCount; v++)
                {
                    Vertex vertex;
                    Mesh mesh = scene.Meshes[m];

                    // Use glm make_* functions to convert ASSIMP vectors to glm vectors
                    vertex.pos = new Vector3(mesh.Vertices[v].X, mesh.Vertices[v].Y, mesh.Vertices[v].Z) * scale;
                    vertex.normal = new Vector3(mesh.Normals[v].X, mesh.Normals[v].Y, mesh.Normals[v].Z);
                    // Texture coordinates and colors may have multiple channels, we only use the first [0] one
                    vertex.uv = new Vector2(mesh.TextureCoordinateChannels[0][v].X, mesh.TextureCoordinateChannels[0][v].Y);
                    // Mesh may not have vertex colors
                    if (mesh.HasVertexColors(0))
                    {
                        vertex.color = new Vector3(mesh.VertexColorChannels[0][v].R, mesh.VertexColorChannels[0][v].G, mesh.VertexColorChannels[0][v].B);
                    }
                    else
                    {
                        vertex.color = new Vector3(1f);
                    }

                    // Vulkan uses a right-handed NDC (contrary to OpenGL), so simply flip Y-Axis
                    vertex.pos.Y *= -1.0f;

                    vertexBuffer.Add(vertex);
                }
            }
            ulong vertexBufferSize = (ulong)(vertexBuffer.Count * sizeof(Vertex));

            // Generate index buffer from ASSIMP scene data
            NativeList<uint> indexBuffer = new NativeList<uint>();
            for (int m = 0; m < scene.MeshCount; m++)
            {
                uint indexBase = indexBuffer.Count;
                for (int f = 0; f < scene.Meshes[m].FaceCount; f++)
                {
                    // We assume that all faces are triangulated
                    for (int i = 0; i < 3; i++)
                    {
                        indexBuffer.Add((uint)scene.Meshes[m].Faces[f].Indices[i] + indexBase);
                    }
                }
            }
            ulong indexBufferSize = (ulong)(indexBuffer.Count * sizeof(uint));
            model_indices_count = (int)indexBuffer.Count;

            // Static mesh should always be Device local

            bool useStaging = true;

            if (useStaging)
            {

                VkBuffer vertexStaging_buffer;
                VkDeviceMemory vertexStaging_memory;
                VkBuffer indexStaging_buffer;
                VkDeviceMemory indexStaging_memory;

                // Create staging buffers
                // Vertex data
                Util.CheckResult(Device.createBuffer(
                    VkBufferUsageFlags.TransferSrc,
                    VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                    vertexBufferSize,
                    &vertexStaging_buffer,
                    &vertexStaging_memory,
                    vertexBuffer.Data.ToPointer()));
                // Index data
                Util.CheckResult(Device.createBuffer(
                    VkBufferUsageFlags.TransferSrc,
                    VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                    indexBufferSize,
                    &indexStaging_buffer,
                    &indexStaging_memory,
                    indexBuffer.Data.ToPointer()));

                // Create Device local buffers
                // Vertex buffer
                Util.CheckResult(Device.createBuffer(
                    VkBufferUsageFlags.VertexBuffer | VkBufferUsageFlags.TransferDst,
                    VkMemoryPropertyFlags.DeviceLocal,
                    vertexBufferSize,
                    out model_vertices_buffer,
                    out model_vertices_memory));
                // Index buffer
                Util.CheckResult(Device.createBuffer(
                    VkBufferUsageFlags.IndexBuffer | VkBufferUsageFlags.TransferDst,
                    VkMemoryPropertyFlags.DeviceLocal,
                    indexBufferSize,
                    out model_indices_buffer,
                    out model_indices_memory));

                // Copy from staging buffers
                VkCommandBuffer copyCmd = Device.createCommandBuffer(VkCommandBufferLevel.Primary, true);

                VkBufferCopy copyRegion = new VkBufferCopy();

                copyRegion.size = vertexBufferSize;

                vkCmdCopyBuffer(
                    copyCmd,
                    vertexStaging_buffer,
                    model_vertices_buffer,
                    1,
                    &copyRegion);

                copyRegion.size = indexBufferSize;

                vkCmdCopyBuffer(
                    copyCmd,
                    indexStaging_buffer,
                    model_indices_buffer,
                    1,
                    &copyRegion);

                Device.flushCommandBuffer(copyCmd, graphics.queue, true);


                Device.DestroyBuffer(vertexStaging_buffer);

                Device.FreeMemory(vertexStaging_memory);

                Device.DestroyBuffer(indexStaging_buffer);

                Device.FreeMemory(indexStaging_memory);
            }
            else
            {
                // Vertex buffer
                Util.CheckResult(Device.createBuffer(
                    VkBufferUsageFlags.VertexBuffer,
                    VkMemoryPropertyFlags.HostVisible,
                    vertexBufferSize,
                    out model_vertices_buffer,
                    out model_vertices_memory,
                    vertexBuffer.Data.ToPointer()));
                // Index buffer
                Util.CheckResult(Device.createBuffer(
                    VkBufferUsageFlags.IndexBuffer,
                    VkMemoryPropertyFlags.HostVisible,
                    indexBufferSize,
                    out model_indices_buffer,
                    out model_indices_memory,
                    indexBuffer.Data.ToPointer()));
            }
        }

        void loadAssets()
        {
            loadModel(getAssetPath() + "models/voyager/voyager.dae");
            if (Device.Features.textureCompressionBC == 1)
            {
                textures_colorMap.loadFromFile(getAssetPath() + "models/voyager/voyager_bc3_unorm.ktx",
                    VkFormat.Bc3UnormBlock, graphics.queue);
            }
            else if (Device.Features.textureCompressionASTC_LDR == 1)
            {
                textures_colorMap.loadFromFile(getAssetPath() + "models/voyager/voyager_astc_8x8_unorm.ktx", VkFormat.Astc8x8UnormBlock, graphics.queue);
            }
            else if (Device.Features.textureCompressionETC2 == 1)
            {
                textures_colorMap.loadFromFile(getAssetPath() + "models/voyager/voyager_etc2_unorm.ktx", VkFormat.Etc2R8g8b8a8UnormBlock, graphics.queue);
            }
            else
            {
                throw new InvalidOperationException("Device does not support any compressed texture format!");
            }
        }

        void setupVertexDescriptions()
        {
            // Binding description
            vertices_bindingDescriptions.Count = 1;
            vertices_bindingDescriptions[0] =
                Builder.VertexBinding(
                    VERTEX_BUFFER_BIND_ID,
                    (uint)sizeof(Vertex),
                    VkVertexInputRate.Vertex);

            // Attribute descriptions
            // Describes memory layout and shader positions
            vertices_attributeDescriptions.Count = 4;
            // Location 0 : Position
            vertices_attributeDescriptions[0] =
                Builder.VertexElement(
                    VERTEX_BUFFER_BIND_ID,
                    0,
                    VkFormat.R32g32b32Sfloat,
                    Vertex.PositionOffset);
            // Location 1 : Normal
            vertices_attributeDescriptions[1] =
                Builder.VertexElement(
                    VERTEX_BUFFER_BIND_ID,
                    1,
                    VkFormat.R32g32b32Sfloat,
                    Vertex.NormalOffset);
            // Location 2 : Texture coordinates
            vertices_attributeDescriptions[2] =
                Builder.VertexElement(
                    VERTEX_BUFFER_BIND_ID,
                    2,
                    VkFormat.R32g32Sfloat,
                    Vertex.UvOffset);
            // Location 3 : Color
            vertices_attributeDescriptions[3] =
                Builder.VertexElement(
                    VERTEX_BUFFER_BIND_ID,
                    3,
                    VkFormat.R32g32b32Sfloat,
                    Vertex.ColorOffset);

            vertices_inputState = Builder.VertexInputStateCreateInfo();
            vertices_inputState.vertexBindingDescriptionCount = (vertices_bindingDescriptions.Count);
            vertices_inputState.pVertexBindingDescriptions = (VkVertexInputBindingDescription*)vertices_bindingDescriptions.Data;
            vertices_inputState.vertexAttributeDescriptionCount = (vertices_attributeDescriptions.Count);
            vertices_inputState.pVertexAttributeDescriptions = (VkVertexInputAttributeDescription*)vertices_attributeDescriptions.Data;
        }

        void setupDescriptorPool()
        {
            // Example uses one ubo and one combined image sampler
            FixedArray2<VkDescriptorPoolSize> poolSizes = new FixedArray2<VkDescriptorPoolSize>(
                Builder.DescriptorPoolSize(VkDescriptorType.UniformBuffer, 1),
                Builder.DescriptorPoolSize(VkDescriptorType.CombinedImageSampler, 1));

            VkDescriptorPoolCreateInfo descriptorPoolInfo =
                Builder.DescriptorPoolCreateInfo(
                    poolSizes.Count,
                    &poolSizes.First,
                    1);

            Util.CheckResult(vkCreateDescriptorPool(Graphics.device, &descriptorPoolInfo, null, out descriptorPool));
        }

        void setupDescriptorSetLayout()
        {
            FixedArray2<VkDescriptorSetLayoutBinding> setLayoutBindings = new FixedArray2<VkDescriptorSetLayoutBinding>(
                // Binding 0 : Vertex shader uniform buffer
                Builder.DescriptorSetLayoutBinding(
                    VkDescriptorType.UniformBuffer,
                    VkShaderStageFlags.Vertex,
                    0),
                // Binding 1 : Fragment shader combined sampler
                Builder.DescriptorSetLayoutBinding(
                    VkDescriptorType.CombinedImageSampler,
                    VkShaderStageFlags.Fragment,
                    1));

            VkDescriptorSetLayoutCreateInfo descriptorLayout =
                Builder.DescriptorSetLayoutCreateInfo(
                    &setLayoutBindings.First,
                    setLayoutBindings.Count);

            Util.CheckResult(vkCreateDescriptorSetLayout(Graphics.device, &descriptorLayout, null, out descriptorSetLayout));

            var dsl = descriptorSetLayout;
            VkPipelineLayoutCreateInfo pPipelineLayoutCreateInfo =
                Builder.PipelineLayoutCreateInfo(
                    ref dsl,
                    1);

            Util.CheckResult(vkCreatePipelineLayout(Graphics.device, &pPipelineLayoutCreateInfo, null, out pipelineLayout));
        }

        void setupDescriptorSet()
        {
            var dsl = descriptorSetLayout;
            VkDescriptorSetAllocateInfo allocInfo =
                Builder.DescriptorSetAllocateInfo(
                    descriptorPool,
                    &dsl,
                    1);

            Util.CheckResult(vkAllocateDescriptorSets(Graphics.device, &allocInfo, out descriptorSet));

            VkDescriptorImageInfo texDescriptor =
                Builder.DescriptorImageInfo(
                    textures_colorMap.sampler,
                    textures_colorMap.view,
                    VkImageLayout.General);

            var temp = uniformBuffers_scene.descriptor;
            FixedArray2<VkWriteDescriptorSet> writeDescriptorSets = new FixedArray2<VkWriteDescriptorSet>(
                // Binding 0 : Vertex shader uniform buffer
                Builder.WriteDescriptorSet(
                    descriptorSet,
                    VkDescriptorType.UniformBuffer,
                    0,
                    ref temp),
                // Binding 1 : Color map 
                Builder.WriteDescriptorSet(
                    descriptorSet,
                    VkDescriptorType.CombinedImageSampler,
                    1,
                    ref texDescriptor));

            vkUpdateDescriptorSets(Graphics.device, (writeDescriptorSets.Count), ref writeDescriptorSets.First, 0, null);
        }

        void preparePipelines()
        {
            VkPipelineInputAssemblyStateCreateInfo inputAssemblyState =
                Builder.InputAssemblyStateCreateInfo(
                    PrimitiveTopology.TriangleList,
                    0,
                    False);

            VkPipelineRasterizationStateCreateInfo rasterizationState =
                Builder.RasterizationStateCreateInfo(
                    VkPolygonMode.Fill,
                    VkCullModeFlags.Back,
                    VkFrontFace.Clockwise,
                    0);

            VkPipelineColorBlendAttachmentState blendAttachmentState =
                Builder.ColorBlendAttachmentState(
                    (VkColorComponentFlags)0xf,
                    false);

            VkPipelineColorBlendStateCreateInfo colorBlendState =
                Builder.ColorBlendStateCreateInfo(
                    1,
                    ref blendAttachmentState);

            VkPipelineDepthStencilStateCreateInfo depthStencilState =
                Builder.DepthStencilStateCreateInfo(
                    true,
                    true,
                     VkCompareOp.LessOrEqual);

            VkPipelineViewportStateCreateInfo viewportState =
                Builder.ViewportStateCreateInfo(1, 1, 0);

            VkPipelineMultisampleStateCreateInfo multisampleState =
                Builder.MultisampleStateCreateInfo(
                    VkSampleCountFlags.Count1,
                    0);

            FixedArray2<VkDynamicState> dynamicStateEnables = new FixedArray2<VkDynamicState>(
                 VkDynamicState.Viewport,
                 VkDynamicState.Scissor);
            VkPipelineDynamicStateCreateInfo dynamicState =
                Builder.DynamicStateCreateInfo(
                    &dynamicStateEnables.First,
                    dynamicStateEnables.Count,
                    0);

            // Solid rendering pipeline
            // Load shaders
            FixedArray2<VkPipelineShaderStageCreateInfo> shaderStages = new FixedArray2<VkPipelineShaderStageCreateInfo>(
                Graphics.loadShader(getAssetPath() + "shaders/mesh/mesh.vert.spv", VkShaderStageFlags.Vertex),
                Graphics.loadShader(getAssetPath() + "shaders/mesh/mesh.frag.spv", VkShaderStageFlags.Fragment));

            VkGraphicsPipelineCreateInfo pipelineCreateInfo =
                Builder.GraphicsPipelineCreateInfo(
                    pipelineLayout,
                    Graphics.renderPass,
                    0);

            var via = vertices_inputState;
            pipelineCreateInfo.pVertexInputState = &via;
            pipelineCreateInfo.pInputAssemblyState = &inputAssemblyState;
            pipelineCreateInfo.pRasterizationState = &rasterizationState;
            pipelineCreateInfo.pColorBlendState = &colorBlendState;
            pipelineCreateInfo.pMultisampleState = &multisampleState;
            pipelineCreateInfo.pViewportState = &viewportState;
            pipelineCreateInfo.pDepthStencilState = &depthStencilState;
            pipelineCreateInfo.pDynamicState = &dynamicState;
            pipelineCreateInfo.stageCount = shaderStages.Count;
            pipelineCreateInfo.pStages = &shaderStages.First;

            pipelines_solid = Device.CreateGraphicsPipeline(ref pipelineCreateInfo);

            // Wire frame rendering pipeline
            if (Device.Features.fillModeNonSolid == 1)
            {
                rasterizationState.polygonMode = VkPolygonMode.Line;
                rasterizationState.lineWidth = 1.0f;
                pipelines_wireframe = Device.CreateGraphicsPipeline(ref pipelineCreateInfo);
            }
        }

        // Prepare and initialize uniform buffer containing shader uniforms
        void prepareUniformBuffers()
        {
            // Vertex shader uniform buffer block
            Util.CheckResult(Device.createBuffer(
                VkBufferUsageFlags.UniformBuffer,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                uniformBuffers_scene,
                (uint)sizeof(UboVS)));

            // Map persistent
            Util.CheckResult(uniformBuffers_scene.map());

            updateUniformBuffers();
        }

        void updateUniformBuffers()
        {
            uboVS.projection = System.Numerics.Matrix4x4.CreatePerspectiveFieldOfView(Util.DegreesToRadians(60.0f), (float)width / (float)height, 0.1f, 256.0f);
            System.Numerics.Matrix4x4 viewMatrix = System.Numerics.Matrix4x4.CreateTranslation(0.0f, 0.0f, zoom);

            uboVS.model = viewMatrix * System.Numerics.Matrix4x4.CreateTranslation((System.Numerics.Vector3)cameraPos);
            uboVS.model = System.Numerics.Matrix4x4.CreateRotationX(Util.DegreesToRadians(rotation.X)) * uboVS.model;
            uboVS.model = System.Numerics.Matrix4x4.CreateRotationY(Util.DegreesToRadians(rotation.Y)) * uboVS.model;
            uboVS.model = System.Numerics.Matrix4x4.CreateRotationZ(Util.DegreesToRadians(rotation.Z)) * uboVS.model;

            Unsafe.Copy(uniformBuffers_scene.mapped, ref uboVS);
        }

        void draw()
        {
            graphics.prepareFrame();

            graphics.submitFrame();
        }


        public override void Initialize()
        {
            base.Initialize();
            loadAssets();
            setupVertexDescriptions();
            prepareUniformBuffers();
            setupDescriptorSetLayout();
            preparePipelines();
            setupDescriptorPool();
            setupDescriptorSet();
            buildCommandBuffers();
            prepared = true;
        }

        protected override void render()
        {
            if (!prepared)
                return;
            draw();
        }

        protected override void viewChanged()
        {
            updateUniformBuffers();
        }

        protected override void keyPressed(Key keyCode)
        {
            switch (keyCode)
            {
                case  Key.W:
                    if (Device.Features.fillModeNonSolid == 1)
                    {
                        wireframe = !wireframe;
                        reBuildCommandBuffers();
                    }
                    break;
            }
        }

        public static void Main() => new MeshExample().Run();
    }
}
