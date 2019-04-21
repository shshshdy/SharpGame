using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{

    public class View : Object
    {
        public Scene scene;
        public Camera camera;

        public RenderPath renderPath;

        private List<RenderPass> renderPasses = new List<RenderPass>();

        internal PipelineViewportStateCreateInfo viewportStateCreateInfo;

        public void Update()
        {

        }
    }
}
