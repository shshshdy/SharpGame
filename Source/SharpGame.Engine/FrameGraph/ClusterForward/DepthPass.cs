using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class DepthPass : GraphicsPass
    {
        public DepthPass() : base(Pass.Depth)
        {
        }
    }
}
