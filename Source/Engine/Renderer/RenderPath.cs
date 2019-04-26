using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct RenderPassDesc
    {

    }

    public class RenderPath : Resource
    {
        public List<RenderPassDesc> renderPasses_ = new List<RenderPassDesc>();

    }

    public static class RenderPassHelper
    {
        public static RenderPath RenderPath()
        {
            return new RenderPath();
        }
    }
}
