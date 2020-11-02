using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SharpGame
{
    public class FrameGraphPass : Object, IEnumerable<Subpass>
    {
        public RenderPipeline Renderer { get; set; }
        public RenderView View => Renderer?.View;
        public Graphics Graphics => Graphics.Instance;
        public FrameGraph FrameGraph => FrameGraph.Instance;
        public string Name { get; }
        public SubmitQueue Queue { get; set; } = SubmitQueue.Graphics;
        public RenderTextureInfo[] Attachments { get; set; }
        public RenderTarget renderTarget;
        public RenderPass RenderPass { get; set; }
        public bool UseSecondCmdBuffer { get; set; } = false;

        protected Framebuffer[] framebuffers;
        public Framebuffer[] Framebuffers => framebuffers;

        public ClearColorValue[] ClearColorValue { get; set; } = { new ClearColorValue(0.25f, 0.25f, 0.25f, 1) };
        public ClearDepthStencilValue? ClearDepthStencilValue { get; set; } = new ClearDepthStencilValue(1.0f, 0);

        protected ClearValue[] clearValues = new ClearValue[5];

        List<Subpass> subpasses = new List<Subpass>();
        public Subpass[] Subpasses
        {
            set
            {
                foreach(var subpass in value)
                {
                    Add(subpass);
                }
            }
        }

        public Func<RenderPass> renderPassCreator { get; set; }
        public Func<RenderPass, Framebuffer[]> frameBufferCreator { get; set; }

        public FrameGraphPass()
        {
        }

        public FrameGraphPass(string name)
        {
            Name = name;
        }

        public void Add(Subpass subpass)
        {
            subpass.FrameGraphPass = this;
            subpass.subpassIndex = (uint)subpasses.Count;
            subpasses.Add(subpass);
        }

        public virtual void Init()
        {
            foreach(var subpass in subpasses)
            {
                subpass.Init();
            }
        }

        public virtual void DeviceLost()
        {
            foreach (var subpass in subpasses)
            {
                subpass.DeviceLost();
            }

            RenderPass = null;
            framebuffers = null;

        }

        public virtual void DeviceReset()
        {
            CreateRenderPass();
            CreateRenderTargets();

            foreach (var subpass in subpasses)
            {
                subpass.DeviceReset();
            }

        }

        protected virtual void CreateRenderTargets()
        {
            if (frameBufferCreator != null)
            {
                framebuffers = frameBufferCreator.Invoke(RenderPass);
            }
            else
            {
                framebuffers = Graphics.Framebuffers;
            }

        }

        protected virtual void CreateRenderPass()
        {
            if(renderPassCreator != null)
            {
                RenderPass = renderPassCreator.Invoke();
            }
            else
            {
                RenderPass = Graphics.RenderPass;
            }
        }

        public virtual void Update()
        {
            foreach (var subpass in subpasses)
            {
                subpass.Update();
            }
        }

        public virtual void Draw(RenderContext rc, CommandBuffer cmd)
        {
            BeginRenderPass(cmd);

            for(int i = 0; i < subpasses.Count; i++)
            {
                if(i > 0)
                {
                    cmd.NextSubpass(SubpassContents.Inline);
                }

                subpasses[i].Draw(rc, cmd);

            }

            EndRenderPass(cmd);

        }

        Viewport viewport;
        Rect2D renderArea;
        public void BeginRenderPass(CommandBuffer cb)
        {
            if(RenderPass == null)
            {
                CreateRenderPass();
            }

            if(framebuffers == null)
            {
                CreateRenderTargets();
            }


            ref Framebuffer framebuffer = ref framebuffers[Graphics.WorkImage];

            if (framebuffer == null)
            {
                framebuffer = Graphics.Framebuffers[Graphics.WorkImage];
            }

            if (View != null)
            {
                viewport = View.Viewport;
                renderArea = View.ViewRect;
            }
            else
            {
                viewport = new Viewport(0, 0, Graphics.Width, Graphics.Height);
                renderArea = new Rect2D(0, 0, Graphics.Width, Graphics.Height);
            }

            int clearValuesCount = 0;
            if (ClearColorValue != null)
            {
                clearValuesCount = ClearColorValue.Length;
            }

            if (ClearDepthStencilValue.HasValue)
            {
                clearValuesCount += 1;
            }

            if (clearValues.Length != clearValuesCount)
            {
                Array.Resize(ref clearValues, clearValuesCount);
            }

            if (ClearColorValue != null)
            {
                for (int i = 0; i < ClearColorValue.Length; i++)
                {
                    clearValues[i] = ClearColorValue[i];
                }
            }

            if (ClearDepthStencilValue.HasValue)
            {
                clearValues[clearValues.Length - 1] = ClearDepthStencilValue.Value;
            }

            BeginRenderPass(cb, framebuffer, renderArea, clearValues);

            cb.SetViewport(in viewport);
            cb.SetScissor(in renderArea);
        }

        public void BeginRenderPass(CommandBuffer cb, Framebuffer framebuffer, Rect2D renderArea, ClearValue[] clearValues)
        {
            var rpBeginInfo = new RenderPassBeginInfo(framebuffer.renderPass, framebuffer, renderArea, clearValues);
            cb.BeginRenderPass(in rpBeginInfo, UseSecondCmdBuffer? SubpassContents.SecondaryCommandBuffers : SubpassContents.Inline);
        }

        public void EndRenderPass(CommandBuffer cb)
        {
            cb.EndRenderPass();
        }

        public virtual void Shutdown()
        {
        }

        public IEnumerator<Subpass> GetEnumerator()
        {
            return ((IEnumerable<Subpass>)subpasses).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Subpass>)subpasses).GetEnumerator();
        }

    }


}
