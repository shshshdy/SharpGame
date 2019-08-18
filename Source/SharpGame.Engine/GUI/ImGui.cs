﻿#define OVERLAY_PASS
using ImGuiNET;
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

    public class ImGUI : System<ImGUI>
    {
        DeviceBuffer[] vertexBuffer = new DeviceBuffer[2];
        DeviceBuffer[] indexBuffer = new DeviceBuffer[2];
        DeviceBuffer uniformBufferVS = new DeviceBuffer();
        Texture texture;
        Shader uiShader;
        Pass pass;
        ResourceLayout resourceLayout;
        ResourceSet resourceSet;
        ResourceLayout resourceLayoutTex;
        ResourceSet resourceSetTex;
        private IntPtr fontAtlasID = (IntPtr)1;

        RenderPass renderPass;
        Framebuffer[] framebuffers;

        GraphicsPass guiPass;

        private struct ResourceSetInfo
        {
            public readonly IntPtr ImGuiBinding;
            public readonly ResourceSet ResourceSet;

            public ResourceSetInfo(IntPtr imGuiBinding, ResourceSet resourceSet)
            {
                ImGuiBinding = imGuiBinding;
                ResourceSet = resourceSet;
            }
        }

        private readonly Dictionary<Texture, ResourceSetInfo> _setsByView = new Dictionary<Texture, ResourceSetInfo>();
        private readonly Dictionary<IntPtr, ResourceSetInfo> _viewsById = new Dictionary<IntPtr, ResourceSetInfo>();
        private readonly List<IDisposable> _ownedResources = new List<IDisposable>();
        private int _lastAssignedID = 100;

        public ImGUI()
        {
            IntPtr context = ImGuiNET.ImGui.CreateContext();
            ImGuiNET.ImGui.SetCurrentContext(context);
            //ImGui.GetIO().Fonts.AddFontDefault();
            File file = FileSystem.Instance.GetFile("fonts/arial.ttf");
            var bytes = file.ReadAllBytes();
            ImGuiNET.ImGui.GetIO().Fonts.AddFontFromMemoryTTF(Utilities.AsPointer(ref bytes[0]), 32, 15);

            CreateGraphicsResources();
            RecreateFontDeviceTexture();

            resourceSet = new ResourceSet(resourceLayout, uniformBufferVS);
            resourceSetTex = new ResourceSet(resourceLayoutTex, texture);

            ImGuiStylePtr style = ImGuiNET.ImGui.GetStyle();
            style.WindowRounding = 2;

            SetOpenTKKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            ImGuiNET.ImGui.NewFrame();

            this.Subscribe<BeginFrame>((e) => Update());

            guiPass = new GraphicsPass
            {
                framebuffers = framebuffers,
                renderPass = renderPass,
                OnDraw = (pass, view) =>
                {

                    var cmdBuffer = pass.CmdBuffer;
                    RenderImDrawData(cmdBuffer, ImGuiNET.ImGui.GetDrawData());
                }
            };

            Renderer.Instance.MainView.OverlayPass = guiPass;

        }

        protected override void Destroy()
        {
            texture.Dispose();

            vertexBuffer[0]?.Dispose();
            indexBuffer[0]?.Dispose();
            vertexBuffer[1]?.Dispose();
            indexBuffer[1]?.Dispose();
            uniformBufferVS.Dispose();
            uiShader.Dispose();
        }

        unsafe void CreateGraphicsResources()
        {
            uniformBufferVS = DeviceBuffer.CreateUniformBuffer<Matrix4x4>();

            resourceLayout = new ResourceLayout(0)
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex)
            };

            resourceLayoutTex = new ResourceLayout(1)
            {
                new ResourceLayoutBinding(0, DescriptorType.CombinedImageSampler, ShaderStage.Fragment)
            };

            uiShader = Resources.Instance.Load<Shader>("Shaders/ImGui.shader");
            pass = uiShader.Main;
            pass.VertexLayout = VertexPos2dTexColor.Layout;

            var graphics = Graphics.Instance;

            renderPass = graphics.CreateRenderPass();
            framebuffers = graphics.CreateSwapChainFramebuffers(renderPass);
        }

        private unsafe void RecreateFontDeviceTexture()
        {
            var io = ImGuiNET.ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out byte* out_pixels, out int out_width, out int out_height, out int out_bytes_per_pixel);

            Format[] fmts =
            {
                Format.Undefined,
                Format.R8Unorm,
                Format.R8g8Unorm,
                Format.R8g8b8Unorm,
                Format.R8g8b8a8Unorm,
            };

            texture = SharpGame.Texture.Create2D((uint)out_width, (uint)out_height, fmts[out_bytes_per_pixel], out_pixels);
            io.Fonts.SetTexID(fontAtlasID);
            io.Fonts.ClearTexData();
        }

        public IntPtr GetOrCreateImGuiBinding(Texture texture)
        {
            if (!_setsByView.TryGetValue(texture, out ResourceSetInfo rsi))
            {
                ResourceSet resourceSet = new ResourceSet(resourceLayoutTex, texture);
                rsi = new ResourceSetInfo(GetNextImGuiBindingID(), resourceSet);

                _setsByView.Add(texture, rsi);
                _viewsById.Add(rsi.ImGuiBinding, rsi);
                _ownedResources.Add(resourceSet);
            }

            return rsi.ImGuiBinding;
        }

        public void RemoveImGuiBinding(Texture textureView)
        {
            if (_setsByView.TryGetValue(textureView, out ResourceSetInfo rsi))
            {
                _setsByView.Remove(textureView);
                _viewsById.Remove(rsi.ImGuiBinding);
                _ownedResources.Remove(rsi.ResourceSet);
                rsi.ResourceSet.Dispose();
            }
        }

        private IntPtr GetNextImGuiBindingID()
        {
            int newID = _lastAssignedID++;
            return (IntPtr)newID;
        }

        public ResourceSet GetImageResourceSet(IntPtr imGuiBinding)
        {
            if (!_viewsById.TryGetValue(imGuiBinding, out ResourceSetInfo rsi))
            {
                throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding.ToString());
            }

            return rsi.ResourceSet;
        }

        private static unsafe void SetOpenTKKeyMappings()
        {
            ImGuiIOPtr io = ImGuiNET.ImGui.GetIO();
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
            ImGuiIOPtr io = ImGuiNET.ImGui.GetIO();
            io.DisplaySize = new Vector2(Graphics.Instance.Width, Graphics.Instance.Height);
            io.DisplayFramebufferScale = Vector2.One;// window.ScaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        private void Update()
        {
            SetPerFrameImGuiData(Time.Delta);

            UpdateImGuiInput();

            ImGuiNET.ImGui.NewFrame();

            this.SendGlobalEvent(new GUIEvent());

            ImGuiNET.ImGui.Render();

        }

        private unsafe void RenderImDrawData(CommandBuffer cmdBuffer, ImDrawDataPtr draw_data)
        {
            var io = ImGuiNET.ImGui.GetIO();
            var graphics = Graphics.Instance;
            float width = io.DisplaySize.X;
            float height = io.DisplaySize.Y;

            if (draw_data.CmdListsCount == 0)
            {
                return;
            }

            ref DeviceBuffer vb = ref vertexBuffer[graphics.WorkContext];
            ref DeviceBuffer ib = ref indexBuffer[graphics.WorkContext];
            if (vb == null || draw_data.TotalVtxCount * sizeof(ImDrawVert) > (int)vb.Size)
            {
                vb?.Dispose();
                vb = DeviceBuffer.CreateDynamic<ImDrawVert>(BufferUsageFlags.VertexBuffer, (uint)(1.5f * draw_data.TotalVtxCount));
            }

            if (ib == null || draw_data.TotalIdxCount * sizeof(ushort) > (int)ib.Size)
            {
                ib?.Dispose();
                ib = DeviceBuffer.CreateDynamic<ushort>(BufferUsageFlags.IndexBuffer, (uint)(1.5f * draw_data.TotalIdxCount));
            }

            var projection = Matrix4x4.CreateOrthographicOffCenter(0f, width, height, 0.0f, -1.0f, 1.0f);
            uniformBufferVS.SetData(ref projection);

            uint vertexOffsetInVertices = 0;
            uint indexOffsetInElements = 0;

            for (int i = 0; i < draw_data.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

                vb.SetData((void*)cmd_list.VtxBuffer.Data,
                    vertexOffsetInVertices * (uint)sizeof(ImDrawVert), (uint)cmd_list.VtxBuffer.Size * (uint)sizeof(ImDrawVert));

                ib.SetData((void*)cmd_list.IdxBuffer.Data,
                    indexOffsetInElements * sizeof(ushort), (uint)cmd_list.IdxBuffer.Size * sizeof(ushort));

                vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
                indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
            }
            
            var pipeline = pass.GetGraphicsPipeline(graphics.RenderPass, null);

            cmdBuffer.BindResourceSet(PipelineBindPoint.Graphics, pass.PipelineLayout, 0, resourceSet);
            cmdBuffer.BindPipeline(PipelineBindPoint.Graphics, pipeline);
            cmdBuffer.BindVertexBuffer(0, vb);
            cmdBuffer.BindIndexBuffer(ib, 0, IndexType.Uint16);

            draw_data.ScaleClipRects(ImGuiNET.ImGui.GetIO().DisplayFramebufferScale);

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
                                cmdBuffer.BindResourceSet(PipelineBindPoint.Graphics, pass.PipelineLayout, 1, resourceSetTex);
                            }
                            else
                            {
                                cmdBuffer.BindResourceSet(PipelineBindPoint.Graphics, pass.PipelineLayout, 1, GetImageResourceSet(pcmd.TextureId));
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
            var io = ImGuiNET.ImGui.GetIO();
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

        public static void Image(Texture texture, Vector2 size)
        {
            var img = Instance.GetOrCreateImGuiBinding(texture);
            ImGuiNET.ImGui.Image(img, size);
        }
    }
}