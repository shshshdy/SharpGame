using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;
using Veldrid.Sdl2;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
    public unsafe class ImGUI : SampleApp
    {
        public ImGUI()
        {
        }

        public override void Init()
        {
            base.Init();

            this.SubscribeToEvent<GUIEvent>(Handle);

            prepared = true;
        }


        void Handle(GUIEvent e)
        {

            ImGui.ShowDemoWindow();
        }


        public static void Main() => new ImGUI().Run();
    }
}
