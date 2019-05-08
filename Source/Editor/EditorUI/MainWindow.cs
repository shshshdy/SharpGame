using System;
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
        private bool show_test_window = true;
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
                  
            ImGui.EndMainMenuBar();

            if (show_test_window)
            {
                ImGui.SetNextWindowPos(new ImVec2(650, 20), ImGuiCond.FirstUseEver);
                ImGui.ShowDemoWindow(ref show_test_window);
            }

        }
        /*

        unsafe int TextEditCallback(ImGuiTextEditCallbackDataPtr data)
        {
            return 0;
        }*/

    }
}
