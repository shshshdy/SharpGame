using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
    public unsafe class ImGUI : Sample
    {
        public ImGUI()
        {
        }


        public override void OnGUI()
        {
            ImGui.ShowDemoWindow();
        }


    }
}
