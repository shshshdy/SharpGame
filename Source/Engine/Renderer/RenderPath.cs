using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class RenderPath : Resource
    {
        public List<RenderPass> RenderPasses { get; set; } = new List<RenderPass>();

        public RenderPath()
        {
        }

        public RenderPath(params RenderPass[] renderPasss)
        {
            RenderPasses.AddRange(renderPasss);
        }

    }

}
