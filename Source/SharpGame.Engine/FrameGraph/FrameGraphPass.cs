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

        public VkClearValue[] clearValues = new VkClearValue[2]
        { 
            new VkClearColorValue(0.25f, 0.25f, 0.25f, 1),
            new VkClearDepthStencilValue(1.0f, 0)
        };

        List<Subpass> subpasses = new List<Subpass>();

        public Func<RenderPass> renderPassCreator { get; set; }
        public Func<RenderTarget> renderTargetCreator { get; set; }

        protected RenderTarget renderTarget;
        public RenderTarget RenderTarget => renderTarget;

        private List<AttachmentInfo> renderTextureInfos = new List<AttachmentInfo>();
        
        public FrameGraphPass(SubmitQueue queue = SubmitQueue.Graphics)
        {
            Queue = queue;
        }

        public void AddAttachment(AttachmentInfo attachment)
        {
            renderTextureInfos.Add(attachment);
        }

        public void AddSubpass(Subpass subpass)
        {
            subpass.FrameGraphPass = this;
            subpass.SubpassIndex = (uint)subpasses.Count;
            subpasses.Add(subpass);
        }

        public virtual void Init()
        {
            CreateRenderPass();
            CreateRenderTargets();

            foreach (var subpass in subpasses)
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

        protected virtual void CreateRenderPass()
        {
            if (renderPassCreator != null)
            {
                RenderPass = renderPassCreator.Invoke();
            }
            else if(!renderTextureInfos.Empty())
            {   
                var attachmentDescriptions = new VkAttachmentDescription[renderTextureInfos.Count];
                var subpassDescriptions = new SubpassDescription[subpasses.Count];
                var dependencies = new VkSubpassDependency[subpasses.Count + 1];
                
                for(int i = 0; i < renderTextureInfos.Count; i++)
                {
                    attachmentDescriptions[i] = renderTextureInfos[i].attachmentDescription;       
                }
            
                dependencies[0] = new VkSubpassDependency
                {
                    srcSubpass = Vulkan.SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = VkPipelineStageFlags.BottomOfPipe,
                    dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                    srcAccessMask = VkAccessFlags.MemoryRead,
                    dstAccessMask = (VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite),
                    dependencyFlags = VkDependencyFlags.ByRegion
                };

                for (int i = 0; i < subpasses.Count; i++)
                {
                    subpasses[i].GetDescription(attachmentDescriptions, ref subpassDescriptions[i]);

                    if(i > 0)
                    {
                        //dependencies[i] = subpasses[i].Dependency;
                        dependencies[i].srcSubpass = (uint)(i - 1);
                        dependencies[i].dstSubpass = (uint)i;
                        dependencies[i].srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
                        dependencies[i].dstStageMask = VkPipelineStageFlags.FragmentShader;
                        dependencies[i].srcAccessMask = VkAccessFlags.ColorAttachmentWrite;
                        dependencies[i].dstAccessMask = VkAccessFlags.InputAttachmentRead;
                        dependencies[i].dependencyFlags = VkDependencyFlags.ByRegion;
                    }
                }

                dependencies[subpasses.Count] = new VkSubpassDependency
                {
                    srcSubpass = (uint)(subpasses.Count - 1),
                    dstSubpass = Vulkan.SubpassExternal,
                    srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = VkPipelineStageFlags.BottomOfPipe,
                    srcAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite,
                    dstAccessMask = VkAccessFlags.MemoryRead,
                    dependencyFlags = VkDependencyFlags.ByRegion
                };

                RenderPass = new RenderPass(attachmentDescriptions, subpassDescriptions, dependencies);
            }            
            
            if (RenderPass == null)
            {
                RenderPass = Graphics.RenderPass;
            }            

        }

        protected virtual void CreateRenderTargets()
        {
            Debug.Assert(RenderPass != null);

            if (renderTargetCreator != null)
            {
                renderTarget = renderTargetCreator();
                foreach(var rt in renderTarget.Attachments)
                {
                    if (!rt.id.IsNullOrEmpty)
                    {
                        Renderer.RegResource(rt.id, rt);
                    }
                }
            }
            else
            {
                if (renderTextureInfos.Count > 0)
                {
                    renderTarget = new RenderTarget(Graphics.Width, Graphics.Height);

                    Array.Resize(ref clearValues, renderTextureInfos.Count);
                    for (int i = 0; i < renderTextureInfos.Count; i++)
                    {
                        var info = renderTextureInfos[i];
                        if(info.RTType == RTType.ColorOutput)
                        {
                            renderTarget.Add(Renderer.ColorTexture);
                        }
                        else if(info.RTType == RTType.DepthOutput)
                        {
                            renderTarget.Add(Renderer.DepthTexture);
                        }
                        else
                        {
                            var rt = renderTarget.Add(info);
                            if(!rt.id.IsNullOrEmpty)
                            {
                                Renderer.RegResource(rt.id, rt);
                            }
                        }

                        clearValues[i] = renderTextureInfos[i].ClearValue;
                    }

                }

            }

            if (renderTarget == null)
            {
                renderTarget = Renderer.RenderTarget;
            }

            framebuffers = new Framebuffer[Swapchain.IMAGE_COUNT];
            for (int i = 0; i < framebuffers.Length; i++)
            {
                var attachments = renderTarget.GetViews(i);
                framebuffers[i] = new Framebuffer(RenderPass, renderTarget.Extent.width, renderTarget.Extent.height, 1, attachments);
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
                    cmd.NextSubpass(VkSubpassContents.Inline);
                }

                subpasses[i].Draw(rc, cmd);

            }

            EndRenderPass(cmd);

        }

        VkViewport viewport;
        VkRect2D renderArea;
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

            if (View != null)
            {
                viewport = View.Viewport;
                renderArea = View.ViewRect;
            }
            else
            {
                viewport = new VkViewport(0, 0, Graphics.Width, Graphics.Height);
                renderArea = new VkRect2D(0, 0, Graphics.Width, Graphics.Height);
            }

            BeginRenderPass(cb, framebuffer, renderArea, clearValues);

            cb.SetViewport(in viewport);
            cb.SetScissor(in renderArea);
        }

        public void BeginRenderPass(CommandBuffer cb, Framebuffer framebuffer, VkRect2D renderArea, VkClearValue[] clearValues)
        {
            var rpBeginInfo = new RenderPassBeginInfo(framebuffer.renderPass, framebuffer, renderArea, clearValues);
            cb.BeginRenderPass(ref rpBeginInfo, UseSecondCmdBuffer? VkSubpassContents.SecondaryCommandBuffers : VkSubpassContents.Inline);
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
            if(obj is AttachmentInfo rt)
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
