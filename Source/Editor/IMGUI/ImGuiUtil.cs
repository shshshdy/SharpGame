using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

using ImVec2 = System.Numerics.Vector2;
using ImVec3 = System.Numerics.Vector3;

namespace UniqueEditor
{
    public enum ImGuiStyle
    {
        Default = 0,
        Gray,        // This is the default theme of my main.cpp demo.
        Light,
        OSX,         // Posted by @itamago here: https://github.com/ocornut/imgui/pull/511 (hope I can use it)
        OSXOpaque,   // Posted by @dougbinks here: https://gist.github.com/dougbinks/8089b4bbaccaaf6fa204236978d165a9 (hope I can use it)
        DarkOpaque,
        Soft,        // Posted by @olekristensen here: https://github.com/ocornut/imgui/issues/539 (hope I can use it)
        EdinBlack,   // Posted (via image) by edin_p in the screenshot section of Dear ImGui
        EdinWhite,   // Posted (via image) by edin_p in the screenshot section of Dear ImGui
        Maya,        // Posted by @ongamex here https://gist.github.com/ongamex/4ee36fb23d6c527939d0f4ba72144d29

        DefaultInverse,
        OSXInverse,
        OSXOpaqueInverse,
        DarkOpaqueInverse,

        Count
    }

    public partial class ImGuiUtil
    {
        static ImVec4 new_ImVec4(float x, float y, float z, float w)
        {
            return new ImVec4( x, y, z, w);
        }

        // @dougbinks (https://github.com/ocornut/imgui/issues/438)
        static void ChangeStyleColors(Style style, float satThresholdForInvertingLuminance, float shiftHue)
        {
            if (satThresholdForInvertingLuminance >= 1.0f && shiftHue == 0.0f) return;
            for (int i = 0; i < (int)ColorTarget.Count; i++)
            {
                ImVec4 col = style.GetColor((ColorTarget)i);
                float H = 0.0f, S = 0.0f, V = 0.0f;
                ImGuiUtil.ColorConvertRGBtoHSV(col.X, col.Y, col.Z, ref H, ref S, ref V);
                if (S <= satThresholdForInvertingLuminance) { V = 1.0f - V; }
                if (shiftHue != 0.0f) { H += shiftHue; if (H > 1) H -= 1.0f; else if (H < 0) H += 1.0f; }
                ImGuiUtil.ColorConvertHSVtoRGB(H, S, V, ref col.X, ref col.Y, ref col.Z);
            }
        }
        static void InvertStyleColors(Style style) { ChangeStyleColors(style, .1f, 0.0f); }
        static void ChangeStyleColorsHue(Style style, float shiftHue = 0.0f) { ChangeStyleColors(style, 0.0f, shiftHue); }
        static ImVec4 ConvertTitleBgColFromPrevVersion(ImVec4 win_bg_col, ImVec4 title_bg_col)
        {
            float new_a = 1.0f - ((1.0f - win_bg_col.W) * (1.0f - title_bg_col.W)), k = title_bg_col.W / new_a;
            return new_ImVec4((win_bg_col.X* win_bg_col.W + title_bg_col.X) * k, (win_bg_col.Y* win_bg_col.W + title_bg_col.Y) * k, (win_bg_col.Z* win_bg_col.W + title_bg_col.Z) * k, new_a);
        }

        public static bool ResetStyle(ImGuiStyle styleEnum, Style style)
        {
            if (styleEnum < 0 || styleEnum >= ImGuiStyle.Count) return false;
           // style = ImGuiStyle();
            switch (styleEnum)
            {
                case ImGuiStyle.DefaultInverse:
                    InvertStyleColors(style);
                    style.SetColor(ColorTarget.PopupBg, new_ImVec4(0.79f, 0.76f, 0.725f, 0.875f));

                    break;
                case ImGuiStyle.Gray:
                    {
                        style.AntiAliasedLines = true;
                        style.AntiAliasedShapes = true;
                        style.CurveTessellationTolerance = 1.25f;
                        style.Alpha = 1.0f;
                        //style.WindowFillAlphaDefault = .7f;

                        style.WindowPadding = new ImVec2(8, 8);
                        style.WindowRounding = 6;
                        style.ChildWindowRounding = 0;
                        style.FramePadding = new ImVec2(3, 3);
                        style.FrameRounding = 2;
                        style.ItemSpacing = new ImVec2(8, 4);
                        style.ItemInnerSpacing = new ImVec2(5, 5);
                        style.TouchExtraPadding = new ImVec2(0, 0);
                        style.IndentSpacing = 22;
                        style.ScrollbarSize = 16;
                        style.ScrollbarRounding = 4;
                        style.GrabMinSize = 8;
                        style.GrabRounding = 0;

                        style.SetColor(ColorTarget.Text, new_ImVec4(0.82f, 0.82f, 0.82f, 1.00f));
                        style.SetColor(ColorTarget.TextDisabled, new_ImVec4(0.60f, 0.60f, 0.60f, 1.00f));
                        style.SetColor(ColorTarget.WindowBg, new_ImVec4(0.16f, 0.16f, 0.18f, 0.70f));
                        style.SetColor(ColorTarget.ChildWindowBg, new_ImVec4(0.00f, 0.00f, 0.00f, 0.00f));
                        style.SetColor(ColorTarget.Border, new_ImVec4(0.00f, 0.00f, 0.00f, 0.60f));
                        style.SetColor(ColorTarget.BorderShadow, new_ImVec4(0.33f, 0.29f, 0.33f, 0.60f));
                        style.SetColor(ColorTarget.FrameBg, new_ImVec4(0.80f, 0.80f, 0.39f, 0.26f));
                        style.SetColor(ColorTarget.FrameBgHovered, new_ImVec4(0.90f, 0.80f, 0.80f, 0.40f));
                        style.SetColor(ColorTarget.FrameBgActive, new_ImVec4(0.90f, 0.65f, 0.65f, 0.45f));
                        style.SetColor(ColorTarget.TitleBg, ConvertTitleBgColFromPrevVersion(style.GetColor(ColorTarget.WindowBg), new_ImVec4(0.26f, 0.27f, 0.74f, 1.00f)));
                        style.SetColor(ColorTarget.TitleBgCollapsed, new_ImVec4(0.28f, 0.28f, 0.76f, 0.16f));
                        style.SetColor(ColorTarget.TitleBgActive, ConvertTitleBgColFromPrevVersion(style.GetColor(ColorTarget.WindowBg), new_ImVec4(0.50f, 0.50f, 1.00f, 0.55f)));
                        style.SetColor(ColorTarget.MenuBarBg, new_ImVec4(0.40f, 0.40f, 0.55f, 0.80f));
                        style.SetColor(ColorTarget.ScrollbarBg, new_ImVec4(1.00f, 1.00f, 1.00f, 0.18f));
                        style.SetColor(ColorTarget.ScrollbarGrab, new_ImVec4(0.67f, 0.58f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarGrabHovered, new_ImVec4(0.83f, 0.88f, 0.25f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarGrabActive, new_ImVec4(1.00f, 1.00f, 0.67f, 1.00f));
                        style.SetColor(ColorTarget.ComboBg, new_ImVec4(0.20f, 0.20f, 0.20f, 1.00f));
                        style.SetColor(ColorTarget.CheckMark, new_ImVec4(0.90f, 0.90f, 0.90f, 0.50f));
                        style.SetColor(ColorTarget.SliderGrab, new_ImVec4(1.00f, 1.00f, 1.00f, 0.29f));
                        style.SetColor(ColorTarget.SliderGrabActive, new_ImVec4(0.80f, 0.50f, 0.50f, 1.00f));
                        style.SetColor(ColorTarget.Button, new_ImVec4(0.25f, 0.29f, 0.61f, 1.00f));
                        style.SetColor(ColorTarget.ButtonHovered, new_ImVec4(0.35f, 0.40f, 0.68f, 1.00f));
                        style.SetColor(ColorTarget.ButtonActive, new_ImVec4(0.50f, 0.52f, 0.81f, 1.00f));
                        style.SetColor(ColorTarget.Header, new_ImVec4(0.11f, 0.37f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.HeaderHovered, new_ImVec4(0.40f, 0.50f, 0.25f, 1.00f));
                        style.SetColor(ColorTarget.HeaderActive, new_ImVec4(0.51f, 0.63f, 0.27f, 1.00f));
                        style.SetColor(ColorTarget.Column, new_ImVec4(1.00f, 1.00f, 1.00f, 1.00f));
                        style.SetColor(ColorTarget.ColumnHovered, new_ImVec4(0.60f, 0.40f, 0.40f, 1.00f));
                        style.SetColor(ColorTarget.ColumnActive, new_ImVec4(0.80f, 0.50f, 0.50f, 1.00f));
                        style.SetColor(ColorTarget.ResizeGrip, new_ImVec4(1.00f, 0.33f, 0.38f, 0.37f));
                        style.SetColor(ColorTarget.ResizeGripHovered, new_ImVec4(1.00f, 0.73f, 0.69f, 0.41f));
                        style.SetColor(ColorTarget.ResizeGripActive, new_ImVec4(1.00f, 1.00f, 0.75f, 0.90f));
                        style.SetColor(ColorTarget.CloseButton, new_ImVec4(0.73f, 0.20f, 0.00f, 0.68f));
                        style.SetColor(ColorTarget.CloseButtonHovered, new_ImVec4(1.00f, 0.27f, 0.27f, 0.50f));
                        style.SetColor(ColorTarget.CloseButtonActive, new_ImVec4(0.38f, 0.23f, 0.12f, 0.50f));
                        style.SetColor(ColorTarget.PlotLines, new_ImVec4(0.73f, 0.68f, 0.65f, 1.00f));
                        style.SetColor(ColorTarget.PlotLinesHovered, new_ImVec4(0.90f, 0.70f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogram, new_ImVec4(0.90f, 0.70f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogramHovered, new_ImVec4(1.00f, 0.60f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.TextSelectedBg, new_ImVec4(0.00f, 0.00f, 0.66f, 0.34f));
                        style.SetColor(ColorTarget.PopupBg, new_ImVec4(0.05f, 0.05f, 0.10f, 0.90f));
                        style.SetColor(ColorTarget.ModalWindowDarkening, new_ImVec4(0.20f, 0.20f, 0.20f, 0.35f));
                    }
                    break;
                case ImGuiStyle.Light:
                    {
                        style.AntiAliasedLines = true;
                        style.AntiAliasedShapes = true;
                        style.CurveTessellationTolerance = 1.25f;
                        style.Alpha = 1.0f;
                        //style.WindowFillAlphaDefault = .7f;

                        style.WindowPadding = new ImVec2(8, 8);
                        style.WindowRounding = 6;
                        style.ChildWindowRounding = 0;
                        style.FramePadding = new ImVec2(4, 3);
                        style.FrameRounding = 0;
                        style.ItemSpacing = new ImVec2(8, 4);
                        style.ItemInnerSpacing = new ImVec2(4, 4);
                        style.TouchExtraPadding = new ImVec2(0, 0);
                        style.IndentSpacing = 21;
                        style.ScrollbarSize = 16;
                        style.ScrollbarRounding = 4;
                        style.GrabMinSize = 8;
                        style.GrabRounding = 0;

                        style.SetColor(ColorTarget.Text, new_ImVec4(0.00f, 0.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.TextDisabled, new_ImVec4(0.00f, 0.00f, 0.00f, 0.71f));
                        style.SetColor(ColorTarget.WindowBg, new_ImVec4(0.56f, 0.56f, 0.56f, 1.00f));
                        style.SetColor(ColorTarget.ChildWindowBg, new_ImVec4(0.99f, 1.00f, 0.71f, 0.10f));
                        style.SetColor(ColorTarget.PopupBg, new_ImVec4(0.51f, 0.63f, 0.63f, 0.92f));
                        style.SetColor(ColorTarget.Border, new_ImVec4(0.14f, 0.14f, 0.14f, 0.51f));
                        style.SetColor(ColorTarget.BorderShadow, new_ImVec4(0.86f, 0.86f, 0.86f, 0.51f));
                        style.SetColor(ColorTarget.FrameBg, new_ImVec4(0.54f, 0.67f, 0.67f, 1.00f));
                        style.SetColor(ColorTarget.FrameBgHovered, new_ImVec4(0.61f, 0.74f, 0.75f, 1.00f));
                        style.SetColor(ColorTarget.FrameBgActive, new_ImVec4(0.67f, 0.82f, 0.82f, 1.00f));
                        style.SetColor(ColorTarget.TitleBg, new_ImVec4(0.54f, 0.54f, 0.24f, 1.00f));
                        style.SetColor(ColorTarget.TitleBgCollapsed, new_ImVec4(0.54f, 0.54f, 0.24f, 0.39f));
                        style.SetColor(ColorTarget.TitleBgActive, new_ImVec4(0.68f, 0.69f, 0.30f, 1.00f));
                        style.SetColor(ColorTarget.MenuBarBg, new_ImVec4(0.50f, 0.57f, 0.73f, 0.92f));
                        style.SetColor(ColorTarget.ScrollbarBg, new_ImVec4(0.26f, 0.29f, 0.31f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarGrab, new_ImVec4(0.61f, 0.60f, 0.26f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarGrabHovered, new_ImVec4(0.73f, 0.72f, 0.31f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarGrabActive, new_ImVec4(0.82f, 0.82f, 0.35f, 1.00f));
                        style.SetColor(ColorTarget.ComboBg, new_ImVec4(0.51f, 0.63f, 0.63f, 0.92f));
                        style.SetColor(ColorTarget.CheckMark, new_ImVec4(0.85f, 0.86f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.SliderGrab, new_ImVec4(0.81f, 0.82f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.SliderGrabActive, new_ImVec4(0.87f, 0.88f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.Button, new_ImVec4(0.41f, 0.59f, 0.31f, 1.00f));
                        style.SetColor(ColorTarget.ButtonHovered, new_ImVec4(0.45f, 0.65f, 0.34f, 1.00f));
                        style.SetColor(ColorTarget.ButtonActive, new_ImVec4(0.50f, 0.73f, 0.38f, 1.00f));
                        style.SetColor(ColorTarget.Header, new_ImVec4(0.42f, 0.47f, 0.88f, 1.00f));
                        style.SetColor(ColorTarget.HeaderHovered, new_ImVec4(0.44f, 0.51f, 0.93f, 1.00f));
                        style.SetColor(ColorTarget.HeaderActive, new_ImVec4(0.50f, 0.62f, 1.00f, 1.00f));
                        style.SetColor(ColorTarget.Column, new_ImVec4(0.13f, 0.14f, 0.11f, 1.00f));
                        style.SetColor(ColorTarget.ColumnHovered, new_ImVec4(0.73f, 0.75f, 0.61f, 1.00f));
                        style.SetColor(ColorTarget.ColumnActive, new_ImVec4(0.89f, 0.90f, 0.70f, 1.00f));
                        style.SetColor(ColorTarget.ResizeGrip, new_ImVec4(0.61f, 0.22f, 0.22f, 1.00f));
                        style.SetColor(ColorTarget.ResizeGripHovered, new_ImVec4(0.69f, 0.24f, 0.24f, 1.00f));
                        style.SetColor(ColorTarget.ResizeGripActive, new_ImVec4(0.80f, 0.28f, 0.28f, 1.00f));
                        style.SetColor(ColorTarget.CloseButton, new_ImVec4(0.67f, 0.00f, 0.00f, 0.50f));
                        style.SetColor(ColorTarget.CloseButtonHovered, new_ImVec4(0.78f, 0.00f, 0.00f, 0.60f));
                        style.SetColor(ColorTarget.CloseButtonActive, new_ImVec4(0.92f, 0.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.PlotLines, new_ImVec4(0.17f, 0.35f, 0.03f, 1.00f));
                        style.SetColor(ColorTarget.PlotLinesHovered, new_ImVec4(0.41f, 0.81f, 0.06f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogram, new_ImVec4(0.81f, 0.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogramHovered, new_ImVec4(1.00f, 0.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.TextSelectedBg, new_ImVec4(0.48f, 0.61f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.ModalWindowDarkening, new_ImVec4(0.39f, 0.12f, 0.12f, 0.20f));
                    }
                    break;
                case ImGuiStyle.OSX:
                case ImGuiStyle.OSXInverse:
                    {
                        // Posted by @itamago here: https://github.com/ocornut/imgui/pull/511
                        style.SetColor(ColorTarget.Text, new_ImVec4(0.00f, 0.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.TextDisabled, new_ImVec4(0.60f, 0.60f, 0.60f, 1.00f));
                        //style.SetColor(ColorTarget.TextHovered]           = new_ImVec4(1.00f, 1.00f, 1.00f, 1.00f));
                        //style.SetColor(ColorTarget.TextActive]            = new_ImVec4(1.00f, 1.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.WindowBg, new_ImVec4(0.94f, 0.94f, 0.94f, 0.7f));
                        style.SetColor(ColorTarget.ChildWindowBg, new_ImVec4(0.00f, 0.00f, 0.00f, 0.00f));
                        style.SetColor(ColorTarget.Border, new_ImVec4(0.00f, 0.00f, 0.00f, 0.39f));
                        style.SetColor(ColorTarget.BorderShadow, new_ImVec4(1.00f, 1.00f, 1.00f, 0.10f));
                        style.SetColor(ColorTarget.FrameBg, new_ImVec4(1.00f, 1.00f, 1.00f, 1.00f));
                        style.SetColor(ColorTarget.FrameBgHovered, new_ImVec4(0.26f, 0.59f, 0.98f, 0.40f));
                        style.SetColor(ColorTarget.FrameBgActive, new_ImVec4(0.26f, 0.59f, 0.98f, 0.67f));
                        style.SetColor(ColorTarget.TitleBg, new_ImVec4(0.96f, 0.96f, 0.96f, 1.00f));
                        style.SetColor(ColorTarget.TitleBgCollapsed, new_ImVec4(1.00f, 1.00f, 1.00f, 0.51f));
                        style.SetColor(ColorTarget.TitleBgActive, new_ImVec4(0.82f, 0.82f, 0.82f, 1.00f));
                        style.SetColor(ColorTarget.MenuBarBg, new_ImVec4(0.86f, 0.86f, 0.86f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarBg, new_ImVec4(0.98f, 0.98f, 0.98f, 0.53f));
                        style.SetColor(ColorTarget.ScrollbarGrab, new_ImVec4(0.69f, 0.69f, 0.69f, 0.80f));
                        style.SetColor(ColorTarget.ScrollbarGrabHovered, new_ImVec4(0.49f, 0.49f, 0.49f, 0.80f));
                        style.SetColor(ColorTarget.ScrollbarGrabActive, new_ImVec4(0.49f, 0.49f, 0.49f, 1.00f));
                        style.SetColor(ColorTarget.ComboBg, new_ImVec4(0.86f, 0.86f, 0.86f, 0.99f));
                        style.SetColor(ColorTarget.CheckMark, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.SliderGrab, new_ImVec4(0.26f, 0.59f, 0.98f, 0.78f));
                        style.SetColor(ColorTarget.SliderGrabActive, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.Button, new_ImVec4(0.26f, 0.59f, 0.98f, 0.40f));
                        style.SetColor(ColorTarget.ButtonHovered, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.ButtonActive, new_ImVec4(0.06f, 0.53f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.Header, new_ImVec4(0.26f, 0.59f, 0.98f, 0.31f));
                        style.SetColor(ColorTarget.HeaderHovered, new_ImVec4(0.26f, 0.59f, 0.98f, 0.80f));
                        style.SetColor(ColorTarget.HeaderActive, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.Column, new_ImVec4(0.39f, 0.39f, 0.39f, 1.00f));
                        style.SetColor(ColorTarget.ColumnHovered, new_ImVec4(0.26f, 0.59f, 0.98f, 0.78f));
                        style.SetColor(ColorTarget.ColumnActive, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.ResizeGrip, new_ImVec4(1.00f, 1.00f, 1.00f, 0.00f));
                        style.SetColor(ColorTarget.ResizeGripHovered, new_ImVec4(0.26f, 0.59f, 0.98f, 0.67f));
                        style.SetColor(ColorTarget.ResizeGripActive, new_ImVec4(0.26f, 0.59f, 0.98f, 0.95f));
                        style.SetColor(ColorTarget.CloseButton, new_ImVec4(0.59f, 0.59f, 0.59f, 0.50f));
                        style.SetColor(ColorTarget.CloseButtonHovered, new_ImVec4(0.98f, 0.39f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.CloseButtonActive, new_ImVec4(0.98f, 0.39f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.PlotLines, new_ImVec4(0.39f, 0.39f, 0.39f, 1.00f));
                        style.SetColor(ColorTarget.PlotLinesHovered, new_ImVec4(1.00f, 0.43f, 0.35f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogram, new_ImVec4(0.90f, 0.70f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogramHovered, new_ImVec4(1.00f, 0.60f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.TextSelectedBg, new_ImVec4(0.26f, 0.59f, 0.98f, 0.35f));
                        style.SetColor(ColorTarget.PopupBg, new_ImVec4(1.00f, 1.00f, 1.00f, 0.94f));
                        style.SetColor(ColorTarget.ModalWindowDarkening, new_ImVec4(0.20f, 0.20f, 0.20f, 0.35f));

                        if (styleEnum == ImGuiStyle.OSXInverse) InvertStyleColors(style);
                    }
                    break;
                case ImGuiStyle.DarkOpaque:
                case ImGuiStyle.DarkOpaqueInverse:
                    {
                        style.AntiAliasedLines = true;
                        style.AntiAliasedShapes = true;
                        style.CurveTessellationTolerance = 1.25f;
                        style.Alpha = 1.0f;
                        //style.WindowFillAlphaDefault = .7f;

                        style.WindowPadding = new ImVec2(8, 8);
                        style.WindowRounding = 4;
                        style.ChildWindowRounding = 0;
                        style.FramePadding = new ImVec2(3, 3);
                        style.FrameRounding = 0;
                        style.ItemSpacing = new ImVec2(8, 4);
                        style.ItemInnerSpacing = new ImVec2(5, 5);
                        style.TouchExtraPadding = new ImVec2(0, 0);
                        style.IndentSpacing = 22;
                        style.ScrollbarSize = 16;
                        style.ScrollbarRounding = 8;
                        style.GrabMinSize = 8;
                        style.GrabRounding = 0;

                        //ImGuiStyle & style = ImGui::GetStyle();
                        style.SetColor(ColorTarget.Text, new_ImVec4(0.73f, 0.73f, 0.73f, 1.00f));
                        style.SetColor(ColorTarget.TextDisabled, new_ImVec4(0.73f, 0.73f, 0.73f, 0.39f));
                        style.SetColor(ColorTarget.WindowBg, new_ImVec4(0.18f, 0.18f, 0.18f, 1.00f));
                        style.SetColor(ColorTarget.ChildWindowBg, new_ImVec4(0.00f, 0.00f, 0.00f, 0.00f));
                        style.SetColor(ColorTarget.PopupBg, new_ImVec4(0.01f, 0.04f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.Border, new_ImVec4(0.04f, 0.04f, 0.04f, 0.51f));
                        style.SetColor(ColorTarget.BorderShadow, new_ImVec4(0.18f, 0.18f, 0.18f, 1.00f));
                        style.SetColor(ColorTarget.FrameBg, new_ImVec4(0.33f, 0.33f, 0.33f, 1.00f));
                        style.SetColor(ColorTarget.FrameBgHovered, new_ImVec4(0.39f, 0.39f, 0.40f, 1.00f));
                        style.SetColor(ColorTarget.FrameBgActive, new_ImVec4(0.54f, 0.54f, 0.55f, 1.00f));
                        style.SetColor(ColorTarget.TitleBg, new_ImVec4(0.25f, 0.25f, 0.24f, 1.00f));
                        style.SetColor(ColorTarget.TitleBgCollapsed, new_ImVec4(0.25f, 0.25f, 0.24f, 0.23f));
                        style.SetColor(ColorTarget.TitleBgActive, new_ImVec4(0.35f, 0.35f, 0.34f, 1.00f));
                        style.SetColor(ColorTarget.MenuBarBg, new_ImVec4(0.38f, 0.38f, 0.45f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarBg, new_ImVec4(0.24f, 0.27f, 0.30f, 0.60f));
                        style.SetColor(ColorTarget.ScrollbarGrab, new_ImVec4(0.64f, 0.64f, 0.80f, 0.59f));
                        style.SetColor(ColorTarget.ScrollbarGrabHovered, new_ImVec4(0.64f, 0.64f, 0.80f, 0.78f));
                        style.SetColor(ColorTarget.ScrollbarGrabActive, new_ImVec4(0.64f, 0.64f, 0.80f, 1.00f));
                        style.SetColor(ColorTarget.ComboBg, new_ImVec4(0.10f, 0.10f, 0.10f, 1.00f));
                        style.SetColor(ColorTarget.CheckMark, new_ImVec4(0.69f, 0.69f, 0.69f, 1.00f));
                        style.SetColor(ColorTarget.SliderGrab, new_ImVec4(0.69f, 0.69f, 0.69f, 1.00f));
                        style.SetColor(ColorTarget.SliderGrabActive, new_ImVec4(0.88f, 0.88f, 0.88f, 1.00f));
                        style.SetColor(ColorTarget.Button, new_ImVec4(0.25f, 0.25f, 0.25f, 1.00f));
                        style.SetColor(ColorTarget.ButtonHovered, new_ImVec4(0.33f, 0.33f, 0.33f, 1.00f));
                        style.SetColor(ColorTarget.ButtonActive, new_ImVec4(0.45f, 0.45f, 0.45f, 1.00f));
                        style.SetColor(ColorTarget.Header, new_ImVec4(0.25f, 0.25f, 0.25f, 1.00f));
                        style.SetColor(ColorTarget.HeaderHovered, new_ImVec4(0.35f, 0.35f, 0.35f, 1.00f));
                        style.SetColor(ColorTarget.HeaderActive, new_ImVec4(0.42f, 0.42f, 0.43f, 1.00f));
                        style.SetColor(ColorTarget.Column, new_ImVec4(0.84f, 0.84f, 0.84f, 0.90f));
                        style.SetColor(ColorTarget.ColumnHovered, new_ImVec4(0.90f, 0.90f, 0.90f, 0.95f));
                        style.SetColor(ColorTarget.ColumnActive, new_ImVec4(1.00f, 1.00f, 1.00f, 1.00f));
                        style.SetColor(ColorTarget.ResizeGrip, new_ImVec4(1.00f, 1.00f, 1.00f, 0.30f));
                        style.SetColor(ColorTarget.ResizeGripHovered, new_ImVec4(1.00f, 1.00f, 1.00f, 0.60f));
                        style.SetColor(ColorTarget.ResizeGripActive, new_ImVec4(1.00f, 1.00f, 1.00f, 0.90f));
                        style.SetColor(ColorTarget.CloseButton, new_ImVec4(0.70f, 0.72f, 0.71f, 1.00f));
                        style.SetColor(ColorTarget.CloseButtonHovered, new_ImVec4(0.83f, 0.86f, 0.84f, 1.00f));
                        style.SetColor(ColorTarget.CloseButtonActive, new_ImVec4(0.70f, 0.70f, 0.70f, 1.00f));
                        style.SetColor(ColorTarget.PlotLines, new_ImVec4(1.00f, 1.00f, 1.00f, 1.00f));
                        style.SetColor(ColorTarget.PlotLinesHovered, new_ImVec4(0.90f, 0.78f, 0.37f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogram, new_ImVec4(0.90f, 0.78f, 0.37f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogramHovered, new_ImVec4(1.00f, 0.77f, 0.41f, 1.00f));
                        style.SetColor(ColorTarget.TextSelectedBg, new_ImVec4(0.26f, 0.26f, 0.63f, 1.00f));
                        style.SetColor(ColorTarget.ModalWindowDarkening, new_ImVec4(0.20f, 0.20f, 0.20f, 0.35f));

                        if (styleEnum == ImGuiStyle.DarkOpaqueInverse)
                        {
                            InvertStyleColors(style);
                            style.SetColor(ColorTarget.PopupBg, new_ImVec4(0.99f, 0.96f, 1.00f, 1.00f));
                        }
                    }
                    break;
                case ImGuiStyle.OSXOpaque:
                case ImGuiStyle.OSXOpaqueInverse:
                    {
                        //ImVec4 Full = new_ImVec4(0.00f, 0.00f, 0.00f, 1.00f));
                        style.FrameRounding = 3.0f;
                        style.SetColor(ColorTarget.Text, new_ImVec4(0.00f, 0.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.TextDisabled, new_ImVec4(0.60f, 0.60f, 0.60f, 1.00f));
                        style.SetColor(ColorTarget.WindowBg, new_ImVec4(0.94f, 0.94f, 0.94f, 1.00f));
                        style.SetColor(ColorTarget.ChildWindowBg, new_ImVec4(0.00f, 0.00f, 0.00f, 0.00f));
                        style.SetColor(ColorTarget.Border, new_ImVec4(0.00f, 0.00f, 0.00f, 0.39f));
                        style.SetColor(ColorTarget.BorderShadow, new_ImVec4(1.00f, 1.00f, 1.00f, 0.10f));
                        style.SetColor(ColorTarget.FrameBg, new_ImVec4(1.00f, 1.00f, 1.00f, 1.00f));
                        style.SetColor(ColorTarget.FrameBgHovered, new_ImVec4(0.26f, 0.59f, 0.98f, 0.40f));
                        style.SetColor(ColorTarget.FrameBgActive, new_ImVec4(0.26f, 0.59f, 0.98f, 0.67f));
                        style.SetColor(ColorTarget.TitleBg, new_ImVec4(0.96f, 0.96f, 0.96f, 1.00f));
                        style.SetColor(ColorTarget.TitleBgCollapsed, new_ImVec4(1.00f, 1.00f, 1.00f, 0.51f));
                        style.SetColor(ColorTarget.TitleBgActive, new_ImVec4(0.82f, 0.82f, 0.82f, 1.00f));
                        style.SetColor(ColorTarget.MenuBarBg, new_ImVec4(0.86f, 0.86f, 0.86f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarBg, new_ImVec4(0.98f, 0.98f, 0.98f, 0.53f));
                        style.SetColor(ColorTarget.ScrollbarGrab, new_ImVec4(0.69f, 0.69f, 0.69f, 0.80f));
                        style.SetColor(ColorTarget.ScrollbarGrabHovered, new_ImVec4(0.49f, 0.49f, 0.49f, 0.80f));
                        style.SetColor(ColorTarget.ScrollbarGrabActive, new_ImVec4(0.49f, 0.49f, 0.49f, 1.00f));
                        style.SetColor(ColorTarget.ComboBg, new_ImVec4(0.86f, 0.86f, 0.86f, 0.99f));
                        style.SetColor(ColorTarget.CheckMark, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.SliderGrab, new_ImVec4(0.26f, 0.59f, 0.98f, 0.78f));
                        style.SetColor(ColorTarget.SliderGrabActive, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.Button, new_ImVec4(0.26f, 0.59f, 0.98f, 0.40f));
                        style.SetColor(ColorTarget.ButtonHovered, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.ButtonActive, new_ImVec4(0.06f, 0.53f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.Header, new_ImVec4(0.26f, 0.59f, 0.98f, 0.31f));
                        style.SetColor(ColorTarget.HeaderHovered, new_ImVec4(0.26f, 0.59f, 0.98f, 0.80f));
                        style.SetColor(ColorTarget.HeaderActive, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.Column, new_ImVec4(0.39f, 0.39f, 0.39f, 1.00f));
                        style.SetColor(ColorTarget.ColumnHovered, new_ImVec4(0.26f, 0.59f, 0.98f, 0.78f));
                        style.SetColor(ColorTarget.ColumnActive, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.ResizeGrip, new_ImVec4(1.00f, 1.00f, 1.00f, 0.50f));
                        style.SetColor(ColorTarget.ResizeGripHovered, new_ImVec4(0.26f, 0.59f, 0.98f, 0.67f));
                        style.SetColor(ColorTarget.ResizeGripActive, new_ImVec4(0.26f, 0.59f, 0.98f, 0.95f));
                        style.SetColor(ColorTarget.CloseButton, new_ImVec4(0.59f, 0.59f, 0.59f, 0.50f));
                        style.SetColor(ColorTarget.CloseButtonHovered, new_ImVec4(0.98f, 0.39f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.CloseButtonActive, new_ImVec4(0.98f, 0.39f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.PlotLines, new_ImVec4(0.39f, 0.39f, 0.39f, 1.00f));
                        style.SetColor(ColorTarget.PlotLinesHovered, new_ImVec4(1.00f, 0.43f, 0.35f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogram, new_ImVec4(0.90f, 0.70f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogramHovered, new_ImVec4(1.00f, 0.60f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.TextSelectedBg, new_ImVec4(0.26f, 0.59f, 0.98f, 0.35f));
                        style.SetColor(ColorTarget.PopupBg, new_ImVec4(1.00f, 1.00f, 1.00f, 0.94f));
                        style.SetColor(ColorTarget.ModalWindowDarkening, new_ImVec4(0.20f, 0.20f, 0.20f, 0.35f));

                        if (styleEnum == ImGuiStyle.OSXOpaqueInverse)
                        {
                            InvertStyleColors(style);
                            //style.SetColor(ColorTarget.PopupBg]	     = new_ImVec4(0.3f, 0.3f, 0.4f, 1.00f));
                        }

                    }
                    break;
                case ImGuiStyle.Soft:
                    {
                        // style by olekristensen [https://github.com/ocornut/imgui/issues/539]
                        /* olekristensen used it wth these fonts:
                        io.Fonts->Clear();
                        io.Fonts->AddFontFromFileTTF(ofToDataPath("fonts/OpenSans-Light.ttf", true).c_str(), 16);
                        io.Fonts->AddFontFromFileTTF(ofToDataPath("fonts/OpenSans-Regular.ttf", true).c_str(), 16);
                        io.Fonts->AddFontFromFileTTF(ofToDataPath("fonts/OpenSans-Light.ttf", true).c_str(), 32);
                        io.Fonts->AddFontFromFileTTF(ofToDataPath("fonts/OpenSans-Regular.ttf", true).c_str(), 11);
                        io.Fonts->AddFontFromFileTTF(ofToDataPath("fonts/OpenSans-Bold.ttf", true).c_str(), 11);
                        io.Fonts->Build();*/

                        style.WindowPadding = new ImVec2(15, 15);
                        style.WindowRounding = 5.0f;
                        style.FramePadding = new ImVec2(5, 5);
                        style.FrameRounding = 4.0f;
                        style.ItemSpacing = new ImVec2(12, 8);
                        style.ItemInnerSpacing = new ImVec2(8, 6);
                        style.IndentSpacing = 25.0f;
                        style.ScrollbarSize = 15.0f;
                        style.ScrollbarRounding = 9.0f;
                        style.GrabMinSize = 5.0f;
                        style.GrabRounding = 3.0f;

                        style.SetColor(ColorTarget.Text, new_ImVec4(0.40f, 0.39f, 0.38f, 1.00f));
                        style.SetColor(ColorTarget.TextDisabled, new_ImVec4(0.40f, 0.39f, 0.38f, 0.77f));
                        style.SetColor(ColorTarget.WindowBg, new_ImVec4(0.92f, 0.91f, 0.88f, 0.70f));
                        style.SetColor(ColorTarget.ChildWindowBg, new_ImVec4(1.00f, 0.98f, 0.95f, 0.58f));
                        style.SetColor(ColorTarget.PopupBg, new_ImVec4(0.92f, 0.91f, 0.88f, 0.92f));
                        style.SetColor(ColorTarget.Border, new_ImVec4(0.84f, 0.83f, 0.80f, 0.65f));
                        style.SetColor(ColorTarget.BorderShadow, new_ImVec4(0.92f, 0.91f, 0.88f, 0.00f));
                        style.SetColor(ColorTarget.FrameBg, new_ImVec4(1.00f, 0.98f, 0.95f, 1.00f));
                        style.SetColor(ColorTarget.FrameBgHovered, new_ImVec4(0.99f, 1.00f, 0.40f, 0.78f));
                        style.SetColor(ColorTarget.FrameBgActive, new_ImVec4(0.26f, 1.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.TitleBg, new_ImVec4(1.00f, 0.98f, 0.95f, 1.00f));
                        style.SetColor(ColorTarget.TitleBgCollapsed, new_ImVec4(1.00f, 0.98f, 0.95f, 0.75f));
                        style.SetColor(ColorTarget.TitleBgActive, new_ImVec4(0.25f, 1.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.MenuBarBg, new_ImVec4(1.00f, 0.98f, 0.95f, 0.47f));
                        style.SetColor(ColorTarget.ScrollbarBg, new_ImVec4(1.00f, 0.98f, 0.95f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarGrab, new_ImVec4(0.00f, 0.00f, 0.00f, 0.21f));
                        style.SetColor(ColorTarget.ScrollbarGrabHovered, new_ImVec4(0.90f, 0.91f, 0.00f, 0.78f));
                        style.SetColor(ColorTarget.ScrollbarGrabActive, new_ImVec4(0.25f, 1.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.ComboBg, new_ImVec4(1.00f, 0.98f, 0.95f, 1.00f));
                        style.SetColor(ColorTarget.CheckMark, new_ImVec4(0.25f, 1.00f, 0.00f, 0.80f));
                        style.SetColor(ColorTarget.SliderGrab, new_ImVec4(0.00f, 0.00f, 0.00f, 0.14f));
                        style.SetColor(ColorTarget.SliderGrabActive, new_ImVec4(0.25f, 1.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.Button, new_ImVec4(0.00f, 0.00f, 0.00f, 0.14f));
                        style.SetColor(ColorTarget.ButtonHovered, new_ImVec4(0.99f, 1.00f, 0.22f, 0.86f));
                        style.SetColor(ColorTarget.ButtonActive, new_ImVec4(0.25f, 1.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.Header, new_ImVec4(0.25f, 1.00f, 0.00f, 0.76f));
                        style.SetColor(ColorTarget.HeaderHovered, new_ImVec4(0.25f, 1.00f, 0.00f, 0.86f));
                        style.SetColor(ColorTarget.HeaderActive, new_ImVec4(0.25f, 1.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.Column, new_ImVec4(0.00f, 0.00f, 0.00f, 0.32f));
                        style.SetColor(ColorTarget.ColumnHovered, new_ImVec4(0.25f, 1.00f, 0.00f, 0.78f));
                        style.SetColor(ColorTarget.ColumnActive, new_ImVec4(0.25f, 1.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.ResizeGrip, new_ImVec4(0.00f, 0.00f, 0.00f, 0.04f));
                        style.SetColor(ColorTarget.ResizeGripHovered, new_ImVec4(0.25f, 1.00f, 0.00f, 0.78f));
                        style.SetColor(ColorTarget.ResizeGripActive, new_ImVec4(0.25f, 1.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.CloseButton, new_ImVec4(0.40f, 0.39f, 0.38f, 0.16f));
                        style.SetColor(ColorTarget.CloseButtonHovered, new_ImVec4(0.40f, 0.39f, 0.38f, 0.39f));
                        style.SetColor(ColorTarget.CloseButtonActive, new_ImVec4(0.40f, 0.39f, 0.38f, 1.00f));
                        style.SetColor(ColorTarget.PlotLines, new_ImVec4(0.40f, 0.39f, 0.38f, 0.63f));
                        style.SetColor(ColorTarget.PlotLinesHovered, new_ImVec4(0.25f, 1.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogram, new_ImVec4(0.40f, 0.39f, 0.38f, 0.63f));
                        style.SetColor(ColorTarget.PlotHistogramHovered, new_ImVec4(0.25f, 1.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.TextSelectedBg, new_ImVec4(0.25f, 1.00f, 0.00f, 0.43f));
                        style.SetColor(ColorTarget.ModalWindowDarkening, new_ImVec4(1.00f, 0.98f, 0.95f, 0.73f));
                    }
                    break;
                case ImGuiStyle.EdinBlack:
                    {
                        // style based on an image posted by edin_p in the screenshot section (part 3) of Dear ImGui Issue Section.
                        style.WindowRounding = 6.0f;
                        style.ScrollbarRounding = 2.0f;
                       // style.WindowTitleAlign.x = 0.45f;
                        style.SetColor(ColorTarget.Text, new_ImVec4(0.98f, 0.98f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.TextDisabled, new_ImVec4(0.98f, 0.98f, 0.98f, 0.50f));
                        style.SetColor(ColorTarget.WindowBg, new_ImVec4(0.10f, 0.10f, 0.10f, 1.00f));
                        style.SetColor(ColorTarget.ChildWindowBg, new_ImVec4(0.00f, 0.00f, 0.00f, 0.00f));
                        style.SetColor(ColorTarget.PopupBg, new_ImVec4(0.10f, 0.10f, 0.10f, 0.90f));
                        style.SetColor(ColorTarget.Border, new_ImVec4(0.27f, 0.27f, 0.27f, 1.00f));
                        style.SetColor(ColorTarget.BorderShadow, new_ImVec4(0.00f, 0.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.FrameBg, new_ImVec4(0.23f, 0.23f, 0.23f, 1.00f));
                        style.SetColor(ColorTarget.FrameBgHovered, new_ImVec4(0.28f, 0.28f, 0.28f, 0.40f));
                        style.SetColor(ColorTarget.FrameBgActive, new_ImVec4(0.31f, 0.31f, 0.31f, 0.45f));
                        style.SetColor(ColorTarget.TitleBg, new_ImVec4(0.19f, 0.19f, 0.19f, 1.00f));
                        style.SetColor(ColorTarget.TitleBgCollapsed, new_ImVec4(0.19f, 0.19f, 0.19f, 0.20f));
                        style.SetColor(ColorTarget.TitleBgActive, new_ImVec4(0.30f, 0.30f, 0.30f, 0.87f));
                        style.SetColor(ColorTarget.MenuBarBg, new_ImVec4(0.10f, 0.10f, 0.10f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarBg, new_ImVec4(0.30f, 0.30f, 0.30f, 0.60f));
                        style.SetColor(ColorTarget.ScrollbarGrab, new_ImVec4(0.80f, 0.80f, 0.80f, 0.30f));
                        style.SetColor(ColorTarget.ScrollbarGrabHovered, new_ImVec4(0.80f, 0.80f, 0.80f, 0.40f));
                        style.SetColor(ColorTarget.ScrollbarGrabActive, new_ImVec4(0.86f, 0.86f, 0.86f, 0.52f));
                        style.SetColor(ColorTarget.ComboBg, new_ImVec4(0.21f, 0.21f, 0.21f, 0.99f));
                        style.SetColor(ColorTarget.CheckMark, new_ImVec4(0.47f, 0.47f, 0.47f, 1.00f));
                        style.SetColor(ColorTarget.SliderGrab, new_ImVec4(0.60f, 0.60f, 0.60f, 0.34f));
                        style.SetColor(ColorTarget.SliderGrabActive, new_ImVec4(0.84f, 0.84f, 0.84f, 0.34f));
                        style.SetColor(ColorTarget.Button, new_ImVec4(0.29f, 0.29f, 0.29f, 1.00f));
                        style.SetColor(ColorTarget.ButtonHovered, new_ImVec4(0.33f, 0.33f, 0.33f, 1.00f));
                        style.SetColor(ColorTarget.ButtonActive, new_ImVec4(0.42f, 0.42f, 0.42f, 1.00f));
                        style.SetColor(ColorTarget.Header, new_ImVec4(0.34f, 0.34f, 0.34f, 1.00f));
                        style.SetColor(ColorTarget.HeaderHovered, new_ImVec4(0.42f, 0.42f, 0.42f, 1.00f));
                        style.SetColor(ColorTarget.HeaderActive, new_ImVec4(0.50f, 0.50f, 0.50f, 1.00f));
                        style.SetColor(ColorTarget.Column, new_ImVec4(0.50f, 0.50f, 0.50f, 1.00f));
                        style.SetColor(ColorTarget.ColumnHovered, new_ImVec4(0.70f, 0.60f, 0.60f, 1.00f));
                        style.SetColor(ColorTarget.ColumnActive, new_ImVec4(0.90f, 0.70f, 0.70f, 1.00f));
                        style.SetColor(ColorTarget.ResizeGrip, new_ImVec4(1.00f, 1.00f, 1.00f, 0.30f));
                        style.SetColor(ColorTarget.ResizeGripHovered, new_ImVec4(1.00f, 1.00f, 1.00f, 0.60f));
                        style.SetColor(ColorTarget.ResizeGripActive, new_ImVec4(1.00f, 1.00f, 1.00f, 0.90f));
                        style.SetColor(ColorTarget.CloseButton, new_ImVec4(1.00f, 1.00f, 1.00f, 0.50f));
                        style.SetColor(ColorTarget.CloseButtonHovered, new_ImVec4(0.90f, 0.90f, 0.90f, 0.60f));
                        style.SetColor(ColorTarget.CloseButtonActive, new_ImVec4(0.70f, 0.70f, 0.70f, 1.00f));
                        style.SetColor(ColorTarget.PlotLines, new_ImVec4(1.00f, 1.00f, 1.00f, 1.00f));
                        style.SetColor(ColorTarget.PlotLinesHovered, new_ImVec4(0.90f, 0.70f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogram, new_ImVec4(0.90f, 0.70f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogramHovered, new_ImVec4(1.00f, 0.60f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.TextSelectedBg, new_ImVec4(0.27f, 0.36f, 0.59f, 0.61f));
                        style.SetColor(ColorTarget.ModalWindowDarkening, new_ImVec4(0.20f, 0.20f, 0.20f, 0.35f));


                    }
                    break;
                case ImGuiStyle.EdinWhite:
                    {
                        // style based on an image posted by edin_p in the screenshot section (part 3) of Dear ImGui Issue Section.
                        style.WindowRounding = 6.0f;
                        style.ScrollbarRounding = 2.0f;
                    //    style.WindowTitleAlign.x = 0.45f;
                        style.SetColor(ColorTarget.Text, new_ImVec4(0.00f, 0.00f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.TextDisabled, new_ImVec4(0.00f, 0.00f, 0.00f, 0.50f));
                        style.SetColor(ColorTarget.WindowBg, new_ImVec4(0.92f, 0.92f, 0.92f, 1.00f));
                        style.SetColor(ColorTarget.ChildWindowBg, new_ImVec4(1.00f, 1.00f, 1.00f, 0.00f));
                        style.SetColor(ColorTarget.PopupBg, new_ImVec4(1.00f, 1.00f, 1.00f, 0.92f));
                        style.SetColor(ColorTarget.Border, new_ImVec4(0.73f, 0.73f, 0.73f, 0.65f));
                        style.SetColor(ColorTarget.BorderShadow, new_ImVec4(0.65f, 0.65f, 0.65f, 0.31f));
                        style.SetColor(ColorTarget.FrameBg, new_ImVec4(0.96f, 0.96f, 0.96f, 1.00f));
                        style.SetColor(ColorTarget.FrameBgHovered, new_ImVec4(0.98f, 0.98f, 0.98f, 0.88f));
                        style.SetColor(ColorTarget.FrameBgActive, new_ImVec4(1.00f, 1.00f, 1.00f, 1.00f));
                        style.SetColor(ColorTarget.TitleBg, new_ImVec4(0.94f, 0.94f, 0.94f, 1.00f));
                        style.SetColor(ColorTarget.TitleBgCollapsed, new_ImVec4(0.94f, 0.94f, 0.94f, 0.20f));
                        style.SetColor(ColorTarget.TitleBgActive, new_ImVec4(1.00f, 1.00f, 1.00f, 1.00f));
                        style.SetColor(ColorTarget.MenuBarBg, new_ImVec4(0.95f, 0.95f, 0.95f, 0.92f));
                        style.SetColor(ColorTarget.ScrollbarBg, new_ImVec4(0.96f, 0.96f, 0.96f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarGrab, new_ImVec4(0.75f, 0.75f, 0.75f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarGrabHovered, new_ImVec4(0.67f, 0.67f, 0.67f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarGrabActive, new_ImVec4(0.55f, 0.55f, 0.55f, 1.00f));
                        style.SetColor(ColorTarget.ComboBg, new_ImVec4(0.96f, 0.96f, 0.96f, 0.92f));
                        style.SetColor(ColorTarget.CheckMark, new_ImVec4(0.72f, 0.72f, 0.72f, 1.00f));
                        style.SetColor(ColorTarget.SliderGrab, new_ImVec4(0.57f, 0.57f, 0.57f, 0.34f));
                        style.SetColor(ColorTarget.SliderGrabActive, new_ImVec4(0.24f, 0.24f, 0.24f, 0.34f));
                        style.SetColor(ColorTarget.Button, new_ImVec4(0.97f, 0.97f, 0.97f, 1.00f));
                        style.SetColor(ColorTarget.ButtonHovered, new_ImVec4(0.93f, 0.93f, 0.93f, 1.00f));
                        style.SetColor(ColorTarget.ButtonActive, new_ImVec4(0.84f, 0.84f, 0.84f, 1.00f));
                        style.SetColor(ColorTarget.Header, new_ImVec4(1.00f, 1.00f, 1.00f, 1.00f));
                        style.SetColor(ColorTarget.HeaderHovered, new_ImVec4(0.92f, 0.92f, 0.92f, 1.00f));
                        style.SetColor(ColorTarget.HeaderActive, new_ImVec4(0.84f, 0.84f, 0.84f, 1.00f));
                        style.SetColor(ColorTarget.Column, new_ImVec4(0.50f, 0.50f, 0.50f, 1.00f));
                        style.SetColor(ColorTarget.ColumnHovered, new_ImVec4(0.70f, 0.60f, 0.60f, 1.00f));
                        style.SetColor(ColorTarget.ColumnActive, new_ImVec4(0.90f, 0.70f, 0.70f, 1.00f));
                        style.SetColor(ColorTarget.ResizeGrip, new_ImVec4(0.00f, 0.00f, 0.00f, 0.30f));
                        style.SetColor(ColorTarget.ResizeGripHovered, new_ImVec4(0.00f, 0.00f, 0.00f, 0.37f));
                        style.SetColor(ColorTarget.ResizeGripActive, new_ImVec4(0.00f, 0.00f, 0.00f, 0.47f));
                        style.SetColor(ColorTarget.CloseButton, new_ImVec4(0.00f, 0.00f, 0.00f, 0.18f));
                        style.SetColor(ColorTarget.CloseButtonHovered, new_ImVec4(0.10f, 0.10f, 0.10f, 0.12f));
                        style.SetColor(ColorTarget.CloseButtonActive, new_ImVec4(0.30f, 0.30f, 0.30f, 0.08f));
                        style.SetColor(ColorTarget.PlotLines, new_ImVec4(0.41f, 0.41f, 0.41f, 1.00f));
                        style.SetColor(ColorTarget.PlotLinesHovered, new_ImVec4(0.69f, 0.56f, 0.12f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogram, new_ImVec4(0.64f, 0.50f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogramHovered, new_ImVec4(0.37f, 0.22f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.TextSelectedBg, new_ImVec4(0.46f, 0.61f, 1.00f, 0.61f));
                        style.SetColor(ColorTarget.ModalWindowDarkening, new_ImVec4(0.80f, 0.80f, 0.80f, 0.35f));

                    }
                    break;
                case ImGuiStyle.Maya:
                    {
                        // Posted by @ongamex here https://gist.github.com/ongamex/4ee36fb23d6c527939d0f4ba72144d29
                        style.ChildWindowRounding = 3.0f;
                        style.GrabRounding = 0.0f;
                        style.WindowRounding = 0.0f;
                        style.ScrollbarRounding = 3.0f;
                        style.FrameRounding = 3.0f;
                        style.WindowTitleAlign = Align.Center | Align.VCenter;// new ImVec2(0.5f, 0.5f));

                        style.SetColor(ColorTarget.Text, new_ImVec4(0.73f, 0.73f, 0.73f, 1.00f));
                        style.SetColor(ColorTarget.TextDisabled, new_ImVec4(0.50f, 0.50f, 0.50f, 1.00f));
                        style.SetColor(ColorTarget.WindowBg, new_ImVec4(0.26f, 0.26f, 0.26f, 0.95f));
                        style.SetColor(ColorTarget.ChildWindowBg, new_ImVec4(0.28f, 0.28f, 0.28f, 1.00f));
                        style.SetColor(ColorTarget.PopupBg, new_ImVec4(0.26f, 0.26f, 0.26f, 1.00f));
                        style.SetColor(ColorTarget.Border, new_ImVec4(0.26f, 0.26f, 0.26f, 1.00f));
                        style.SetColor(ColorTarget.BorderShadow, new_ImVec4(0.26f, 0.26f, 0.26f, 1.00f));
                        style.SetColor(ColorTarget.FrameBg, new_ImVec4(0.16f, 0.16f, 0.16f, 1.00f));
                        style.SetColor(ColorTarget.FrameBgHovered, new_ImVec4(0.16f, 0.16f, 0.16f, 1.00f));
                        style.SetColor(ColorTarget.FrameBgActive, new_ImVec4(0.16f, 0.16f, 0.16f, 1.00f));
                        style.SetColor(ColorTarget.TitleBg, new_ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.TitleBgCollapsed, new_ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.TitleBgActive, new_ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.MenuBarBg, new_ImVec4(0.26f, 0.26f, 0.26f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarBg, new_ImVec4(0.21f, 0.21f, 0.21f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarGrab, new_ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarGrabHovered, new_ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.ScrollbarGrabActive, new_ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.ComboBg, new_ImVec4(0.32f, 0.32f, 0.32f, 1.00f));
                        style.SetColor(ColorTarget.CheckMark, new_ImVec4(0.78f, 0.78f, 0.78f, 1.00f));
                        style.SetColor(ColorTarget.SliderGrab, new_ImVec4(0.74f, 0.74f, 0.74f, 1.00f));
                        style.SetColor(ColorTarget.SliderGrabActive, new_ImVec4(0.74f, 0.74f, 0.74f, 1.00f));
                        style.SetColor(ColorTarget.Button, new_ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.ButtonHovered, new_ImVec4(0.43f, 0.43f, 0.43f, 1.00f));
                        style.SetColor(ColorTarget.ButtonActive, new_ImVec4(0.11f, 0.11f, 0.11f, 1.00f));
                        style.SetColor(ColorTarget.Header, new_ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.HeaderHovered, new_ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.HeaderActive, new_ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.Column, new_ImVec4(0.39f, 0.39f, 0.39f, 1.00f));
                        style.SetColor(ColorTarget.ColumnHovered, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.ColumnActive, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.ResizeGrip, new_ImVec4(0.36f, 0.36f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.ResizeGripHovered, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.ResizeGripActive, new_ImVec4(0.26f, 0.59f, 0.98f, 1.00f));
                        style.SetColor(ColorTarget.CloseButton, new_ImVec4(0.59f, 0.59f, 0.59f, 1.00f));
                        style.SetColor(ColorTarget.CloseButtonHovered, new_ImVec4(0.98f, 0.39f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.CloseButtonActive, new_ImVec4(0.98f, 0.39f, 0.36f, 1.00f));
                        style.SetColor(ColorTarget.PlotLines, new_ImVec4(0.39f, 0.39f, 0.39f, 1.00f));
                        style.SetColor(ColorTarget.PlotLinesHovered, new_ImVec4(1.00f, 0.43f, 0.35f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogram, new_ImVec4(0.90f, 0.70f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.PlotHistogramHovered, new_ImVec4(1.00f, 0.60f, 0.00f, 1.00f));
                        style.SetColor(ColorTarget.TextSelectedBg, new_ImVec4(0.32f, 0.52f, 0.65f, 1.00f));
                        style.SetColor(ColorTarget.ModalWindowDarkening, new_ImVec4(0.20f, 0.20f, 0.20f, 0.50f));

                    }
                    break;
                default:
                    break;
            }

            return true;
        }

        // Convert rgb floats ([0-1],[0-1],[0-1]) to hsv floats ([0-1],[0-1],[0-1]), from Foley & van Dam p592
        // Optimized http://lolengine.net/blog/2013/01/13/fast-rgb-to-hsv
        public static void ColorConvertRGBtoHSV(float r, float g, float b, ref float out_h, ref float out_s, ref float out_v)
        {
            float K = 0.0f;
            if(g < b)
            {
                float tmp = g; g = b; b = tmp;
                K = -1.0f;
            }
            if(r < g)
            {
                float tmp = r; r = g; g = tmp;
                K = -2.0f / 6.0f - K;
            }

            float chroma = r - (g < b ? g : b);
            out_h = Math.Abs(K + (g - b) / (6.0f * chroma + 1e-20f));
            out_s = chroma / (r + 1e-20f);
            out_v = r;
        }

        // Convert hsv floats ([0-1],[0-1],[0-1]) to rgb floats ([0-1],[0-1],[0-1]), from Foley & van Dam p593
        // also http://en.wikipedia.org/wiki/HSL_and_HSV
        public static void ColorConvertHSVtoRGB(float h, float s, float v, ref float out_r, ref float out_g, ref float out_b)
        {
            if(s == 0.0f)
            {
                // gray
                out_r = out_g = out_b = v;
                return;
            }

            h = (h % 1.0f) / (60.0f / 360.0f);
            int i = (int)h;
            float f = h - (float)i;
            float p = v * (1.0f - s);
            float q = v * (1.0f - s * f);
            float t = v * (1.0f - s * (1.0f - f));

            switch(i)
            {
                case 0: out_r = v; out_g = t; out_b = p; break;
                case 1: out_r = q; out_g = v; out_b = p; break;
                case 2: out_r = p; out_g = v; out_b = t; break;
                case 3: out_r = p; out_g = q; out_b = v; break;
                case 4: out_r = t; out_g = p; out_b = v; break;
                case 5: default: out_r = v; out_g = p; out_b = q; break;
            }
        }
    }
}
