﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public struct GUIEvent
    {
    }

    public class GUI : System<GUI>
    {
        GraphicsBuffer vertexBuffer = new GraphicsBuffer();
        GraphicsBuffer indexBuffer = new GraphicsBuffer();
        GraphicsBuffer uniformBufferVS = new GraphicsBuffer();
        Texture texture;
        Shader uiShader;
        Pipeline pipeline;
        ResourceLayout resourceLayout;
        ResourceSet resourceSet;
        private IntPtr fontAtlasID = (IntPtr)1;

        RenderPass renderPass;
        Framebuffer[] framebuffers;

        public GUI()
        {
            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            //ImGui.GetIO().Fonts.AddFontDefault();
            File file = ResourceCache.Instance.Open("fonts/arial.ttf");
            var bytes = file.ReadAllBytes();
            ImGui.GetIO().Fonts.AddFontFromMemoryTTF(Utilities.AsPointer(ref bytes[0]), 32, 15);

            CreateGraphicsResources();
            RecreateFontDeviceTexture();

            resourceSet = new ResourceSet(resourceLayout, uniformBufferVS, texture);

            ImGuiStylePtr style = ImGui.GetStyle();
            style.WindowRounding = 2;

            SetOpenTKKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            ImGui.NewFrame();

            this.Subscribe<BeginFrame>(Handle);
            this.Subscribe<EndRender>(Handle);
        }

        protected override void Destroy()
        {
            texture.Dispose();

            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            uniformBufferVS.Dispose();
            pipeline.Dispose();
        }


        unsafe void CreateGraphicsResources()
        {
            vertexBuffer = GraphicsBuffer.CreateDynamic<VertexPos2dTexColor>(BufferUsage.VertexBuffer, 4096);
            indexBuffer = GraphicsBuffer.CreateDynamic<ushort>(BufferUsage.IndexBuffer, 4096);
            uniformBufferVS = GraphicsBuffer.CreateUniformBuffer<Matrix4x4>();

            resourceLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex),
                new ResourceLayoutBinding(1, DescriptorType.CombinedImageSampler, ShaderStage.Fragment)
            };

            uiShader = new Shader("UI")
            {
                new Pass("shaders/ImGui.vert.spv", "shaders/ImGui.frag.spv")
                {
                    ResourceLayout = new []
                    {
                        resourceLayout
                    }
                }
            };

            var graphics = Graphics.Instance;

            pipeline = new Pipeline
            {
                CullMode = CullMode.None,
                DepthTestEnable = false,
                DepthWriteEnable = false,
                BlendMode = BlendMode.Alpha,
                DynamicState = new DynamicStateInfo(DynamicState.Viewport, DynamicState.Scissor),
                VertexLayout = VertexPos2dTexColor.Layout
            };

            AttachmentDescription[] attachments =
            {
                // Color attachment
                new AttachmentDescription
                (                
                    graphics.ColorFormat,
                    loadOp : AttachmentLoadOp.DontCare,
                    storeOp : AttachmentStoreOp.Store,
                    finalLayout : ImageLayout.PresentSrcKHR
                ),

                // Depth attachment
                new AttachmentDescription
                (
                    graphics.DepthFormat,
                    loadOp : AttachmentLoadOp.DontCare,
                    storeOp : AttachmentStoreOp.DontCare,
                    finalLayout : ImageLayout.DepthStencilAttachmentOptimal
                )
            };

            SubpassDescription[] subpassDescription =
            {
                new SubpassDescription
                {
                    pipelineBindPoint = PipelineBindPoint.Graphics,

                    pColorAttachments = new []
                    {
                        new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                    },

                    pDepthStencilAttachment = new []
                    {
                        new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal)
                    },
                }
            };

            // Subpass dependencies for layout transitions
            SubpassDependency[] dependencies =
            {
                new SubpassDependency
                {
                    srcSubpass = VulkanNative.SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = PipelineStageFlags.BottomOfPipe,
                    dstStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    srcAccessMask = AccessFlags.MemoryRead,
                    dstAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dependencyFlags = DependencyFlags.ByRegion
                },

                new SubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = VulkanNative.SubpassExternal,
                    srcStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = PipelineStageFlags.BottomOfPipe,
                    srcAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dstAccessMask = AccessFlags.MemoryRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },
            };

            var renderPassInfo = new RenderPassCreateInfo(attachments, subpassDescription, dependencies);
            renderPass = new RenderPass(ref renderPassInfo);
            framebuffers = Graphics.Instance.CreateSwapChainFramebuffers(renderPass);
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
            io.DisplaySize = new System.Numerics.Vector2(Graphics.Instance.Width, Graphics.Instance.Height);
            io.DisplayFramebufferScale = System.Numerics.Vector2.One;// window.ScaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        void Handle(BeginFrame e)
        {
            SetPerFrameImGuiData(Time.Delta);

            UpdateImGuiInput();

            ImGui.NewFrame();

            this.SendGlobalEvent(new GUIEvent());

            ImGui.Render();

        }

        unsafe void Handle(EndRender e)
        {
            var graphics = Graphics.Instance;
            var width = graphics.Width;
            var height = graphics.Height;
            var cmdBuffer = Graphics.Instance.RenderCmdBuffer;

            var renderPassBeginInfo = new RenderPassBeginInfo
            (
                renderPass, framebuffers[graphics.currentImage],
                new Rect2D(0, 0, width, height)
            );

            cmdBuffer.BeginRenderPass(ref renderPassBeginInfo, SubpassContents.Inline);

            cmdBuffer.SetViewport(new Viewport(0, 0, width, height));
            cmdBuffer.SetScissor(new Rect2D(0, 0, width, height));

            RenderImDrawData(ImGui.GetDrawData());

            cmdBuffer.EndRenderPass();
            
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

            if (draw_data.TotalVtxCount * sizeof(ImDrawVert) > (int)vertexBuffer.size)
            {
                vertexBuffer.Dispose();
                vertexBuffer = GraphicsBuffer.CreateDynamic<ImDrawVert>(BufferUsage.VertexBuffer, (int)(1.5f * draw_data.TotalVtxCount));
            }

            if (draw_data.TotalIdxCount * sizeof(ushort) > (int)indexBuffer.size)
            {
                indexBuffer.Dispose();
                indexBuffer = GraphicsBuffer.CreateDynamic<ushort>(BufferUsage.IndexBuffer, (int)(1.5f * draw_data.TotalIdxCount));
            }

            var projection = Matrix4x4.CreateOrthographicOffCenter(0f, width, height, 0.0f, -1.0f, 1.0f);
            uniformBufferVS.SetData(ref projection);

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

            var graphics = Graphics.Instance;
            var cmdBuffer = graphics.RenderCmdBuffer;

            var pipelines_solid = pipeline.GetGraphicsPipeline(graphics.RenderPass, uiShader.Main, null);

            cmdBuffer.BindDescriptorSets(PipelineBindPoint.Graphics, pipeline, 0, 1, ref resourceSet.descriptorSet, 0, null);
            cmdBuffer.BindPipeline(PipelineBindPoint.Graphics, pipelines_solid);
            cmdBuffer.BindVertexBuffer(0, vertexBuffer);
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


                        Rect2D scissor = new Rect2D((int)pcmd.ClipRect.X, (int)pcmd.ClipRect.Y,
                            (int)(pcmd.ClipRect.Z - pcmd.ClipRect.X), (int)(pcmd.ClipRect.W - pcmd.ClipRect.Y));
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
            var snapshot = Input.Instance.snapshot;

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
    }
}
