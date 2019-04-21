using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    /*
    public class FrameBuffer : IDeviceObject
    {
        internal VulkanCore.Framebuffer framebuffer;
        internal VulkanCore.ImageView[] imageViews;

        public void Dispose()
        {
        }

        public void Recreate()
        {
        }


    }
*/

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
