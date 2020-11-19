using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SharpGame
{
    public class FrameGraphPass : Object, IEnumerable<Subpass>
    {
        public RenderPipeline Renderer { get; internal set; }
        public RenderView View => Renderer?.View;
        public Graphics Graphics => Graphics.Instance;
        public FrameGraph FrameGraph => FrameGraph.Instance;
        public SubmitQueue Queue { get; } = SubmitQueue.Graphics;

        public RenderPass RenderPass { get; protected set; }
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
                    AddSubpass(subpass);
                }
            }
        }

        public Func<RenderPass> renderPassCreator { get; set; }
        public Func<RenderPass, Framebuffer[]> frameBufferCreator { get; set; }

        protected RenderTarget renderTarget;

        private List<RenderTextureInfo> renderTextureInfos = new List<RenderTextureInfo>();
        
        public FrameGraphPass(SubmitQueue queue = SubmitQueue.Graphics)
        {
            Queue = queue;
        }

        public void AddAttachment(RenderTextureInfo attachment)
        {
            renderTextureInfos.Add(attachment);
        }

        public void AddSubpass(Subpass subpass)
        {
            subpass.FrameGraphPass = this;
            subpass.subpassIndex = (uint)subpasses.Count;
            subpasses.Add(subpass);
        }

        public void SetLoadOp(params AttachmentLoadOp[] attachmentLoadOp)
        {
            for(int i = 0; i < attachmentLoadOp.Length; i++)
            {
                renderTextureInfos[i].loadOp = attachmentLoadOp[i];
            }
        }

        public void SetStoreOp(params AttachmentStoreOp[] attachmentStoreOp)
        {
            for (int i = 0; i < attachmentStoreOp.Length; i++)
            {
                renderTextureInfos[i].storeOp = attachmentStoreOp[i];
            }
        }

        public void SetStencilLoadOp(params AttachmentLoadOp[] attachmentLoadOp)
        {
            for (int i = 0; i < attachmentLoadOp.Length; i++)
            {
                renderTextureInfos[i].stencilLoadOp = attachmentLoadOp[i];
            }
        }

        public void SetStencilStoreOp(params AttachmentStoreOp[] attachmentStoreOp)
        {
            for (int i = 0; i < attachmentStoreOp.Length; i++)
            {
                renderTextureInfos[i].stencilStoreOp = attachmentStoreOp[i];
            }
        }

        public void SetInitialLayout(params ImageLayout[] imageLayouts)
        {
            for (int i = 0; i < imageLayouts.Length; i++)
            {
                renderTextureInfos[i].initialLayout = imageLayouts[i];
            }
        }

        public void SetFinalLayout(params ImageLayout[] imageLayouts)
        {
            for (int i = 0; i < imageLayouts.Length; i++)
            {
                renderTextureInfos[i].finalLayout = imageLayouts[i];
            }
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
                if(renderTextureInfos.Count > 0)
                {
                    renderTarget = new RenderTarget();

                    foreach (var rtInfo in renderTextureInfos)
                    {
                        renderTarget.Add(rtInfo);
                    }

                    framebuffers = new Framebuffer[Swapchain.IMAGE_COUNT];
                    for (int i = 0; i < framebuffers.Length; i++)
                    {
                        var attachments = renderTarget.GetViews(i);
                        framebuffers[i] = new Framebuffer(RenderPass, renderTarget.extent.width, renderTarget.extent.height, 1, attachments);
                    }
                }

            }

            if (framebuffers == null)
            {
                framebuffers = Graphics.Framebuffers;
            }
        }

        protected virtual void CreateRenderPass()
        {
            if (renderPassCreator != null)
            {
                RenderPass = renderPassCreator.Invoke();
            }
            else if(!renderTextureInfos.Empty())
            {   
                var attachmentDescriptions = new AttachmentDescription[renderTextureInfos.Count];
                var subpassDescriptions = new SubpassDescription[subpasses.Count];
                var dependencies = new SubpassDependency[subpasses.Count + 1];

                for(int i = 0; i < renderTextureInfos.Count; i++)
                {
                    attachmentDescriptions[i] = renderTextureInfos[i].attachmentDescription;
                }
            
                dependencies[0] = new SubpassDependency
                {
                    srcSubpass = Vulkan.VulkanNative.SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = PipelineStageFlags.BottomOfPipe,
                    dstStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    srcAccessMask = AccessFlags.MemoryRead,
                    dstAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dependencyFlags = DependencyFlags.ByRegion
                };

                for (int i = 0; i < subpasses.Count; i++)
                {
                    subpasses[i].GetDescription(attachmentDescriptions, ref subpassDescriptions[i]);

                    if(i > 0)
                    {
                        dependencies[i] = subpasses[i].Dependency;
                        dependencies[i].srcSubpass = (uint)(i - 1);
                        dependencies[i].dstSubpass = (uint)i;
                        dependencies[i].srcStageMask = PipelineStageFlags.ColorAttachmentOutput;
                        dependencies[i].dstStageMask = PipelineStageFlags.FragmentShader;
                        dependencies[i].srcAccessMask = AccessFlags.ColorAttachmentWrite;
                        dependencies[i].dstAccessMask = AccessFlags.InputAttachmentRead;
                        dependencies[i].dependencyFlags = DependencyFlags.ByRegion;
                    }
                }

                dependencies[subpasses.Count] = new SubpassDependency
                {
                    srcSubpass = (uint)(subpasses.Count - 1),
                    dstSubpass = Vulkan.VulkanNative.SubpassExternal,
                    srcStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = PipelineStageFlags.BottomOfPipe,
                    srcAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite,
                    dstAccessMask = AccessFlags.MemoryRead,
                    dependencyFlags = DependencyFlags.ByRegion
                };

                RenderPass = new RenderPass(attachmentDescriptions, subpassDescriptions, dependencies);
            }            
            
            if (RenderPass == null)
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

        public void Add(object obj)
        {
            if(obj is RenderTextureInfo rt)
            {
                AddAttachment(rt);
            }
            else if(obj is Subpass subpass)
            {
                AddSubpass(subpass);
            }
        }

    }


}
