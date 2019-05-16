// This code has been adapted from the "Vulkan" C++ example repository, by Sascha Willems: https://github.com/SaschaWillems/Vulkan
// It is a direct translation from the original C++ code and style, with as little transformation as possible.

// Original file: texture/texture.cpp, 

/*
* Vulkan Example - Texture loading (and display) example (including mip maps)
*
* Copyright (C) 2016 by Sascha Willems - www.saschawillems.de
*
* This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
*/

using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;
using Veldrid.Sdl2;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
    // ImVertex layout for this example
    public struct ImVertex
    {
        public Vector2 pos;
        public Vector2 uv;
        public uint color;

        public const uint PositionOffset = 0;
        public const uint UvOffset = 8;
        public const uint ColorOffset = 16;
    };

    public unsafe class ImGUI : Application, IDisposable
    {
        Texture texture;

        public class Vertices
        {
            public VkPipelineVertexInputStateCreateInfo inputState;
            public NativeList<VkVertexInputBindingDescription> bindingDescriptions = new NativeList<VkVertexInputBindingDescription>();
            public NativeList<VkVertexInputAttributeDescription> attributeDescriptions = new NativeList<VkVertexInputAttributeDescription>();
        }

        Vertices vertices = new Vertices();

        GraphicsBuffer vertexBuffer = new GraphicsBuffer();
        GraphicsBuffer indexBuffer = new GraphicsBuffer();

        GraphicsBuffer uniformBufferVS = new GraphicsBuffer();

        public struct UboVS
        {
            public Matrix4x4 projection;
        }

        UboVS uboVS;
        VkPipeline pipelines_solid;

        VkPipelineLayout pipelineLayout;
        VkDescriptorSet descriptorSet;
        VkDescriptorSetLayout descriptorSetLayout;
        private const uint VERTEX_BUFFER_BIND_ID = 0;

        ImGUI()
        {
            zoom = -2.5f;
            rotation = new Vector3(0.0f, 15.0f, 0.0f);
            Title = "Vulkan Example - ImGUI";
            // enableTextOverlay = true;
        }

        public void Dispose()
        {
            // Clean up used Vulkan resources 
            // Note : Inherited destructor cleans up resources stored in base class

            destroyTextureImage(texture);

            Device.DestroyPipeline(pipelines_solid);

            Device.DestroyPipelineLayout(pipelineLayout);
            Device.DestroyDescriptorSetLayout(descriptorSetLayout);

            vertexBuffer.destroy();
            indexBuffer.destroy();
            uniformBufferVS.destroy();
        }

        // Free all Vulkan resources used a texture object
        void destroyTextureImage(Texture texture)
        {
            vkDestroyImageView(Graphics.device, texture.view, null);
            vkDestroyImage(Graphics.device, texture.image, null);
            vkDestroySampler(Graphics.device, texture.sampler, null);
            vkFreeMemory(Graphics.device, texture.deviceMemory, null);
        }

        void generateQuad()
        {
            // Create buffers
            // For the sake of simplicity we won't stage the vertex data to the gpu memory
            // ImVertex buffer
            Util.CheckResult(Device.createBuffer(
                VkBufferUsageFlags.VertexBuffer,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                vertexBuffer,
                (ulong)(4096 * sizeof(ImVertex)),
                null));
            // Index buffer
            Util.CheckResult(Device.createBuffer(
                VkBufferUsageFlags.IndexBuffer,
                VkMemoryPropertyFlags.HostCoherent | VkMemoryPropertyFlags.HostCoherent,
                indexBuffer,
                4096 * sizeof(ushort),
                null));
        }

        void setupVertexDescriptions()
        {
            // Binding description
            vertices.bindingDescriptions.Count = 1;
            vertices.bindingDescriptions[0] =
                Builder.VertexInputBindingDescription(
                    VERTEX_BUFFER_BIND_ID,
                    (uint)sizeof(ImVertex),
                    VkVertexInputRate.Vertex);

            // Attribute descriptions
            // Describes memory layout and shader positions
            vertices.attributeDescriptions.Count = 3;
            // Location 0 : Position
            vertices.attributeDescriptions[0] =
                Builder.VertexInputAttributeDescription(
                    VERTEX_BUFFER_BIND_ID,
                    0,
                    VkFormat.R32g32Sfloat,
                    ImVertex.PositionOffset);
            // Location 1 : Texture coordinates
            vertices.attributeDescriptions[1] =
                Builder.VertexInputAttributeDescription(
                    VERTEX_BUFFER_BIND_ID,
                    1,
                    VkFormat.R32g32Sfloat,
                    ImVertex.UvOffset);
            // Location 1 : ImVertex normal
            vertices.attributeDescriptions[2] =
                Builder.VertexInputAttributeDescription(
                    VERTEX_BUFFER_BIND_ID,
                    2,
                    VkFormat.R8g8b8a8Unorm,
                    ImVertex.ColorOffset);

            vertices.inputState = Builder.VertexInputStateCreateInfo();
            vertices.inputState.vertexBindingDescriptionCount = vertices.bindingDescriptions.Count;
            vertices.inputState.pVertexBindingDescriptions = (VkVertexInputBindingDescription*)vertices.bindingDescriptions.Data;
            vertices.inputState.vertexAttributeDescriptionCount = vertices.attributeDescriptions.Count;
            vertices.inputState.pVertexAttributeDescriptions = (VkVertexInputAttributeDescription*)vertices.attributeDescriptions.Data;
        }

        void setupDescriptorPool()
        {
            // Example uses one ubo and one image sampler
            FixedArray2<VkDescriptorPoolSize> poolSizes = new FixedArray2<VkDescriptorPoolSize>(
                    Builder.DescriptorPoolSize(VkDescriptorType.UniformBuffer, 1),
                    Builder.DescriptorPoolSize(VkDescriptorType.CombinedImageSampler, 1)
            );

            VkDescriptorPoolCreateInfo descriptorPoolInfo =
                Builder.DescriptorPoolCreateInfo(
                    poolSizes.Count,
                    (VkDescriptorPoolSize*)Unsafe.AsPointer(ref poolSizes),
                    2);

            Util.CheckResult(vkCreateDescriptorPool(Graphics.device, &descriptorPoolInfo, null, out descriptorPool));
        }

        void setupDescriptorSetLayout()
        {
            FixedArray2<VkDescriptorSetLayoutBinding> setLayoutBindings = new FixedArray2<VkDescriptorSetLayoutBinding>(
                // Binding 0 : ImVertex shader uniform buffer
                Builder.DescriptorSetLayoutBinding(
                    VkDescriptorType.UniformBuffer,
                    VkShaderStageFlags.Vertex,
                    0),
                // Binding 1 : Fragment shader image sampler
                Builder.DescriptorSetLayoutBinding(
                    VkDescriptorType.CombinedImageSampler,
                    VkShaderStageFlags.Fragment,
                    1)
            );

            VkDescriptorSetLayoutCreateInfo descriptorLayout =
                Builder.DescriptorSetLayoutCreateInfo(
                    (VkDescriptorSetLayoutBinding*)Unsafe.AsPointer(ref setLayoutBindings),
                    setLayoutBindings.Count);

            Util.CheckResult(vkCreateDescriptorSetLayout(Graphics.device, &descriptorLayout, null, out descriptorSetLayout));

            var layout = descriptorSetLayout;
            VkPipelineLayoutCreateInfo pPipelineLayoutCreateInfo =
                Builder.PipelineLayoutCreateInfo(
                    ref layout,
                    1);

            Util.CheckResult(vkCreatePipelineLayout(Graphics.device, &pPipelineLayoutCreateInfo, null, out pipelineLayout));
        }

        void setupDescriptorSet()
        {
            var layout = descriptorSetLayout;
            VkDescriptorSetAllocateInfo allocInfo =
                Builder.DescriptorSetAllocateInfo(
                    descriptorPool,
                    &layout,
                    1);

            Util.CheckResult(vkAllocateDescriptorSets(Graphics.device, &allocInfo, out descriptorSet));

            // Setup a descriptor image info for the current texture to be used as a combined image sampler
            VkDescriptorImageInfo textureDescriptor;
            textureDescriptor.imageView = texture.view;             // The image's view (images are never directly accessed by the shader, but rather through views defining subresources)
            textureDescriptor.sampler = texture.sampler;            //	The sampler (Telling the pipeline how to sample the texture, including repeat, border, etc.)
            textureDescriptor.imageLayout = texture.imageLayout;    //	The current layout of the image (Note: Should always fit the actual use, e.g. shader read)

            var descriptor = uniformBufferVS.descriptor;
            FixedArray2<VkWriteDescriptorSet> writeDescriptorSets = new FixedArray2<VkWriteDescriptorSet>(
                    // Binding 0 : ImVertex shader uniform buffer
                    Builder.WriteDescriptorSet(
                        descriptorSet,
                        VkDescriptorType.UniformBuffer,
                        0,
                        ref descriptor),
                    // Binding 1 : Fragment shader texture sampler
                    //	Fragment shader: layout (binding = 1) uniform sampler2D samplerColor;
                    Builder.WriteDescriptorSet(
                        descriptorSet,
                        VkDescriptorType.CombinedImageSampler,          // The descriptor set will use a combined image sampler (sampler and image could be split)
                        1,                                                  // Shader binding point 1
                        ref textureDescriptor)								// Pointer to the descriptor image for our texture
            );

            vkUpdateDescriptorSets(Graphics.device, writeDescriptorSets.Count, ref writeDescriptorSets.First, 0, null);
        }

        void preparePipelines()
        {
            VkPipelineInputAssemblyStateCreateInfo inputAssemblyState =
                Builder.InputAssemblyStateCreateInfo(
                    VkPrimitiveTopology.TriangleList,
                    0,
                    False);

            VkPipelineRasterizationStateCreateInfo rasterizationState =
                Builder.RasterizationStateCreateInfo(
                    VkPolygonMode.Fill,
                    VkCullModeFlags.None,
                    VkFrontFace.CounterClockwise,
                    0);

            VkPipelineColorBlendAttachmentState blendAttachmentState =
                Builder.ColorBlendAttachmentState(
                    (VkColorComponentFlags)0xf, true);
            blendAttachmentState.alphaBlendOp = VkBlendOp.Add;
            blendAttachmentState.colorBlendOp = VkBlendOp.Add;
            blendAttachmentState.srcColorBlendFactor = VkBlendFactor.SrcAlpha;
            blendAttachmentState.dstColorBlendFactor = VkBlendFactor.OneMinusDstAlpha;
            blendAttachmentState.srcAlphaBlendFactor = VkBlendFactor.One;
            blendAttachmentState.dstAlphaBlendFactor = VkBlendFactor.Zero;

            VkPipelineColorBlendStateCreateInfo colorBlendState =
                Builder.ColorBlendStateCreateInfo(
                    1,
                    ref blendAttachmentState);

            VkPipelineDepthStencilStateCreateInfo depthStencilState =
                Builder.DepthStencilStateCreateInfo(
                    false,
                    false,
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
                    (VkDynamicState*)Unsafe.AsPointer(ref dynamicStateEnables),
                    dynamicStateEnables.Count,
                    0);

            // Load shaders
            FixedArray2<VkPipelineShaderStageCreateInfo> shaderStages = new FixedArray2<VkPipelineShaderStageCreateInfo>();

            shaderStages.First = Graphics.loadShader(getAssetPath() + "shaders/texture/ImGui.vert.spv", VkShaderStageFlags.Vertex);
            shaderStages.Second = Graphics.loadShader(getAssetPath() + "shaders/texture/ImGui.frag.spv", VkShaderStageFlags.Fragment);

            VkGraphicsPipelineCreateInfo pipelineCreateInfo =
                Builder.PipelineCreateInfo(
                    pipelineLayout,
                    Graphics.renderPass,
                    0);

            var vertexInputState = vertices.inputState;
            pipelineCreateInfo.pVertexInputState = &vertexInputState;
            pipelineCreateInfo.pInputAssemblyState = &inputAssemblyState;
            pipelineCreateInfo.pRasterizationState = &rasterizationState;
            pipelineCreateInfo.pColorBlendState = &colorBlendState;
            pipelineCreateInfo.pMultisampleState = &multisampleState;
            pipelineCreateInfo.pViewportState = &viewportState;
            pipelineCreateInfo.pDepthStencilState = &depthStencilState;
            pipelineCreateInfo.pDynamicState = &dynamicState;
            pipelineCreateInfo.stageCount = shaderStages.Count;
            pipelineCreateInfo.pStages = (VkPipelineShaderStageCreateInfo*)Unsafe.AsPointer(ref shaderStages);

            Util.CheckResult(vkCreateGraphicsPipelines(Graphics.device, graphics.pipelineCache, 1, &pipelineCreateInfo, null, out pipelines_solid));
        }

        // Prepare and initialize uniform buffer containing shader uniforms
        void prepareUniformBuffers()
        {
            var localUboVS = uboVS;
            // ImVertex shader uniform buffer block
            Util.CheckResult(Device.createBuffer(
                VkBufferUsageFlags.UniformBuffer,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                uniformBufferVS,
                (uint)sizeof(UboVS),
                &localUboVS));

            updateUniformBuffers();
        }

        void updateUniformBuffers()
        {
            uboVS.projection = Matrix4x4.CreateOrthographicOffCenter(
                     0f,
                     width,
                     height,
                     0.0f,
                     -1.0f,
                     1.0f);

            Util.CheckResult(uniformBufferVS.map());
            var local = uboVS;
            Unsafe.CopyBlock(uniformBufferVS.mapped, &local, (uint)sizeof(UboVS));
            uniformBufferVS.unmap();
        }


        public override void Initialize()
        {
            base.Initialize();

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            ImGui.GetIO().Fonts.AddFontDefault();
           

            //loadTextures();
            generateQuad();
            setupVertexDescriptions();
            prepareUniformBuffers();
            setupDescriptorSetLayout();
            preparePipelines();
            setupDescriptorPool();


            RecreateFontDeviceTexture();

            setupDescriptorSet();
            //buildCommandBuffers();

            ImGuiStylePtr style = ImGui.GetStyle();

            SetOpenTKKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            ImGui.NewFrame();

            prepared = true;
        }

        private IntPtr _fontAtlasID = (IntPtr)1;
      

        private unsafe void RecreateFontDeviceTexture()
        {
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out byte* out_pixels, out int out_width, out int out_height, out int out_bytes_per_pixel);
            this.texture = graphics.createTexture((uint)out_width, (uint)out_height, (uint)out_bytes_per_pixel, out_pixels);
            io.Fonts.SetTexID(_fontAtlasID);
            io.Fonts.ClearTexData();
        }

        private static unsafe void SetOpenTKKeyMappings()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.BackSpace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
            io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
        }

        private unsafe void SetPerFrameImGuiData(float deltaSeconds)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(
                this.width,
                this.height);
            io.DisplayFramebufferScale = System.Numerics.Vector2.One;// window.ScaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
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
            renderPassBeginInfo.pClearValues = (VkClearValue*)Unsafe.AsPointer(ref clearValues);

            //for (int i = 0; i < drawCmdBuffers.Count; ++i)
           
            var cmdBuffer = Graphics.drawCmdBuffers[graphics.currentBuffer];
            {
                // Set target frame buffer
                renderPassBeginInfo.framebuffer = Graphics.frameBuffers[graphics.currentBuffer];

                Util.CheckResult(vkBeginCommandBuffer(cmdBuffer, &cmdBufInfo));

                vkCmdBeginRenderPass(cmdBuffer, &renderPassBeginInfo, VkSubpassContents.Inline);

                VkViewport viewport = Builder.Viewport((float)width, (float)height, 0.0f, 1.0f);
                vkCmdSetViewport(cmdBuffer, 0, 1, &viewport);

                VkRect2D scissor = Builder.Rect2D(width, height, 0, 0);
                vkCmdSetScissor(cmdBuffer, 0, 1, &scissor);

                RenderImDrawData(ImGui.GetDrawData());

                vkCmdEndRenderPass(cmdBuffer);

                Util.CheckResult(vkEndCommandBuffer(cmdBuffer));
            }
        }

        void draw()
        {
            graphics.prepareFrame();

            buildCommandBuffers();

            graphics.submitFrame();
        }

        protected override void render()
        {
            if (!prepared)
                return;

            UpdateImGuiInput();

            ImGui.NewFrame();

            ImGui.ShowDemoWindow();

            ImGui.Render();

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
                case Key.KeypadAdd:
                    //changeLodBias(0.1f);
                    break;
                case Key.KeypadSubtract:
                    //changeLodBias(-0.1f);
                    break;
            }
        }

        private unsafe void RenderImDrawData(ImDrawDataPtr draw_data)
        {
            var io = ImGui.GetIO();

            float width = io.DisplaySize.X;
            float height = io.DisplaySize.Y;
            
            if (draw_data.CmdListsCount == 0)
            {
                return;
            }
            /*
            if (draw_data.TotalVtxCount*sizeof(ImDrawVert) > (int)vertexBuffer.size)
            {
                vertexBuffer.destroy();
                //vertexBuffer = GraphicsBuffer.CreateDynamic<ImDrawVert>(BufferUsages.VertexBuffer, (int)(1.5f * draw_data.TotalVtxCount));
            }

            if (draw_data.TotalIdxCount * sizeof(ushort) > (int)indexBuffer.size)
            {
                indexBuffer.destroy();
                //indexBuffer = GraphicsBuffer.CreateDynamic<ushort>(BufferUsages.IndexBuffer, (int)(1.5f * draw_data.TotalIdxCount));
            }*/

            updateUniformBuffers();

            uint vertexOffsetInVertices = 0;
            uint indexOffsetInElements = 0;

            for (int i = 0; i < draw_data.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

                vertexBuffer.SetData((void*)cmd_list.VtxBuffer.Data,
                    vertexOffsetInVertices * (uint)sizeof(ImDrawVert), (uint)cmd_list.VtxBuffer.Size * (uint)sizeof(ImDrawVert));

                indexBuffer.SetData((void*)cmd_list.IdxBuffer.Data,
                    indexOffsetInElements * sizeof(ushort), (uint)cmd_list.IdxBuffer.Size * sizeof(ushort));

                vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
                indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
            }


            var cmdBuffer = Graphics.drawCmdBuffers[graphics.currentBuffer];
            vkCmdBindDescriptorSets(cmdBuffer, VkPipelineBindPoint.Graphics, pipelineLayout, 0, 1, ref descriptorSet, 0, null);
            vkCmdBindPipeline(cmdBuffer, VkPipelineBindPoint.Graphics, pipelines_solid);

            ulong offsets = 0;
            vkCmdBindVertexBuffers(cmdBuffer, VERTEX_BUFFER_BIND_ID, 1, ref vertexBuffer.buffer, &offsets);
            vkCmdBindIndexBuffer(cmdBuffer, indexBuffer.buffer, 0, VkIndexType.Uint16);

            draw_data.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

            int vtx_offset = 0;
            int idx_offset = 0;
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        if (pcmd.TextureId != IntPtr.Zero)
                        {
                            if (pcmd.TextureId == _fontAtlasID)
                            {
                                //    cl.SetGraphicsResourceSet(1, _fontTextureResourceSet);
                            }
                            else
                            {
                                //    cl.SetGraphicsResourceSet(1, GetImageResourceSet(pcmd.TextureId));
                            }
                        }
                        

                        VkRect2D scissor = Builder.Rect2D((uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                            (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y), (int)pcmd.ClipRect.X, (int)pcmd.ClipRect.Y);
                        vkCmdSetScissor(cmdBuffer, 0, 1, &scissor);

                        vkCmdDrawIndexed(cmdBuffer, pcmd.ElemCount, 1, (uint)idx_offset, vtx_offset, 0);

                    }

                    idx_offset += (int)pcmd.ElemCount;
                }

                vtx_offset += cmd_list.VtxBuffer.Size;
            }

        }


        private bool _controlDown;
        private bool _shiftDown;
        private bool _altDown;
        private bool _winKeyDown;
        private unsafe void UpdateImGuiInput()
        {

            ImGuiIOPtr io = ImGui.GetIO();

            var mousePosition = snapshot.MousePosition;

            // Determine if any of the mouse buttons were pressed during this snapshot period, even if they are no longer held.
            bool leftPressed = false;
            bool middlePressed = false;
            bool rightPressed = false;
            foreach (MouseEvent me in snapshot.MouseEvents)
            {
                if (me.Down)
                {
                    switch (me.MouseButton)
                    {
                        case MouseButton.Left:
                            leftPressed = true;
                            break;
                        case MouseButton.Middle:
                            middlePressed = true;
                            break;
                        case MouseButton.Right:
                            rightPressed = true;
                            break;
                    }
                }
            }

            io.MouseDown[0] = leftPressed || snapshot.IsMouseDown(MouseButton.Left);
            io.MouseDown[1] = rightPressed || snapshot.IsMouseDown(MouseButton.Right);
            io.MouseDown[2] = middlePressed || snapshot.IsMouseDown(MouseButton.Middle);
            io.MousePos = mousePosition;
            io.MouseWheel = snapshot.WheelDelta;

            IReadOnlyList<char> keyCharPresses = snapshot.KeyCharPresses;
            for (int i = 0; i < keyCharPresses.Count; i++)
            {
                char c = keyCharPresses[i];
                io.AddInputCharacter(c);
            }

            IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
            for (int i = 0; i < keyEvents.Count; i++)
            {
                KeyEvent keyEvent = keyEvents[i];
                io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
                if (keyEvent.Key == Key.ControlLeft)
                {
                    _controlDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.ShiftLeft)
                {
                    _shiftDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.AltLeft)
                {
                    _altDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.WinLeft)
                {
                    _winKeyDown = keyEvent.Down;
                }
            }

            io.KeyCtrl = _controlDown;
            io.KeyAlt = _altDown;
            io.KeyShift = _shiftDown;
            io.KeySuper = _winKeyDown;
        }

        public static void Main() => new ImGUI().Run();
    }
}
