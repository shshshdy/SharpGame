using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;


namespace SharpGame
{
    public struct DrawEvent
    {
        public RenderPass rendrPass;
        public RenderContext renderContext;
        public CommandBuffer cmd;
    }

    public class RenderPipeline : Object
    {
        public List<FrameGraphPass> RenderPassList { get; } = new List<FrameGraphPass>();

        public RenderView View { get; private set; }

        bool initialized = false;

        public Graphics Graphics => Graphics.Instance;
        public FrameGraph FrameGraph => FrameGraph.Instance;

        protected RenderTexture colorTexture;
        public RenderTexture ColorTexture => colorTexture;

        protected RenderTexture depthTexture;
        public RenderTexture DepthTexture => depthTexture;

        public RenderTarget RenderTarget { get; }

        public RenderPipeline()
        {
            RenderTarget = new RenderTarget(Graphics.Width, Graphics.Height);
        }

        public void Init(RenderView renderView)
        {
            if (initialized)
            {
                return;
            }

            View = renderView;

            OnCreateRenderTarget();

            foreach (var rp in RenderPassList)
            {
                rp.Init();
            }

            OnInit();

            initialized = true;
        }

        public virtual void DeviceLost()
        {
            RenderTarget.Clear();

            foreach (var rp in RenderPassList)
            {
                rp.DeviceLost();
            }
        }
          
        public virtual void DeviceReset()
        {
            OnCreateRenderTarget();

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

        public FrameGraphPass AddGraphicsPass(Action<GraphicsSubpass, RenderContext, CommandBuffer> onDraw)
        {
            var renderPass = new FrameGraphPass
            {
                new AttachmentInfo(Graphics.Swapchain.ColorFormat),

                new GraphicsSubpass
                {
                    OnDraw = onDraw
                }

            };

            AddRenderPass(renderPass);
            return renderPass;
        }

        public ComputePass AddComputePass(Action<ComputePass, RenderContext, CommandBuffer> onDraw)
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

        public void Draw(RenderContext rc)
        {
            Profiler.BeginSample("FrameGraph.Draw");

            foreach (var renderPass in RenderPassList)
            {
                var cmd = rc.GetCmdBuffer(renderPass.Queue);
                OnBeginPass(renderPass, cmd);
                renderPass.Draw(rc, cmd);
                OnEndPass(renderPass, cmd);
            }

            Profiler.EndSample();
        }

        public RenderPipeline Add(FrameGraphPass renderPass)
        {
            AddRenderPass(renderPass);
            return this;
        }

        protected virtual void OnInit()
        {
        }

        protected virtual void OnCreateRenderTarget()
        {
            colorTexture = RenderTarget.Add(Graphics.Swapchain);            
            depthTexture = RenderTarget.Add(Graphics.DepthFormat, VkImageUsageFlags.DepthStencilAttachment | VkImageUsageFlags.Sampled, VkSampleCountFlags.Count1, SizeHint.Full);
        }

        protected virtual void OnUpdate()
        {
        }

        protected virtual void OnBeginPass(FrameGraphPass renderPass, CommandBuffer cmd)
        {
        }

        protected virtual void OnEndPass(FrameGraphPass renderPass, CommandBuffer cmd)
        {
        }

        protected virtual void OnShutdown()
        {
        }
    }


}
