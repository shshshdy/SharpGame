using ImGuiNET;
using Unique;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using ImVec2 = System.Numerics.Vector2;

namespace UniqueEditor
{
    public struct GUIEvent : IEvent
    {
        static GUIEvent ref_;
        public static ref GUIEvent Ref => ref ref_;

    }

    public class GUISystem : Object
    {
        Shader uiShader_;
        ShaderInstance uiShaderInstance_;
        RenderState renderState_ = RenderState.Default;
        Texture fontTex_;
        byte viewId_ = 255;
        
        public override void Init()
        {
            //ImGui.GetIO().FontAtlas.AddDefaultFont();
            ImGui.GetIO().FontAtlas.AddFontFromFileTTF("Data/font/arial.ttf", 16);

            ResourceCache cache = GetSubsystem<ResourceCache>();
            uiShader_ = cache.GetResource<Shader>("shaders/ui.shader");

            uiShaderInstance_ = uiShader_.GetInstance(0, "");
            //uiShaderInstance_.Create();

            RecreateFontDeviceTexture();

            Style style = ImGui.GetStyle();
            //ImGuiUtil.ResetStyle(ImGuiStyle.EdinBlack, style );

            SetOpenTKKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            ImGui.NewFrame();

            SubscribeToEvent((ref BeginFrame e) => UpdateGUI());

            SubscribeToEvent((ref PostRender e) => RenderGUI());

        }

        public override void Deinit()
        {
        }

        private unsafe void RecreateFontDeviceTexture()
        {
            var io = ImGui.GetIO();
            // Build
            var textureData = io.FontAtlas.GetTexDataAsRGBA32();
            MemoryBlock mem = new MemoryBlock((IntPtr)textureData.Pixels, textureData.BytesPerPixel * textureData.Width * textureData.Height);
            fontTex_ = Texture.Create2D(textureData.Width, textureData.Height, false, 1, TextureFormat.BGRA8, TextureFlags.None, mem);

            // Store our identifier
            io.FontAtlas.SetTexID(fontTex_.Handle);
            io.FontAtlas.ClearTexData();
        }

        private static unsafe void SetOpenTKKeyMappings()
        {
            IO io = ImGui.GetIO();
            io.KeyMap[GuiKey.Tab] = (int)Key.Tab;
            io.KeyMap[GuiKey.LeftArrow] = (int)Key.Left;
            io.KeyMap[GuiKey.RightArrow] = (int)Key.Right;
            io.KeyMap[GuiKey.UpArrow] = (int)Key.Up;
            io.KeyMap[GuiKey.DownArrow] = (int)Key.Down;
            io.KeyMap[GuiKey.PageUp] = (int)Key.PageUp;
            io.KeyMap[GuiKey.PageDown] = (int)Key.PageDown;
            io.KeyMap[GuiKey.Home] = (int)Key.Home;
            io.KeyMap[GuiKey.End] = (int)Key.End;
            io.KeyMap[GuiKey.Delete] = (int)Key.Delete;
            io.KeyMap[GuiKey.Backspace] = (int)Key.BackSpace;
            io.KeyMap[GuiKey.Enter] = (int)Key.Enter;
            io.KeyMap[GuiKey.Escape] = (int)Key.Escape;
            io.KeyMap[GuiKey.A] = (int)Key.A;
            io.KeyMap[GuiKey.C] = (int)Key.C;
            io.KeyMap[GuiKey.V] = (int)Key.V;
            io.KeyMap[GuiKey.X] = (int)Key.X;
            io.KeyMap[GuiKey.Y] = (int)Key.Y;
            io.KeyMap[GuiKey.Z] = (int)Key.Z;
        }

        private unsafe void SetPerFrameImGuiData(float deltaSeconds)
        {
            Graphics graphics = GetSubsystem<Graphics>();
            IO io = ImGui.GetIO();
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

        unsafe void RenderGUI()
        {
            ImGui.Render();

            RenderImDrawData(ImGui.GetDrawData());
        }

        private unsafe void RenderImDrawData(DrawData* draw_data)
        {
            IO io = ImGui.GetIO();
            float width = io.DisplaySize.X;
            float height = io.DisplaySize.Y;
            Graphics graphics = GetSubsystem<Graphics>();
            Capabilities caps = Bgfx.GetCaps();
            Matrix ortho = Matrix.OrthoOffCenterLH(0.0f, width, height, 0.0f, -1.0f, 1.0f, caps.HomogeneousDepth);
            Matrix view = Matrix.Identity;
            graphics.SetViewTransform(viewId_, ref view, ref ortho);
            Bgfx.SetViewRect(viewId_, 0, 0, (int)width, (int)height);

          //  ImGui.ScaleClipRects(draw_data, ImGui.GetIO().DisplayFramebufferScale);

            for(int i = 0; i < draw_data->CmdListsCount; ++i)
            {
                NativeDrawList* cmd_list = draw_data->CmdLists[i];
                DrawGUICmdList(cmd_list);
            }

            Bgfx.SetScissor(0, 0, 0, 0);            
        }

        unsafe void DrawGUICmdList(NativeDrawList* cmd_list)
        {
            int num_indices = cmd_list->IdxBuffer.Size;
		    int num_vertices = cmd_list->VtxBuffer.Size;

            var decl = Pos2dTexColorVertex.Layout;

            if(TransientIndexBuffer.GetAvailableSpace(num_indices) < num_indices)
                return;

            if(TransientVertexBuffer.GetAvailableSpace(num_vertices, decl) < num_vertices)
                return;

            Graphics graphics = GetSubsystem<Graphics>();
            TransientVertexBuffer vertex_buffer = new TransientVertexBuffer(num_vertices, decl);
            TransientIndexBuffer index_buffer = new TransientIndexBuffer(num_indices);
       
            Unsafe.CopyBlock((void*)vertex_buffer.Data, cmd_list->VtxBuffer.Data, (uint)(num_vertices * decl.Stride));
            Unsafe.CopyBlock((void*)index_buffer.Data, cmd_list->IdxBuffer.Data, (uint)(num_indices * sizeof(ushort)));
            
            RenderState state = 0
                | RenderState.ColorWrite
                | RenderState.AlphaWrite
                | RenderState.Multisampling
                | RenderState.BlendAlpha
                ;

            uint elem_offset = 0;
            for(int cmd_i = 0; cmd_i < cmd_list->CmdBuffer.Size; cmd_i++)
            {
                DrawCmd* pcmd = &(((DrawCmd*)cmd_list->CmdBuffer.Data)[cmd_i]);
                if(pcmd->UserCallback != IntPtr.Zero)
                {
                    elem_offset += pcmd->ElemCount;
                    throw new NotImplementedException();
                    //pcmd->UserCallback(cmd_list, pcmd);
            
                    continue;
                }

                if(0 == pcmd->ElemCount)
                    continue;

                Bgfx.SetScissor((ushort)(Math.Max(pcmd->ClipRect.X, 0.0f)),
                    (ushort)(Math.Max(pcmd->ClipRect.Y, 0.0f)),
                    (ushort)(Math.Min(pcmd->ClipRect.Z, 65535.0f) - Math.Max(pcmd->ClipRect.X, 0.0f)),
                    (ushort)(Math.Min(pcmd->ClipRect.W, 65535.0f) - Math.Max(pcmd->ClipRect.Y, 0.0f)));
                
                Texture texture = fontTex_;
                if(pcmd->TextureId != null)
                { 
                    IntPtr texID = pcmd->TextureId;
                }

                Bgfx.SetTexture(0, uiShader_.TextureSlot[0].UniformHandle, texture);

                graphics.DrawTransient(
                    viewId_,
                    vertex_buffer,
                    index_buffer,
                    Matrix.Identity,
                    (int)elem_offset,
                    (int)pcmd->ElemCount,
                    state,
                    uiShaderInstance_);

                elem_offset += pcmd->ElemCount;
            }

        }

        private unsafe void UpdateImGuiInput()
        {
            IO io = ImGui.GetIO();

            Input input = GetSubsystem<Input>();

            ImVec2 mousePosition = EditorUtil.Convert(input.MousePosition);

            io.MousePosition = mousePosition;
            io.MouseDown[0] = input.IsMouseDown(MouseButton.Left);
            io.MouseDown[1] = input.IsMouseDown(MouseButton.Right);
            io.MouseDown[2] = input.IsMouseDown(MouseButton.Middle);

            float delta = input.WheelDelta;
            io.MouseWheel = delta;

            ImGui.GetIO().MouseWheel = delta;

            IReadOnlyList<char> keyCharPresses = input.KeyCharPresses;
            for(int i = 0; i < keyCharPresses.Count; i++)
            {
                char c = keyCharPresses[i];
                ImGui.AddInputCharacter(c);
            }

            bool controlDown = false;
            bool shiftDown = false;
            bool altDown = false;

            IReadOnlyList<KeyEvent> keyEvents = input.KeyEvents;
            for(int i = 0; i < keyEvents.Count; i++)
            {
                KeyEvent keyEvent = keyEvents[i];
                io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
                if(keyEvent.Key == Key.ControlLeft)
                {
                    controlDown = keyEvent.Down;
                }
                if(keyEvent.Key == Key.ShiftLeft)
                {
                    shiftDown = keyEvent.Down;
                }
                if(keyEvent.Key == Key.AltLeft)
                {
                    altDown = keyEvent.Down;
                }
            }

            io.CtrlPressed = controlDown;
            io.AltPressed = altDown;
            io.ShiftPressed = shiftDown;
        }

    }
}
