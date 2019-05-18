using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct RenderItem
    {
        public Geometry geometry;

    }

    public class RenderQueue
    {
        private RenderItem[] renderItems_ = new RenderItem[4096];

    }
}
