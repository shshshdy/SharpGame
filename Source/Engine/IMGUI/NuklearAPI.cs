using NuklearSharp;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void FontStashAction(IntPtr Atlas);

    public unsafe partial class ImGUI
    {
        public static nk_context* ctx;
        //public static NuklearDevice Dev;

        static nk_allocator* Allocator;
        static nk_font_atlas* FontAtlas;
        static nk_draw_null_texture* NullTexture;
        static nk_convert_config* ConvertCfg;

        static nk_buffer* Commands, Vertices, Indices;
        static byte[] LastMemory;

        static nk_draw_vertex_layout_element* VertexLayout;
        static nk_plugin_alloc_t Alloc;
        static nk_plugin_free_t Free;

        //static IFrameBuffered FrameBuffered;

        static bool ForceUpdateQueued;

        static bool Initialized = false;


        static IntPtr ManagedAlloc(IntPtr Size, bool ClearMem = true)
        {
            if(ClearMem)
            return Utilities.AllocateAndClear(Size.ToInt32(), 0);
            else
                return Utilities.Allocate(Size.ToInt32());
        }

        static IntPtr ManagedAlloc(int Size)
        {
            return Utilities.Allocate(Size);
        }

        static void ManagedFree(IntPtr Mem)
        {
            //Marshal.FreeHGlobal(Mem);
            Utilities.Free(Mem);
        }

        void FontStash()
        {
            NuklearNative.nk_font_atlas_init(FontAtlas, Allocator);
            NuklearNative.nk_font_atlas_begin(FontAtlas);

            //A?.Invoke(new IntPtr(FontAtlas));
            FontStash(new IntPtr(FontAtlas));

            int W, H;
            IntPtr Image = NuklearNative.nk_font_atlas_bake(FontAtlas, &W, &H, nk_font_atlas_format.NK_FONT_ATLAS_RGBA32);
            int TexHandle = CreateTextureHandle(W, H, Image);

            NuklearNative.nk_font_atlas_end(FontAtlas, NuklearNative.nk_handle_id(TexHandle), NullTexture);

            if (FontAtlas->default_font != null)
                NuklearNative.nk_style_set_font(ctx, &FontAtlas->default_font->handle);
        }


        private void Init()
        {
            if (Initialized)
                throw new InvalidOperationException("NuklearAPI.Init is called twice");

            Initialized = true;

            // TODO: Free these later
            ctx = (nk_context*)ManagedAlloc(sizeof(nk_context));
            Allocator = (nk_allocator*)ManagedAlloc(sizeof(nk_allocator));
            FontAtlas = (nk_font_atlas*)ManagedAlloc(sizeof(nk_font_atlas));
            NullTexture = (nk_draw_null_texture*)ManagedAlloc(sizeof(nk_draw_null_texture));
            ConvertCfg = (nk_convert_config*)ManagedAlloc(sizeof(nk_convert_config));
            Commands = (nk_buffer*)ManagedAlloc(sizeof(nk_buffer));
            Vertices = (nk_buffer*)ManagedAlloc(sizeof(nk_buffer));
            Indices = (nk_buffer*)ManagedAlloc(sizeof(nk_buffer));

            VertexLayout = (nk_draw_vertex_layout_element*)ManagedAlloc(sizeof(nk_draw_vertex_layout_element) * 4);
            VertexLayout[0] = new nk_draw_vertex_layout_element{
                attribute = nk_draw_vertex_layout_attribute.NK_VERTEX_POSITION,
                format = nk_draw_vertex_layout_format.NK_FORMAT_FLOAT,
                offset_nksize = Marshal.OffsetOf(typeof(NkVertex), nameof(NkVertex.Position)) };
            VertexLayout[1] = new nk_draw_vertex_layout_element{
                attribute = nk_draw_vertex_layout_attribute.NK_VERTEX_TEXCOORD,
                format = nk_draw_vertex_layout_format.NK_FORMAT_FLOAT,
                offset_nksize = Marshal.OffsetOf(typeof(NkVertex), nameof(NkVertex.UV)) };
            VertexLayout[2] = new nk_draw_vertex_layout_element{
                attribute = nk_draw_vertex_layout_attribute.NK_VERTEX_COLOR,
                format = nk_draw_vertex_layout_format.NK_FORMAT_R8G8B8A8,
                offset_nksize = Marshal.OffsetOf(typeof(NkVertex), nameof(NkVertex.Color)) };
            VertexLayout[3] = new nk_draw_vertex_layout_element {
                attribute = nk_draw_vertex_layout_attribute.NK_VERTEX_ATTRIBUTE_COUNT,
                format = nk_draw_vertex_layout_format.NK_FORMAT_COUNT,
                offset_nksize = IntPtr.Zero};

            Alloc = (Handle, Old, Size) => ManagedAlloc(Size);
            Free = (Handle, Old) => ManagedFree(Old);
            
            Allocator->alloc_nkpluginalloct = Marshal.GetFunctionPointerForDelegate(Alloc);
            Allocator->free_nkpluginfreet = Marshal.GetFunctionPointerForDelegate(Free);

            NuklearNative.nk_init(ctx, Allocator, null);

            CreateGraphicsResource();

            FontStash();

            ConvertCfg->shape_AA = nk_anti_aliasing.NK_ANTI_ALIASING_ON;
            ConvertCfg->line_AA = nk_anti_aliasing.NK_ANTI_ALIASING_ON;
            ConvertCfg->vertex_layout = VertexLayout;
            ConvertCfg->vertex_size = new IntPtr(sizeof(NkVertex));
            ConvertCfg->vertex_alignment = new IntPtr(1);
            ConvertCfg->circle_segment_count = 22;
            ConvertCfg->curve_segment_count = 22;
            ConvertCfg->arc_segment_count = 22;
            ConvertCfg->global_alpha = 1.0f;
            ConvertCfg->null_tex = *NullTexture;

            NuklearNative.nk_buffer_init(Commands, Allocator, new IntPtr(4 * 1024));
            NuklearNative.nk_buffer_init(Vertices, Allocator, new IntPtr(4 * 1024));
            NuklearNative.nk_buffer_init(Indices, Allocator, new IntPtr(4 * 1024));
        }

        public static void SetDeltaTime(float Delta)
        {
            if (ctx != null)
                ctx->delta_time_Seconds = Delta;
        }

        public static bool Window(string Name, string Title, float X, float Y, float W, float H, nk_panel_flags Flags, Action A)
        {
            bool Res = true;

            if (NuklearNative.nk_begin/*_titled*/(ctx, //(byte*)Marshal.StringToHGlobalAnsi(Name),
                (byte*)Marshal.StringToHGlobalAnsi(Title), new nk_rect { x = X, y = Y, w = W, h = H }, (uint)Flags) != 0)
                A?.Invoke();
            else
                Res = false;

            NuklearNative.nk_end(ctx);
            return Res;
        }

        public static bool Window(string Title, float X, float Y, float W, float H, nk_panel_flags Flags, Action A) => Window(Title, Title, X, Y, W, H, Flags, A);

        public static bool WindowIsClosed(string Name) => NuklearNative.nk_window_is_closed(ctx, (byte*)Marshal.StringToHGlobalAnsi(Name)) != 0;

        public static bool WindowIsHidden(string Name) => NuklearNative.nk_window_is_hidden(ctx, (byte*)Marshal.StringToHGlobalAnsi(Name)) != 0;

        public static bool WindowIsCollapsed(string Name) => NuklearNative.nk_window_is_collapsed(ctx, (byte*)Marshal.StringToHGlobalAnsi(Name)) != 0;

        public static bool Group(string Name, string Title, nk_panel_flags Flags, Action A)
        {
            bool Res = true;

            if (NuklearNative.nk_group_begin_titled(ctx, (byte*)Marshal.StringToHGlobalAnsi(Name), (byte*)Marshal.StringToHGlobalAnsi(Title), (uint)Flags) != 0)
                A?.Invoke();
            else
                Res = false;

            NuklearNative.nk_group_end(ctx);
            return Res;
        }

        public static bool Group(string Name, nk_panel_flags Flags, Action A) => Group(Name, Name, Flags, A);

        public static bool ButtonLabel(string Label)
        {
            return NuklearNative.nk_button_label(ctx, (byte*)Marshal.StringToHGlobalAnsi(Label)) != 0;
        }

        public static bool ButtonText(string Text)
        {
            return NuklearNative.nk_button_text(ctx, (byte*)Marshal.StringToHGlobalAnsi(Text), Text.Length) != 0;
        }

        public static bool ButtonText(char Char) => ButtonText(Char.ToString());

        public static void LayoutRowStatic(float Height, int ItemWidth, int Cols)
        {
            NuklearNative.nk_layout_row_static(ctx, Height, ItemWidth, Cols);
        }

        public static void LayoutRowDynamic(float Height = 0, int Cols = 1)
        {
            NuklearNative.nk_layout_row_dynamic(ctx, Height, Cols);
        }

        public static void Label(string Txt, nk_text_align TextAlign = (nk_text_align)nk_text_alignment.NK_TEXT_LEFT)
        {
            NuklearNative.nk_label(ctx, (byte*)Marshal.StringToHGlobalAnsi(Txt), (uint)TextAlign);
        }

        public static void LabelWrap(string Txt)
        {
            //NuklearNative.nk_label(Ctx, Txt, (uint)TextAlign);
            NuklearNative.nk_label_wrap(ctx, (byte*)Marshal.StringToHGlobalAnsi(Txt));
        }

        public static void LabelColored(string Txt, nk_color Clr, nk_text_align TextAlign = (nk_text_align)nk_text_alignment.NK_TEXT_LEFT)
        {
            NuklearNative.nk_label_colored(ctx, (byte*)Marshal.StringToHGlobalAnsi(Txt), (uint)TextAlign, Clr);
        }

        public static void LabelColored(string Txt, byte R, byte G, byte B, byte A, nk_text_align TextAlign = (nk_text_align)nk_text_alignment.NK_TEXT_LEFT)
        {
            //NuklearNative.nk_label_colored(Ctx, Txt, (uint)TextAlign, new nk_color() { r = R, g = G, b = B, a = A });
            LabelColored(Txt, new nk_color() { r = R, g = G, b = B, a = A }, TextAlign);
        }

        public static void LabelColoredWrap(string Txt, nk_color Clr)
        {
            NuklearNative.nk_label_colored_wrap(ctx, (byte*)Marshal.StringToHGlobalAnsi(Txt), Clr);
        }

        public static void LabelColoredWrap(string Txt, byte R, byte G, byte B, byte A)
        {
            LabelColoredWrap(Txt, new nk_color { r = R, g = G, b = B, a = A });
        }

        public static nk_rect WindowGetBounds()
        {
            return NuklearNative.nk_window_get_bounds(ctx);
        }
        /*
        public static NkEditEvents EditString(NkEditTypes EditType, StringBuilder Buffer, nk_plugin_filter_t Filter)
        {
            return (NkEditEvents)NuklearNative.nk_edit_string_zero_terminated(Ctx, (uint)EditType, Buffer, Buffer.MaxCapacity, Filter);
        }

        public static NkEditEvents EditString(NkEditTypes EditType, StringBuilder Buffer)
        {
            return EditString(EditType, Buffer, (ref nk_text_edit TextBox, uint Rune) => 1);
        }*/

        public static bool IsKeyPressed(nk_keys Key)
        {
            //NuklearNative.nk_input_is_key_pressed()
            return NuklearNative.nk_input_is_key_pressed(&ctx->input, Key) != 0;
        }

        public static void QueueForceUpdate()
        {
            ForceUpdateQueued = true;
        }

        public static void WindowClose(string Name)
        {
            NuklearNative.nk_window_close(ctx, (byte*)Marshal.StringToHGlobalAnsi(Name));
        }

        public static void SetClipboardCallback(Action<string> CopyFunc, Func<string> PasteFunc)
        {
            // TODO: Contains alloc and forget, don't call SetClipboardCallback too many times


            nk_plugin_copy_t NkCopyFunc = (Handle, Str, Len) => {
                byte[] Bytes = new byte[Len];

                for (int i = 0; i < Bytes.Length; i++)
                    Bytes[i] = Str[i];

                CopyFunc(Encoding.UTF8.GetString(Bytes));
            };

            nk_plugin_paste_t NkPasteFunc = (IntPtr Handle, ref nk_text_edit TextEdit) => {
                byte[] Bytes = Encoding.UTF8.GetBytes(PasteFunc());

                fixed (byte* BytesPtr = Bytes)
                fixed (nk_text_edit* TextEditPtr = &TextEdit)
                    NuklearNative.nk_textedit_paste(TextEditPtr, BytesPtr, Bytes.Length);
            };

            GCHandle.Alloc(CopyFunc);
            GCHandle.Alloc(PasteFunc);
            GCHandle.Alloc(NkCopyFunc);
            GCHandle.Alloc(NkPasteFunc);

            ctx->clip.copyfun_nkPluginCopyT = Marshal.GetFunctionPointerForDelegate(NkCopyFunc);
            ctx->clip.pastefun_nkPluginPasteT = Marshal.GetFunctionPointerForDelegate(NkPasteFunc);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NkVertex
    {
        public nk_vec2 Position;
        public nk_vec2 UV;
        public nk_color Color;

        public override string ToString()
        {
            return string.Format("Position: {0}; UV: {1}; Color: {2}", Position, UV, Color);
        }
    }

    public struct NuklearEvent
    {
        public enum EventType
        {
            MouseButton,
            MouseMove,
            Scroll,
            Text,
            KeyboardKey,
            ForceUpdate
        }

        public enum MouseButton
        {
            Left, Middle, Right
        }

        public EventType EvtType;
        public MouseButton MButton;
        public nk_keys Key;
        public int X, Y;
        public bool Down;
        public float ScrollX, ScrollY;
        public string Text;
    }

    public interface IFrameBuffered
    {
        void BeginBuffering();
        void EndBuffering();
        void RenderFinal();
    }

}
