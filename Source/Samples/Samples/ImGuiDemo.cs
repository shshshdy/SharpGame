using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 5)]
    public class ImGuiDemo : Sample
    {
        public override void OnGUI()
        {
            ImGui.ShowDemoWindow();
        }
    }

}
