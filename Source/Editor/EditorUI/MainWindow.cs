﻿using System;
using System.Collections.Generic;
using System.Text;
using ImGuiNET;

using ImVec2 = System.Numerics.Vector2;
using ImVec3 = System.Numerics.Vector3;

namespace SharpGame.Editor
{
    internal class MainWindow : EditorWindow
    {
        bool opened = true;
        byte[] textBuffer = new byte[1024];
        protected override void Draw()
        {

            ImGui.BeginMainMenuBar();

            if(ImGui.BeginMenu("File"))
            {
                if(ImGui.BeginMenu("Open"))
                {
                    ImGui.EndMenu();
                }

                if(ImGui.BeginMenu("Save"))
                {
                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }
            /*
            if(ImGui.BeginMenu("Style"))
            {
                for(ImGuiStyle i = 0; i < ImGuiStyle.Count; i++)
                {
                    if(ImGui.MenuItem(i.ToString()))
                    {
                        ImGuiUtil.ResetStyle(i, ImGui.GetStyle());
                    }
                }

                ImGui.EndMenu();
            }
            */
            ImGui.EndMainMenuBar();

            ImGui.SetNextWindowPos(
                  new ImVec2(10.0f, 50.0f)
                , ImGuiCond.FirstUseEver
                );

            ImGui.Begin("test1", ref opened//, new ImVec2(1024, 768)
                , ImGuiWindowFlags.AlwaysAutoResize
                );

            if(ImGui.Button(" Restart"))
            {
                //  cmdExec("app restart");
            }

            ImGui.Text("Test text, 测试文字");
            /*
            unsafe
            {
                if(ImGui.InputText("Input:", textBuffer, 1024, ImGuiInputTextFlags.None, TextEditCallback))
                {

                }


            }

            ImGuiStylePtr style = ImGui.GetStyle();
            for(ColorTarget i = 0; i < ColorTarget.Count; i++)
            {
                ImVec4 c = style.Colors[i];
                if(ImGui.ColorEdit4(i.ToString(), ref c, true))
                    style.SetColor(i, c);
            }
            */

            ImGui.End();
        }
        /*

        unsafe int TextEditCallback(ImGuiTextEditCallbackDataPtr data)
        {
            return 0;
        }*/

    }
}