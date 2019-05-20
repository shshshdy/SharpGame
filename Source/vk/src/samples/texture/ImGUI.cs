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
    public unsafe class ImGUI : Application
    {
        public ImGUI()
        {
        }

        protected override void Init()
        {
            base.Init();

            Node node = new Node
            {
                Position = new Vector3(),

                Rotation = new Quaternion(),

                Components = new []
                {
                    new Camera
                    {
                    }
                },

                Children = new[]
                {
                    new Node
                    {
                    }
                }
            };


            this.SubscribeToEvent<GUIEvent>(Handle);

        }


        void Handle(GUIEvent e)
        {

            ImGui.ShowDemoWindow();
        }


        public static void Main() => new SampleApplication().Run();
    }
}
