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
        public static bool Begin(string Title, float X, float Y, float W, float H, nk_panel_flags Flags) => NuklearNative.nk_begin/*_titled*/(ctx, //(byte*)Marshal.StringToHGlobalAnsi(Name),
           (byte*)Marshal.StringToHGlobalAnsi(Title), new nk_rect { x = X, y = Y, w = W, h = H }, (uint)Flags) != 0;
        public static void End() => NuklearNative.nk_end(ctx);

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

        public static bool BeginGroup(string Name, string Title, nk_panel_flags Flags)
            => NuklearNative.nk_group_begin_titled(ctx, (byte*)Marshal.StringToHGlobalAnsi(Name), (byte*)Marshal.StringToHGlobalAnsi(Title), (uint)Flags) != 0;

        public static void EndGroup()
            => NuklearNative.nk_group_end(ctx);

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


}
