using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;


namespace SharpGame
{
    public class RenderPipeline : Object, IEnumerable<FrameGraphPass>
    {
        public RenderTarget[] RenderTargets { get; set; }

        public List<FrameGraphPass> RenderPassList { get; set; } = new List<FrameGraphPass>();

        public RenderView View { get; private set; }

        bool initialized = false;
        
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

        public T Get<T>() where T : FrameGraphPass
        {
            foreach(var rp in RenderPassList)
            {
                if(rp is T)
                {
                    return rp as T;
                }
            }
            return null;
        }


        public void AddGraphicsPass(Action<GraphicsPass, RenderView> onDraw)
        {
            var renderPass = new GraphicsPass
            {
                OnDraw = onDraw
            };
            
            AddRenderPass(renderPass);
        }

        public void InsertGraphicsPass(int index, Action<GraphicsPass, RenderView> onDraw)
        {
            var renderPass = new GraphicsPass
            {
                OnDraw = onDraw
            };

            InsertRenderPass(index, renderPass);
        }

        public void AddComputePass(Action<ComputePass, RenderView> onDraw)
        {
            var renderPass = new ComputePass
            {
                OnDraw = onDraw,
            };

            AddRenderPass(renderPass);
        }

        public void InsertRenderPass(int index, FrameGraphPass renderPass)
        {
            RenderPassList.Insert(index, renderPass);
            renderPass.FrameGraph = this;

            if (initialized)
            {
                renderPass.Init();
            }
        }

        public void AddRenderPass(FrameGraphPass renderPass)
        {
            RenderPassList.Add(renderPass);
            renderPass.FrameGraph = this;

            if(initialized)
            {
                renderPass.Init();
            }
        }

        public int IndexOf(FrameGraphPass frameGraphPass)
        {
            return RenderPassList.IndexOf(frameGraphPass);
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
                    renderPass.Submit(cb, imageIndex);
                }
            }
        }

        public void Add(FrameGraphPass renderPass)
        {
            RenderPassList.Add(renderPass);
            renderPass.FrameGraph = this;
        }

        public IEnumerator<FrameGraphPass> GetEnumerator()
        {
            return RenderPassList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)RenderPassList).GetEnumerator();
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

        protected virtual void OnShutdown()
        {
        }
    }

    public class ForwardRenderer : RenderPipeline
    {
        public ForwardRenderer()
        {
            Add(new ShadowPass());
            Add(new ScenePass());
        }

    }

}
