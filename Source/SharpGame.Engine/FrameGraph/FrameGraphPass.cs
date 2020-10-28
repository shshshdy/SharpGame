using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SharpGame
{
    public enum SubmitQueue
    {
        EarlyGraphics = 0,
        Compute = 1,
        Graphics = 2,
        MaxCount
    }

    public class FrameGraphPass : Object, IEnumerable<Subpass>
    {
        public RenderPipeline Renderer { get; set; }
        public RenderView View => Renderer?.View;
        public CommandBuffer CmdBuffer => FrameGraph.GetWorkCmdBuffer(Queue);
        public Graphics Graphics => Graphics.Instance;
        public FrameGraph FrameGraph => FrameGraph.Instance;

        public string Name { get; }

        public SubmitQueue Queue { get; set; } = SubmitQueue.Graphics;

        public RenderTextureInfo[] Attachments { get; set; }

        public RenderTarget renderTarget;

        public RenderPass RenderPass { get; set; }
        public uint Subpass { get; set; }
        public bool UseSecondCmdBuffer { get; set; } = false;

        public Framebuffer[] Framebuffers { get => framebuffers; set => framebuffers = value; }
        Framebuffer[] framebuffers = new Framebuffer[3];

        [IgnoreDataMember]
        public Framebuffer Framebuffer { set => framebuffers[0] = framebuffers[1] = framebuffers[2] = value; }
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
            RenderPass = null;
            Framebuffers = null;
        }

        public virtual void DeviceReset()
        {
            CreateRenderPass();

            CreateRenderTargets();

        }

        private void CreateRenderTargets()
        {
            if (frameBufferCreator != null)
            {
                framebuffers = frameBufferCreator.Invoke(RenderPass);
            }

        }

        private void CreateRenderPass()
        {
            if(renderPassCreator != null)
            {
                RenderPass = renderPassCreator.Invoke();
            }

        }

        public virtual void Update()
        {
            foreach (var subpass in subpasses)
            {
                subpass.Update();
            }
        }

        public virtual void Draw()
        {
            var cmd = CmdBuffer;

            BeginRenderPass(cmd);
            

            uint subpassIndex = 0;
            foreach (var subpass in subpasses)
            {
                if(subpassIndex > 0)
                {
                    cmd.NextSubpass(SubpassContents.Inline);
                }

                subpass.Draw(cmd, subpassIndex);
                subpassIndex++;
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

            if(Framebuffers == null)
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

            BeginRenderPass(framebuffer, renderArea, clearValues);

            cb.SetViewport(in viewport);
            cb.SetScissor(in renderArea);
        }

        public void BeginRenderPass(Framebuffer framebuffer, Rect2D renderArea, ClearValue[] clearValues)
        {
            var cb = CmdBuffer;
            var rpBeginInfo = new RenderPassBeginInfo(framebuffer.renderPass, framebuffer, renderArea, clearValues);

            cb.BeginRenderPass(in rpBeginInfo, UseSecondCmdBuffer? SubpassContents.SecondaryCommandBuffers : SubpassContents.Inline);

            Subpass = 0;
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
