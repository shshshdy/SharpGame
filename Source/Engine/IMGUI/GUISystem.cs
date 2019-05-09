using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using VulkanCore;
using ImVec2 = System.Numerics.Vector2;

namespace SharpGame.Editor
{
    public struct GUIEvent
    {
        static GUIEvent ref_;
        public static ref GUIEvent Ref => ref ref_;

    }

    public class GUISystem : Object
    {
        private GraphicsBuffer _vertexBuffer;
        private GraphicsBuffer _indexBuffer;
        private GraphicsBuffer _projMatrixBuffer;

        private IntPtr _fontAtlasID = (IntPtr)1;

        private ResourceSet resourceSet_;

        private Shader uiShader_;
        private Pipeline pipeline_;
        private Texture fontTex_;

        
        public GUISystem()
        {
            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            ImGui.GetIO().Fonts.AddFontDefault();

            //ImGui.GetIO().Fonts.AddFontFromFileTTF("Data/font/arial.ttf", 16);

            var graphics = Get<Graphics>();
            var cache = Get<ResourceCache>();

            uiShader_ = new Shader(
                "UI",
                new Pass("ImGui.vert.spv", "ImGui.frag.spv")
                {
                    ResourceLayout = new ResourceLayout(
                        new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                        new DescriptorSetLayoutBinding(1, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment)
                    )
                }
            );          

            _projMatrixBuffer = UniformBuffer.Create<Matrix>(1);

            pipeline_ = new Pipeline
            {                
                VertexInputState = Pos2dTexColorVertex.Layout,
                DepthTestEnable = false,
                DepthWriteEnable = false,
                CullMode = CullModes.None,
                BlendMode = BlendMode.Alpha,
                DynamicStateCreateInfo = new PipelineDynamicStateCreateInfo(DynamicState.Scissor)
            };

            unsafe
            {
                _vertexBuffer = VertexBuffer.Create(IntPtr.Zero, sizeof(ImDrawVert), 4046, true);
                _indexBuffer = IndexBuffer.Create(IntPtr.Zero, sizeof(ushort), 4046, true);
            }

            RecreateFontDeviceTexture();

            var resourceLayout = uiShader_.Main.ResourceLayout;
            resourceSet_ = new ResourceSet(resourceLayout, _projMatrixBuffer, fontTex_);

            ImGuiStylePtr style = ImGui.GetStyle();
            //ImGuiUtil.ResetStyle(ImGuiStyle.EdinBlack, style );

            SetOpenTKKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            ImGui.NewFrame();

            this.SubscribeToEvent((ref BeginFrame e) => UpdateGUI());

            this.SubscribeToEvent((EndRenderPass e) => RenderGUI(e.renderPass, e.commandBuffer));

        }
        

        private unsafe void RecreateFontDeviceTexture()
        {
            var io = ImGui.GetIO();         
            io.Fonts.GetTexDataAsRGBA32(out byte* out_pixels, out int out_width, out int out_height, out int out_bytes_per_pixel);
            fontTex_ = Texture.CreateDynamic(out_width, out_height, out_bytes_per_pixel, (IntPtr)out_pixels);
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
            Graphics graphics = Get<Graphics>();
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new ImVec2(
                graphics.Width,
                graphics.Height);
            io.DisplayFramebufferScale = ImVec2.One;// window.ScaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        private void UpdateGUI()
        {
            UpdateImGuiInput();

            ImGui.NewFrame();

            SendEvent(GUIEvent.Ref);

        }

        unsafe void RenderGUI(RenderPass renderPass, CommandBuffer commandBuffer)
        {
            ImGui.Render();

            RenderImDrawData(renderPass, commandBuffer, ImGui.GetDrawData());
        }

        private unsafe void RenderImDrawData(RenderPass renderPass, CommandBuffer cmdBuffer, ImDrawDataPtr draw_data)
        {
            var io = ImGui.GetIO();
            float width = io.DisplaySize.X;
            float height = io.DisplaySize.Y;
            var graphics = Get<Graphics>();

            if (draw_data.CmdListsCount == 0)
            {
                return;
            }
            
            if (draw_data.TotalVtxCount > _vertexBuffer.Count)
            {
                _vertexBuffer.Dispose();
                _vertexBuffer = VertexBuffer.Create(IntPtr.Zero, sizeof(ImDrawVert), (int)(1.5f * draw_data.TotalVtxCount), true);
            }
            
            if (draw_data.TotalIdxCount > _indexBuffer.Count)
            {
                _indexBuffer.Dispose();
                _indexBuffer = IndexBuffer.Create(IntPtr.Zero, sizeof(ushort), (int)(1.5f*draw_data.TotalIdxCount), true);
            }

            Matrix proj = Matrix.OrthoOffCenterLH(
                     0f,
                     io.DisplaySize.X,
                     io.DisplaySize.Y,
                     0.0f,
                     -1.0f,
                     1.0f, false);

            _projMatrixBuffer.SetData(ref proj);            

            uint vertexOffsetInVertices = 0;
            uint indexOffsetInElements = 0;

            for (int i = 0; i < draw_data.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

                _vertexBuffer.SetData(cmd_list.VtxBuffer.Data,
                    (int)vertexOffsetInVertices * sizeof(ImDrawVert), cmd_list.VtxBuffer.Size * sizeof(ImDrawVert));

                _indexBuffer.SetData(cmd_list.IdxBuffer.Data,
                    (int)indexOffsetInElements * sizeof(ushort), cmd_list.IdxBuffer.Size * sizeof(ushort));

                vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
                indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
            }

            cmdBuffer.CmdBindVertexBuffer(_vertexBuffer.Buffer, 0);
            cmdBuffer.CmdBindIndexBuffer(_indexBuffer.Buffer, 0, IndexType.UInt16);
     
            var pipeline = pipeline_.GetGraphicsPipeline(renderPass, uiShader_, null);
            cmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, pipeline_.pipelineLayout, resourceSet_.descriptorSet);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);

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
                        
                        cmdBuffer.CmdSetScissor(
                            new Rect2D(
                            (int)pcmd.ClipRect.X,
                            (int)pcmd.ClipRect.Y,
                            (int)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                            (int)(pcmd.ClipRect.W - pcmd.ClipRect.Y)));

                        cmdBuffer.CmdDrawIndexed((int)pcmd.ElemCount, 1, idx_offset, vtx_offset, 0);
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
            Input input = Get<Input>();
            InputSnapshot snapshot = input.InputSnapshot;
            ImGuiIOPtr io = ImGui.GetIO();

            var mousePosition = NumericsUtil.Convert(snapshot.MousePosition);

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

    }
}
