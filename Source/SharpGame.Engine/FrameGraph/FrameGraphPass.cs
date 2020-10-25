using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SharpGame
{
    public enum PassQueue
    {
        EarlyGraphics = 0,
        Compute = 1,
        Graphics = 2,
    }

    public class FrameGraphPass : Object, IEnumerable<Subpass>
    {
        public PassQueue PassQueue { get; set; } = PassQueue.Graphics;
        public RenderPass RenderPass { get; set; }
        public uint Subpass { get; set; }

        [IgnoreDataMember]
        public RenderPipeline Renderer { get; set; }

        public CommandBuffer CmdBuffer => FrameGraph.GetWorkCmdBuffer(PassQueue);

        public Graphics Graphics => Graphics.Instance;
        public FrameGraph FrameGraph => FrameGraph.Instance;

        [IgnoreDataMember]
        public Framebuffer[] Framebuffers { get; set; }
        [IgnoreDataMember]
        public Framebuffer Framebuffer { set => Framebuffers = new[] { value, value, value }; }
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

        public FrameGraphPass()
        {
        }

        public void Add(Subpass subpass)
        {
            subpass.FrameGraphPass = this;
            subpasses.Add(subpass);
        }

        public virtual void Init()
        {
            foreach(var subpass in subpasses)
            {
                subpass.Init();
            }
        }

        public virtual void Reset()
        {
        }

        public virtual void Update(RenderView view)
        {
            foreach (var subpass in subpasses)
            {
                subpass.Update(view);
            }
        }

        public virtual void Draw(RenderView view)
        {
            BeginRenderPass(view);

            foreach (var subpass in subpasses)
            {
                subpass.Draw(view, 0);
            }

            EndRenderPass(view);
        }

        Viewport viewport;
        Rect2D renderArea;
        public void BeginRenderPass(RenderView view)
        {
            Framebuffer framebuffer = null;
            if (Framebuffers == null)
            {
                framebuffer = Graphics.Framebuffers[Graphics.WorkImage];
            }
            else
            {
                framebuffer = Framebuffers[Graphics.WorkImage];
            }

            if (view != null)
            {
                viewport = view.Viewport;
                renderArea = view.ViewRect;
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

            var cb = CmdBuffer;
            BeginRenderPass(framebuffer, renderArea, clearValues);
            cb.SetViewport(in viewport);
            cb.SetScissor(in renderArea);
        }

        public void BeginRenderPass(Framebuffer framebuffer, Rect2D renderArea, ClearValue[] clearValues)
        {
            var cb = CmdBuffer;
            var rpBeginInfo = new RenderPassBeginInfo(framebuffer.renderPass, framebuffer, renderArea, clearValues);

            cb.BeginRenderPass(in rpBeginInfo, SubpassContents.Inline);

            Subpass = 0;
        }

        public void EndRenderPass(RenderView view)
        {
            var cb = CmdBuffer;
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
