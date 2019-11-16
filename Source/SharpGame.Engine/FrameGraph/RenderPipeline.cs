using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;


namespace SharpGame
{
    public class RenderPipeline : Object
    {
        public List<FrameGraphPass> RenderPassList { get; set; } = new List<FrameGraphPass>();
        public PostProcess PostProcess { get; } = new PostProcess();

        public RenderView View { get; private set; }

        bool initialized = false;

        public Graphics Graphics => Graphics.Instance;
        public FrameGraph FrameGraph => FrameGraph.Instance;

        public RenderPipeline()
        {
        }

        public void Init(RenderView renderView)
        {
            if (initialized)
            {
                return;
            }

            View = renderView;

            PostProcess.Init();

            OnInit();

            foreach (var rp in RenderPassList)
            {
                rp.Init();
            }

            initialized = true;
        }

        public void Reset()
        {
            OnReset();

            foreach (var rp in RenderPassList)
            {
                rp.Reset();
            }
        }

        public void Shutdown()
        {
            foreach (var rp in RenderPassList)
            {
                rp.Shutdown();
            }

            OnShutdown();

            initialized = false;
        }

        public GraphicsPass AddGraphicsPass(Action<GraphicsPass, RenderView> onDraw)
        {
            var renderPass = new GraphicsPass
            {
                OnDraw = onDraw
            };
            
            AddRenderPass(renderPass);
            return renderPass;
        }

        public T AddPass<T>(Action<GraphicsPass, RenderView> onDraw) where T : GraphicsPass, new()
        {
            var renderPass = new T
            {
                OnDraw = onDraw
            };

            AddRenderPass(renderPass);
            return renderPass;
        }

        public GraphicsPass InsertGraphicsPass(int index, Action<GraphicsPass, RenderView> onDraw)
        {
            var renderPass = new GraphicsPass
            {
                OnDraw = onDraw
            };

            InsertRenderPass(index, renderPass);
            return renderPass;
        }

        public ComputePass AddComputePass(Action<ComputePass, RenderView> onDraw)
        {
            var renderPass = new ComputePass
            {
                OnDraw = onDraw,
            };

            AddRenderPass(renderPass);
            return renderPass;
        }

        public void InsertRenderPass(int index, FrameGraphPass renderPass)
        {
            RenderPassList.Insert(index, renderPass);
            renderPass.Renderer = this;

            if (initialized)
            {
                renderPass.Init();
            }
        }

        public void AddRenderPass(FrameGraphPass renderPass)
        {
            RenderPassList.Add(renderPass);
            renderPass.Renderer = this;

            if(initialized)
            {
                renderPass.Init();
            }
        }

        public void Update()
        {
            Profiler.BeginSample("FrameGraph.Update");

            OnUpdate();

            foreach (var renderPass in RenderPassList)
            {
                renderPass.Update(View);
            }

            Profiler.EndSample();
        }

        public void Draw()
        {
            Profiler.BeginSample("FrameGraph.Draw");
            foreach (var renderPass in RenderPassList)
            {
                renderPass.Draw(View);
            }
            Profiler.EndSample();
        }

        public void Submit(CommandBuffer cb, PassQueue passQueue, int imageIndex)
        {
            foreach (var renderPass in RenderPassList)
            {
                if((renderPass.PassQueue & passQueue) == renderPass.PassQueue)
                {
                    OnBeginSubmit(renderPass, cb, imageIndex);
                    renderPass.Submit(cb, imageIndex);
                    OnEndSubmit(renderPass, cb, imageIndex);
                }
            }
        }

        public RenderPipeline Add(FrameGraphPass renderPass)
        {
            RenderPassList.Add(renderPass);
            renderPass.Renderer = this;
            return this;
        }

        protected virtual void OnInit()
        {
        }

        protected virtual void OnReset()
        {
        }

        protected virtual void OnUpdate()
        {
        }

        protected virtual void OnBeginSubmit(FrameGraphPass renderPass, CommandBuffer cb, int imageIndex)
        {
        }

        protected virtual void OnEndSubmit(FrameGraphPass renderPass, CommandBuffer cb, int imageIndex)
        {
        }

        protected virtual void OnShutdown()
        {
        }
    }

    public class ForwardRenderer : RenderPipeline
    {
        public ForwardRenderer()
        {
            Add(new ShadowPass())
            .Add(new ScenePass());
        }

    }

}
