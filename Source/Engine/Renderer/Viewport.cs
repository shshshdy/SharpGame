using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class Viewport
    {
        public VulkanCore.Viewport viewport;
        public Scene scene;
        public Camera camera;
        public RenderPath renderPath;
        public View view;
        public Viewport()
        {
            view = new View();
        }
    }
}
