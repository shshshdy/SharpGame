using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class RenderPipeline : Object
    {
        protected RenderView view;
        protected FrameGraph frameGraph;

        public RenderPipeline(RenderView view)
        {
            this.view = view;
            frameGraph = view.FrameGraph;
        }

        public virtual void Init()
        {
        }

        public virtual void Reset()
        {
        }

        public virtual void Update()
        {
        }

    }

    public class DefaultRenderer : RenderPipeline
    {
        public DefaultRenderer(RenderView view) : base(view)
        {
        }

        public override void Init()
        {
            frameGraph.Add(new ShadowPass());
            frameGraph.Add(new ScenePass());
        }

    }
}
