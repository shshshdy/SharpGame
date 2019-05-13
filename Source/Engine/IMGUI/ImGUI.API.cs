using NuklearSharp;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using static NuklearSharp.NuklearNative;

namespace SharpGame
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void FontStashAction(IntPtr Atlas);

    public unsafe partial class ImGUI
    {
        static byte* _T(string str)
        {
            return (byte*)Marshal.StringToHGlobalAnsi(str);
        }

        static char* _TChar(string str)
        {
            return (char*)Marshal.StringToHGlobalUni(str);
        }

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



        public static nk_vec2 WindowGetPosition() => nk_window_get_position(ctx);
        public static nk_vec2 WindowGetSize() => nk_window_get_size(ctx);
        public static float WindowGetWidth() => nk_window_get_width(ctx);
        public static float WindowGetHeight() => nk_window_get_height(ctx);
        public static nk_rect WindowGetContentRegion() => nk_window_get_content_region(ctx);
        public static nk_vec2 WindowGetContentRegionMin() => nk_window_get_content_region_min(ctx);
        public static nk_vec2 WindowGetContentRegionMax() => nk_window_get_content_region_max(ctx);
        public static nk_vec2 WindowGetContentRegionSize() => nk_window_get_content_region_size(ctx);

        public static bool WindowHasFocus() => nk_window_has_focus(ctx) != 0;
        public static bool WindowIsActive(string name) => nk_window_is_active(ctx, (byte*)Marshal.StringToHGlobalAnsi(name)) != 0;
        public static bool WindowIsHoverd(string name) => nk_window_is_hovered(ctx) != 0;
        public static bool WindowIsAnyHovered(string name) => nk_window_is_any_hovered(ctx) != 0;
        public static bool WindowIsAnyActive(string name) => nk_item_is_any_active(ctx) != 0;
        public static void WindowSetBounds(string name, nk_rect bounds) => nk_window_set_bounds(ctx, (byte*)Marshal.StringToHGlobalAnsi(name), bounds);
        public static void WindowSetPosition(string name, nk_vec2 pos) => nk_window_set_position(ctx, (byte*)Marshal.StringToHGlobalAnsi(name), pos);
        public static void WindowSetSize(string name, nk_vec2 sz) => nk_window_set_size(ctx, (byte*)Marshal.StringToHGlobalAnsi(name), sz);
        public static void WindowSetFocus(string name) => nk_window_set_focus(ctx, (byte*)Marshal.StringToHGlobalAnsi(name));
        public static void WindowCollapse(string name, nk_collapse_states state) => nk_window_collapse(ctx, (byte*)Marshal.StringToHGlobalAnsi(name), state);
        public static void WindowCollapseIf(string name, nk_collapse_states state, int cond) => nk_window_collapse_if(ctx, (byte*)Marshal.StringToHGlobalAnsi(name), state, cond);
        public static void WindowShow(string name, nk_show_states state) => nk_window_show(ctx, (byte*)Marshal.StringToHGlobalAnsi(name), state);
        public static void WindowShowIf(string name, nk_show_states state, int cond) => nk_window_show_if(ctx, (byte*)Marshal.StringToHGlobalAnsi(name), state, cond);

        public static void LayoutSetMinRowHeight(float height) => nk_layout_set_min_row_height(ctx, height);
        public static void LayoutResetMinRowHeight(float height) => nk_layout_reset_min_row_height(ctx);
        public static nk_rect LayoutWidgetBounds() => nk_layout_widget_bounds(ctx);
        public static float LayoutRatioFromPixel(float pixel_width) => nk_layout_ratio_from_pixel(ctx, pixel_width);
        public static void LayoutRowBegin(nk_layout_format fmt, float row_height, int cols) => nk_layout_row_begin(ctx, fmt, row_height, cols);
        public static void LayoutRowPush(float value) => nk_layout_row_push(ctx, value);
        public static void LayoutRowEnd() => nk_layout_row_end(ctx);
        public static void LayoutRow(nk_layout_format fmt, float height, int cols, IntPtr ratio) => nk_layout_row(ctx, fmt, height, cols, (float*)ratio);
        public static void LayoutRowTemplateBegin(float row_height) => nk_layout_row_template_begin(ctx, row_height);
        public static void LayoutRowTemplatePushDynamic() => nk_layout_row_template_push_dynamic(ctx);
        public static void LayoutRowTemplatePushVariable(float min_width) => nk_layout_row_template_push_variable(ctx, min_width);
        public static void LayoutRowTemplatePushStatic(float width) => nk_layout_row_template_push_static(ctx, width);
        public static void LayoutRowTemplateEnd() => nk_layout_row_template_end(ctx);
        public static void LayoutSpaceBegin(nk_layout_format fmt, float height, int widget_count) => nk_layout_space_begin(ctx, fmt, height, widget_count);
        public static void LayoutSpacePush(nk_rect rect) => nk_layout_space_push(ctx, rect);
        public static void LayoutSpaceEnd() => nk_layout_space_end(ctx);
        public static nk_rect LayoutSpaceBounds() => nk_layout_space_bounds(ctx);
        public static nk_vec2 LayoutSpaceToScreen(nk_vec2 pos) => nk_layout_space_to_screen(ctx, pos);
        public static nk_vec2 LayoutSpaceToLocal(nk_vec2 pos) => nk_layout_space_to_local(ctx, pos);
        public static nk_rect LayoutSpaceRectToScreen(nk_rect rect) => nk_layout_space_rect_to_screen(ctx, rect);
        public static nk_rect LayoutSpaceRectToLocal(nk_rect rect) => nk_layout_space_rect_to_local(ctx, rect);

        public static int GroupBegin(string title, uint flag) => nk_group_begin(ctx, _T(title), flag);
        public static int GroupScrolledOffsetBegin(ref uint x_offset, ref uint y_offset, string title, uint flag) 
            => nk_group_scrolled_offset_begin(ctx, (uint*)Utilities.As(ref x_offset), (uint*)Utilities.As(ref y_offset), _T(title), flag);
        public static int GroupScrolledBegin(ref nk_scroll scr, string title, uint flag) => nk_group_scrolled_begin(ctx,
            (nk_scroll*)Utilities.As(ref scr), _T(title), flag);
        public static void GroupScrolledEnd() => nk_group_scrolled_end(ctx);
        public static void GroupEnd() => nk_group_end(ctx);

/*
        public static int ListViewBegin(ref nk_list_view outlv, string id, uint flag, int row_height, int row_count)
            => nk_list_view_begin(ctx, (nk_list_view*)Utilities.As(ref outlv), id, flag, row_height, row_count);
        public static void ListViewEnd(ref nk_list_view outlv) => nk_list_view_end((nk_list_view*)Utilities.As(ref outlv));

        public static int TreePushHashed(nk_tree_type type, string title, nk_collapse_states initial_state, string hash, int len, int seed)
            => nk_tree_push_hashed(ctx, type, title, initial_state, hash, len, seed);

        //#define nk_tree_image_push(ctx, type, img, title, state) nk_tree_image_push_hashed(ctx, type, img, title, state, NK_FILE_LINE,nk_strlen(NK_FILE_LINE),__LINE__)
        //#define nk_tree_image_push_id(ctx, type, img, title, state, id) nk_tree_image_push_hashed(ctx, type, img, title, state, NK_FILE_LINE,nk_strlen(NK_FILE_LINE),id)

        public static int TreeImagePushHashed(nk_tree_type type, nk_image image, string title, nk_collapse_states initial_state, string hash, int len, int seed)
            => nk_tree_image_push_hashed(ctx, type, image, title, initial_state, hash, len, seed);

        public static void TreePop() => nk_tree_pop(ctx);

        public static int TreeStatePush(nk_tree_type type, string title, ref nk_collapse_states state)
            => nk_tree_state_push(ctx, type, title, (nk_collapse_states*)Utilities.As(ref state));

        public static unsafe int TreeStateImagePush(nk_tree_type type, nk_image image, string title, ref nk_collapse_states state)
            => nk_tree_state_image_push(ctx, type, image, title, (nk_collapse_states*)Utilities.As(ref state));

        public static void TreeStatePop() => nk_tree_state_pop(ctx);

        public static nk_widget_layout_states Widget(ref nk_rect rc)
            => nk_widget(ref rc, ctx);
        public static nk_widget_layout_states WidgetFitting(ref nk_rect rc, nk_vec2 p)
            => nk_widget_fitting(ref rc, ctx, p);
        public static nk_rect WidgetBounds() => nk_widget_bounds(ctx);
        public static nk_vec2 WidgetPosition() => nk_widget_position(ctx);
        public static nk_vec2 WidgetSize() => nk_widget_size(ctx);
        public static float WidgetWidth() => nk_widget_width(ctx);
        public static float WidgetHeight() => nk_widget_height(ctx);
        public static bool WidgetIsHovered() => nk_widget_is_hovered(ctx) != 0;
        public static bool WidgetIsMouseClicked(nk_buttons btn) => nk_widget_is_mouse_clicked(ctx, btn) != 0;
        public static bool WidgetHasMouseClickDown(nk_buttons btn, int down)
            => nk_widget_has_mouse_click_down(ctx, btn, down) != 0;
        public static void Spacing(int cols) => nk_spacing(ctx, cols);

        public static void Text(string text, nk_text_alignment flag = nk_text_alignment.NK_TEXT_LEFT) => nk_text(ctx, text, text.Length, (uint)flag);
        public static void Text(string text, nk_text_alignment flag, nk_color c) => nk_text_colored(ctx, text, text.Length, (uint)flag, c);
        public static void TextWrap(string text) => nk_text_wrap(ctx, text, text.Length);
        public static void TextWrap(string text, nk_color c) => nk_text_wrap_colored(ctx, text, text.Length, c);
        public static void Label(string label, nk_text_alignment align = nk_text_alignment.NK_TEXT_LEFT) => nk_label(ctx, label, (uint)align);
        public static void Label(string label, nk_text_alignment align, nk_color c) => nk_label_colored(ctx, label, (uint)align, c);
        public static void LabelWrap(string label) => nk_label_wrap(ctx, label);
        public static void LabelWrap(string label, nk_color c) => nk_label_colored_wrap(ctx, label, c);
        */
        public static void Image(nk_image image) => nk_image(ctx, image);
        /*
        public static void Value(string prefix, bool v) => nk_value_bool(ctx, prefix, v ? 1 : 0);
        public static void Value(string prefix, int v) => nk_value_int(ctx, prefix, v);
        public static void Value(string prefix, uint v) => nk_value_uint(ctx, prefix, v);
        public static void Value(string prefix, float v) => nk_value_float(ctx, prefix, v);
        public static void ValueColorByte(string prefix, nk_color c) => nk_value_color_byte(ctx, prefix, c);
        public static void ValueColorFloat(string prefix, nk_color c) => nk_value_color_float(ctx, prefix, c);
        public static void ValueColorHex(string prefix, nk_color c) => nk_value_color_hex(ctx, prefix, c);
        */
        public static bool Button(string text) => nk_button_text(ctx, _T(text), text.Length) != 0;
        public static bool Button(char c) => Button(c.ToString());
        public static bool Button(nk_color c) => nk_button_color(ctx, c) != 0;
        public static bool Button(nk_symbol_type t) => nk_button_symbol(ctx, t) != 0;
        public static bool Button(nk_image img) => nk_button_image(ctx, img) != 0;
        /*
        public static bool Button(nk_symbol_type t, string text, uint alignment) =>
            nk_button_symbol_text(ctx, t, text, text.Length, alignment) != 0;
        public static bool Button(nk_image img, string text, uint alignment) =>
            nk_button_image_text(ctx, img, text, text.Length, alignment) != 0;
        public static bool ButtonTextStyled(nk_style_button* btn, string title, int len)
            => nk_button_text_styled(ctx, btn, title, len) != 0;
        public static bool ButtonLabelStyled(nk_style_button* btn, string title) =>
            nk_button_label_styled(ctx, btn, title) != 0;
        public static bool ButtonSymbolStyled(nk_style_button* btn, nk_symbol_type t)
            => nk_button_symbol_styled(ctx, btn, t) != 0;
        public static bool ButtonImageStyled(nk_style_button* btn, nk_image img)
            => nk_button_image_styled(ctx, btn, img) != 0;
        public static bool ButtonSymbolTextStyled(ref nk_style_button btn, nk_symbol_type t, string text, uint alignment)
            => nk_button_symbol_text_styled(ctx, (nk_style_button*)Utilities.As(ref btn), t, text, text.Length, alignment) != 0;
        public static bool ButtonSymbolLabelStyled(ref nk_style_button style, nk_symbol_type symbol, string title, uint align)
            => nk_button_symbol_label_styled(ctx, (nk_style_button*)Utilities.As(ref style), symbol, title, align) != 0;
        public static bool ButtonImageLabelStyled(ref nk_style_button style, nk_image img, string title, uint text_alignment)
            => nk_button_image_label_styled(ctx, (nk_style_button*)Utilities.As(ref style), img, title, text_alignment) != 0;
        public static bool ButtonImageTextStyled(ref nk_style_button style, nk_image img, string title, uint alignment)
            => nk_button_image_text_styled(ctx, (nk_style_button*)Utilities.As(ref style), img, title, title.Length, alignment) != 0;
        public static void ButtonSetBehavior(nk_button_behavior b) => nk_button_set_behavior(ctx, b);
        public static bool ButtonPushBehavior(nk_button_behavior b) => nk_button_push_behavior(ctx, b) != 0;
        public static bool ButtonPopBehavior() => nk_button_pop_behavior(ctx) != 0;
        public static int CheckLabel(string label, int active) => nk_check_label(ctx, label, active);
        public static int CheckText(string label, int active) => nk_check_text(ctx, label, label.Length, active);
        public static uint CheckFlagsLabel(string label, uint flags, uint value) => nk_check_flags_label(ctx, label, flags, value);
        public static uint CheckFlagsText(string label, int p1, uint flags, uint value) => nk_check_flags_text(ctx, label, p1, flags, value);
        public static int CheckboxLabel(string label, ref int active) => nk_checkbox_label(ctx, label, (int*)Utilities.As(ref active));
        public static int CheckboxText(string label, int p1, ref int active) => nk_checkbox_text(ctx, label, p1, (int*)Utilities.As(ref active));
        public static int CheckboxFlagsLabel(string label, ref uint flags, uint value) => nk_checkbox_flags_label(ctx, label, (uint*)Utilities.As(ref flags), value);
        public static int CheckboxFlagsText(string label, ref uint flags, uint value) => nk_checkbox_flags_text(ctx, label, label.Length, (uint*)Utilities.As(ref flags), value);
        public static int Radio(string label, ref int active) => nk_radio_text(ctx, label, label.Length, (int*)Utilities.As(ref active));
        public static bool Option(string label, bool active) => nk_option_text(ctx, label, label.Length, active ? 1 : 0) != 0;
        public static int Selectable(string label, uint align, ref int value) => nk_selectable_text(ctx, label, label.Length, align, (int*)Utilities.As(ref value));
        public static int Selectable(nk_image img, string label, uint align, ref int value) => nk_selectable_image_label(ctx, img, label, align, (int*)Utilities.As(ref value));
        public static int Select(string label, uint align, int value) => nk_select_label(ctx, label, align, value);
        public static int Select(nk_image img, string label, uint align, int value) => nk_select_image_label(ctx, img, label, align, value);
        public static float Slide(float min, float val, float max, float step) => nk_slide_float(ctx, min, val, max, step);
        public static int Slide(int min, int val, int max, int step) => nk_slide_int(ctx, min, val, max, step);
        public static int Slider(float min, ref float val, float max, float step) => nk_slider_float(ctx, min, (float*)Utilities.As(ref val), max, step);
        public static int Slider(int min, ref int val, int max, int step) => nk_slider_int(ctx, min, (int*)Utilities.As(ref val), max, step);
        public static int Progress(ref int cur, int max, int modifyable) => nk_progress(ctx, (int*)Utilities.As(ref cur), max, modifyable);
        public static int Prog(int cur, int max, int modifyable) => nk_prog(ctx, cur, max, modifyable);
        */

        public static nk_colorf ColorPicker(Color c, nk_color_format format) 
            => nk_color_picker(ctx, new nk_colorf { r = c.R, g = c.G, b = c.B, a = c.A}, (nk_color_format)format);
        public static bool ColorPick(ref Color c, nk_color_format format) => nk_color_pick(ctx, (nk_colorf*)Utilities.As(ref c), format) != 0;
        public static void PropertyInt(string name, int min, ref int val, int max, int step, float inc_per_pixel) => nk_property_int(ctx, (byte*)Marshal.StringToHGlobalAnsi(name), min, (int*)Utilities.As(ref val), max, step, inc_per_pixel);
        public static void PropertyFloat(string name, float min, ref float val, float max, float step, float inc_per_pixel) => nk_property_float(ctx, (byte*)Marshal.StringToHGlobalAnsi(name), min, (float*)Utilities.As(ref val), max, step, inc_per_pixel);
        public static void PropertyDouble(string name, double min, ref double val, double max, double step, float inc_per_pixel) => nk_property_double(ctx, (byte*)Marshal.StringToHGlobalAnsi(name), min, (double*)Utilities.As(ref val), max, step, inc_per_pixel);
        public static int Propertyi(string name, int min, int val, int max, int step, float inc_per_pixel) => nk_propertyi(ctx, (byte*)Marshal.StringToHGlobalAnsi(name), min, val, max, step, inc_per_pixel);
        public static float Propertyf(string name, float min, float val, float max, float step, float inc_per_pixel) => nk_propertyf(ctx, (byte*)Marshal.StringToHGlobalAnsi(name), min, val, max, step, inc_per_pixel);
        public static double Propertyd(string name, double min, double val, double max, double step, float inc_per_pixel) => nk_propertyd(ctx, (byte*)Marshal.StringToHGlobalAnsi(name), min, val, max, step, inc_per_pixel);

#if false
        public static uint Edit(uint p, StringBuilder buffer, ref int len, nk_plugin_filter_t filter)
                => nk_edit_string(ctx, p, buffer, (int*)Utilities.As(ref len), buffer.MaxCapacity, filter);
        public static uint Edit(uint p, StringBuilder buffer, nk_plugin_filter_t filter)
                => nk_edit_string_zero_terminated(ctx, p, buffer, buffer.MaxCapacity, filter);
        public static uint Edit_buffer(uint p, IntPtr text_edit/*nk_text_edit**/, nk_plugin_filter_t filter)
                => nk_edit_buffer(ctx, p, text_edit, filter);
        public static void EditFocus(uint flags) => nk_edit_focus(ctx, flags);
        public static void EditUnfocus() => nk_edit_unfocus(ctx);

        public static int ChartBegin(nk_chart_type chart_type, int num, float min, float max)
            => nk_chart_begin(ctx, chart_type, num, min, max);
        public static int ChartBegin(nk_chart_type chart_type, nk_color c, nk_color active, int num, float min, float max)
            => nk_chart_begin_colored(ctx, chart_type, c, active, num, min, max);
        public static void ChartAddSlot(nk_chart_type chart_type, int count, float min_value, float max_value)
            => nk_chart_add_slot(ctx, chart_type, count, min_value, max_value);
        public static void ChartAddSlot(nk_chart_type chart_type, nk_color c, nk_color active, int count, float min_value, float max_value)
            => nk_chart_add_slot_colored(ctx, chart_type, c, active, count, min_value, max_value);
        public static uint ChartPush(float p1) => nk_chart_push(ctx, p1);
        public static uint ChartPushSlot(float p1, int p2) => nk_chart_push_slot(ctx, p1, p2);
        public static void ChartEnd() => nk_chart_end(ctx);
        public static void Plot(nk_chart_type chart_type, IntPtr values, int count, int offset)
            => nk_plot(ctx, chart_type, values, count, offset);
        public static void PlotFunction(nk_chart_type chart_type, IntPtr userdata, value_getter val_getter, int count, int offset) =>
            nk_plot_function(ctx, chart_type, userdata, val_getter, count, offset);
        public static int PopupBegin(nk_popup_type type, string text, uint p1, nk_rect bounds) => nk_popup_begin(ctx, type, text, p1, bounds);
        public static void PopupClose() => nk_popup_close(ctx);
        public static void PopupEnd() => nk_popup_end(ctx);
#endif
        //public static int Combo(string[] items, int selected, int item_height, nk_vec2 size) => nk_combo(ctx, items, items.Length, selected, item_height, size);

        public static bool ComboBegin(string selected, nk_vec2 size) => nk_combo_begin_text(ctx, _TChar(selected), selected.Length, size) != 0;
        public static bool ComboBegin(nk_color color, nk_vec2 size) => nk_combo_begin_color(ctx, color, size) != 0;
        public static bool ComboBegin(nk_symbol_type symbol, nk_vec2 size) => nk_combo_begin_symbol(ctx, symbol, size) != 0;
        public static bool ComboBegin(string selected, nk_symbol_type symbol, nk_vec2 size) => nk_combo_begin_symbol_text(ctx, _TChar(selected), selected.Length, symbol, size) != 0;
        public static bool ComboBegin(nk_image img, nk_vec2 size) => nk_combo_begin_image(ctx, img, size) != 0;
        public static bool ComboBegin(string selected, nk_image image, nk_vec2 size) => nk_combo_begin_image_text(ctx, _TChar(selected), selected.Length, image, size) != 0;
        public static bool ComboItem(string text, uint alignment) => nk_combo_item_text(ctx, _T(text), text.Length, alignment) != 0;
        public static bool ComboItem(nk_image image, string label, uint alignment) => nk_combo_item_image_text(ctx, image, _T(label), label.Length, alignment) != 0;
        public static bool ComboItem(nk_symbol_type symbol, string text, uint alignment) => nk_combo_item_symbol_text(ctx, symbol, _T(text), text.Length, alignment) != 0;
        public static void ComboClose() => nk_combo_close(ctx);
        public static void ComboEnd() => nk_combo_end(ctx);

        public static int ContextualBegin(uint p1, nk_vec2 p2, nk_rect trigger_bounds)
            => nk_contextual_begin(ctx, p1, p2, trigger_bounds);
        public static int ContextualItem(string label, uint align)
            => nk_contextual_item_text(ctx, _T(label), label.Length, align);
        public static int ContextualItem(nk_image image, string text, uint alignment)
            => nk_contextual_item_image_text(ctx, image, _T(text), text.Length, alignment);
        public static int ContextualItem(nk_symbol_type symbol, string text, uint alignment)
            => nk_contextual_item_symbol_text(ctx, symbol, _T(text), text.Length, alignment);
        public static void ContextualClose() => nk_contextual_close(ctx);
        public static void ContextualEnd() => nk_contextual_end(ctx);
        public static void Tooltip(string tip) => nk_tooltip(ctx, _T(tip));
        public static int TooltipBegin(float width) => nk_tooltip_begin(ctx, width);
        public static void TooltipEnd() => nk_tooltip_end(ctx);

        public static void MenubarBegin() => nk_menubar_begin(ctx);
        public static void MenubarEnd() => nk_menubar_end(ctx);
        public static bool MenuBegin(string title, nk_text_alignment align, nk_vec2 size) => nk_menu_begin_text(ctx, (byte*)Marshal.StringToHGlobalAnsi(title), title.Length, (uint)align, size) != 0;
        public static bool MenuBegin(string label, nk_image image, nk_vec2 size) => nk_menu_begin_image(ctx, (byte*)Marshal.StringToHGlobalAnsi(label), image, size) != 0;
        public static bool MenuBegin(string label, nk_text_alignment align, nk_image image, nk_vec2 size) => nk_menu_begin_image_text(ctx, (byte*)Marshal.StringToHGlobalAnsi(label), label.Length, (uint)align, image, size) != 0;
        public static bool MenuBegin(string label, nk_symbol_type symbol, nk_vec2 size)
            => nk_menu_begin_symbol(ctx, (byte*)Marshal.StringToHGlobalAnsi(label), symbol, size) != 0;
        public static bool MenuBegin(string text, nk_text_alignment align, nk_symbol_type symbol, nk_vec2 size)
            => nk_menu_begin_symbol_text(ctx, (byte*)Marshal.StringToHGlobalAnsi(text), text.Length, (uint)align, symbol, size) != 0;
        public static bool MenuItem(string text, nk_text_alignment align)
            => nk_menu_item_text(ctx, (byte*)Marshal.StringToHGlobalAnsi(text), text.Length, (uint)align) != 0;
        public static bool MenuItem(nk_image image, string text, uint alignment)
            => nk_menu_item_image_text(ctx, image, (byte*)Marshal.StringToHGlobalAnsi(text), text.Length, alignment) != 0;
        public static bool MenuItem(nk_symbol_type symol, string text, nk_text_alignment align)
            => nk_menu_item_symbol_text(ctx, symol, (byte*)Marshal.StringToHGlobalAnsi(text), text.Length, (uint)align) != 0;
        public static void MenuClose() => nk_menu_close(ctx);
        public static void MenuEnd() => nk_menu_end(ctx);
    }


}
