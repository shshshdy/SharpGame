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

            OnInit();

            foreach (var rp in RenderPassList)
            {
                rp.Init();
            }

            initialized = true;
        }
        public void DeviceLost()
        {
            foreach (var rp in RenderPassList)
            {
                rp.DeviceLost();
            }
        }
          
        public void DeviceReset()
        {
            OnReset();

            foreach (var rp in RenderPassList)
            {
                rp.DeviceReset();
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

        public FrameGraphPass AddGraphicsPass(Action<GraphicsSubpass, CommandBuffer> onDraw)
        {
            var renderPass = new FrameGraphPass
            {
                Subpasses = new[]
                {
                    new GraphicsSubpass
                    {
                        OnDraw = onDraw
                    }
                }
            };

            AddRenderPass(renderPass);
            return renderPass;
        }

        public FrameGraphPass AddPass<T>(Action<GraphicsSubpass, CommandBuffer> onDraw) where T : GraphicsSubpass, new()
        {
            var renderPass = new FrameGraphPass
            {
                Subpasses = new[]
                {
                    new T
                    {
                        OnDraw = onDraw
                    }
                }
            };

            AddRenderPass(renderPass);
            return renderPass;
        }

        public ComputePass AddComputePass(Action<ComputePass, CommandBuffer> onDraw)
        {
            var renderPass = new ComputePass
            {
                OnDraw = onDraw,
            };

            AddRenderPass(renderPass);
            return renderPass;
        }

        public void AddRenderPass(FrameGraphPass renderPass)
        {
            RenderPassList.Add(renderPass);
            renderPass.Renderer = this;

            if (initialized)
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
                renderPass.Update();
            }

            Profiler.EndSample();
        }

        public void Draw()
        {
            Profiler.BeginSample("FrameGraph.Draw");

            int workImage = Graphics.WorkContext;

            foreach (var renderPass in RenderPassList)
            {
                OnBeginPass(renderPass);
                renderPass.Draw();
                OnEndPass(renderPass);
            }

            Profiler.EndSample();
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

        protected virtual void OnBeginPass(FrameGraphPass renderPass)
        {
        }

        protected virtual void OnEndPass(FrameGraphPass renderPass)
        {
        }

        protected virtual void OnShutdown()
        {
        }
    }


}
