﻿using ImGuiNET;
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
    public struct UboVS
    {
        public Matrix4x4 projection;
    }

    public unsafe class ImGUI : Application, IDisposable
    {
        GraphicsBuffer vertexBuffer = new GraphicsBuffer();
        GraphicsBuffer indexBuffer = new GraphicsBuffer();
        GraphicsBuffer uniformBufferVS = new GraphicsBuffer();
        Texture texture;
        UboVS uboVS;
        Shader uiShader;
        Pipeline pipeline;
        ResourceLayout resourceLayout;
        ResourceSet resourceSet;

        private IntPtr fontAtlasID = (IntPtr)1;

        public ImGUI()
        {
            zoom = -2.5f;
            rotation = new Vector3(0.0f, 15.0f, 0.0f);
            Title = "SharpGame Example - ImGUI";
        }

        public override void Initialize()
        {
            base.Initialize();

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            ImGui.GetIO().Fonts.AddFontDefault();

            CreateGraphicsResources();
            
            RecreateFontDeviceTexture();

            resourceSet = new ResourceSet(resourceLayout, uniformBufferVS, texture);

            ImGuiStylePtr style = ImGui.GetStyle();
            style.WindowRounding = 2;

            SetOpenTKKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            ImGui.NewFrame();

            prepared = true;
        }

        protected override void Destroy()
        {
            texture.Dispose();
            
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            uniformBufferVS.Dispose();
            pipeline.Dispose();
        }


        void CreateGraphicsResources()
        {
            vertexBuffer = GraphicsBuffer.CreateDynamic<Pos2dTexColorVertex>(VkBufferUsageFlags.VertexBuffer, 4096);
            indexBuffer = GraphicsBuffer.CreateDynamic<ushort>(VkBufferUsageFlags.IndexBuffer, 4096);
            uniformBufferVS = GraphicsBuffer.CreateUniformBuffer<UboVS>();
                        
            resourceLayout = new ResourceLayout
            (
                new ResourceLayoutBinding(DescriptorType.UniformBuffer, ShaderStage.Vertex, 0),
                new ResourceLayoutBinding(DescriptorType.CombinedImageSampler, ShaderStage.Fragment, 1)
            );

            uiShader = new Shader
            {
                Main = new Pass("shaders/texture/ImGui.vert.spv", "shaders/texture/ImGui.frag.spv")
                {
                    ResourceLayout = resourceLayout
                }
            };

            pipeline = new Pipeline
            {
                CullMode = CullMode.None,
                DepthTestEnable = false,
                DepthWriteEnable = false,
                BlendMode = BlendMode.Alpha,
                DynamicState = new DynamicStateInfo(DynamicState.Viewport, DynamicState.Scissor),
                VertexLayout = Pos2dTexColorVertex.Layout
            };
        }

        // Prepare and initialize uniform buffer containing shader uniforms
        void PrepareUniformBuffers()
        {
            var localUboVS = uboVS;

            uniformBufferVS = GraphicsBuffer.CreateUniformBuffer<UboVS>();

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

            uniformBufferVS.SetData(ref uboVS);
        }

        private unsafe void RecreateFontDeviceTexture()
        {
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out byte* out_pixels, out int out_width, out int out_height, out int out_bytes_per_pixel);
            this.texture = Texture2D.Create((uint)out_width, (uint)out_height, (uint)out_bytes_per_pixel, out_pixels);
            io.Fonts.SetTexID(fontAtlasID);
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
            //VkCommandBufferBeginInfo cmdBufInfo = Builder.CommandBufferBeginInfo();

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

            var cmdBuffer = Graphics.Instance.RenderCmdBuffer;// Graphics.drawCmdBuffers[graphics.currentBuffer];
            {
                // Set target frame buffer
                renderPassBeginInfo.framebuffer = Graphics.frameBuffers[graphics.currentBuffer];

                //Util.CheckResult(vkBeginCommandBuffer(cmdBuffer.commandBuffer, &cmdBufInfo));
                cmdBuffer.Begin();

                //vkCmdBeginRenderPass(cmdBuffer.commandBuffer, &renderPassBeginInfo, VkSubpassContents.Inline);
                cmdBuffer.BeginRenderPass(ref renderPassBeginInfo, VkSubpassContents.Inline);

                VkViewport viewport = Builder.Viewport((float)width, (float)height, 0.0f, 1.0f);
                //vkCmdSetViewport(cmdBuffer.commandBuffer, 0, 1, &viewport);
                cmdBuffer.SetViewport(ref viewport);
                VkRect2D scissor = Builder.Rect2D(0, 0, width, height);
                //vkCmdSetScissor(cmdBuffer.commandBuffer, 0, 1, &scissor);
                cmdBuffer.SetScissor(ref scissor);

                RenderImDrawData(ImGui.GetDrawData());

                //vkCmdEndRenderPass(cmdBuffer.commandBuffer);
                cmdBuffer.EndRenderPass();

                //Util.CheckResult(vkEndCommandBuffer(cmdBuffer.commandBuffer));
                cmdBuffer.End();
            }
        }

        void draw()
        {
            graphics.BeginRender();

            buildCommandBuffers();

            graphics.EndRender();
        }

        protected override void render()
        {
            if (!prepared)
                return;

            SetPerFrameImGuiData(Time.Delta);

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

        private unsafe void RenderImDrawData(ImDrawDataPtr draw_data)
        {
            var io = ImGui.GetIO();

            float width = io.DisplaySize.X;
            float height = io.DisplaySize.Y;
            
            if (draw_data.CmdListsCount == 0)
            {
                return;
            }
           
            if (draw_data.TotalVtxCount*sizeof(ImDrawVert) > (int)vertexBuffer.size)
            {
                vertexBuffer.Dispose();
                vertexBuffer = GraphicsBuffer.CreateDynamic<ImDrawVert>(VkBufferUsageFlags.VertexBuffer, (int)(1.5f * draw_data.TotalVtxCount));
            }

            if (draw_data.TotalIdxCount * sizeof(ushort) > (int)indexBuffer.size)
            {
                indexBuffer.Dispose();
                indexBuffer = GraphicsBuffer.CreateDynamic<ushort>(VkBufferUsageFlags.IndexBuffer, (int)(1.5f * draw_data.TotalIdxCount));
            }

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


            var cmdBuffer = Graphics.Instance.RenderCmdBuffer;////Graphics.drawCmdBuffers[graphics.currentBuffer];

            var pipelines_solid = pipeline.GetGraphicsPipeline(Graphics.renderPass, uiShader.Main, null);
            //vkCmdBindDescriptorSets(cmdBuffer.commandBuffer, VkPipelineBindPoint.Graphics, pipeline.pipelineLayout, 0, 1, ref resourceSet.descriptorSet, 0, null);
            cmdBuffer.BindDescriptorSets(VkPipelineBindPoint.Graphics, pipeline.pipelineLayout, 0, 1, ref resourceSet.descriptorSet, 0, null);
            //vkCmdBindPipeline(cmdBuffer.commandBuffer, VkPipelineBindPoint.Graphics, pipelines_solid);
            cmdBuffer.BindPipeline(VkPipelineBindPoint.Graphics, pipelines_solid);

            ulong offsets = 0;
            //vkCmdBindVertexBuffers(cmdBuffer.commandBuffer, 0, 1, ref vertexBuffer.buffer, &offsets);
            cmdBuffer.BindVertexBuffer(0, vertexBuffer, &offsets);

            //vkCmdBindIndexBuffer(cmdBuffer.commandBuffer, indexBuffer.buffer, 0, VkIndexType.Uint16);
            cmdBuffer.BindIndexBuffer(indexBuffer, 0, IndexType.Uint16);

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
                            if (pcmd.TextureId == fontAtlasID)
                            {
                                //    cl.SetGraphicsResourceSet(1, _fontTextureResourceSet);
                            }
                            else
                            {
                                //    cl.SetGraphicsResourceSet(1, GetImageResourceSet(pcmd.TextureId));
                            }
                        }
                        

                        VkRect2D scissor = Builder.Rect2D((int)pcmd.ClipRect.X, (int)pcmd.ClipRect.Y,
                            (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X), (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));
                        cmdBuffer.SetScissor(ref scissor);

                        cmdBuffer.DrawIndexed(pcmd.ElemCount, 1, (uint)idx_offset, vtx_offset, 0);

                    }

                    idx_offset += (int)pcmd.ElemCount;
                }

                vtx_offset += cmd_list.VtxBuffer.Size;
            }

        }


        private unsafe void UpdateImGuiInput()
        {
            var io = ImGui.GetIO();

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

            bool _controlDown = false;
            bool _shiftDown = false;
            bool _altDown = false;
            bool _winKeyDown = false;

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
