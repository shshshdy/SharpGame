﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ComputePass : FrameGraphPass
    {
        public Action<ComputePass, RenderView> OnDraw { get; set; }

        public ComputePass()
        {
            PassQueue = PassQueue.Compute;
        }

        public ComputePass(Action<ComputePass, RenderView> onDraw)
        {
            PassQueue = PassQueue.Compute;
            OnDraw = onDraw;
        }

        public override void Draw(RenderView view)
        {
            OnDraw?.Invoke(this, view);
        }
        
    }
}
