using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    /*
    public class RenderPass : IDeviceObject
    {
        internal VulkanCore.RenderPass renderPass;

        public void Dispose()
        {
            renderPass?.Dispose();
        }


        
        public void Recreate()
        {
        }
    }*/
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
        internal PipelineViewportStateCreateInfo viewportStateCreateInfo;
    }
}
