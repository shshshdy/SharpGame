using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using VulkanCore;


namespace SharpGame
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WorldViewProjection
    {
        public Matrix World;
        public Matrix View;
        public Matrix ViewInv;
        public Matrix ViewProj;
    }

    public class ScenePass : RenderPass
    {
        public AttachmentDescription[] attachments { get; set; }
        public SubpassDescription[] subpasses { get; set; }


        private ResourceLayout descriptorSetLayout_;

        private ResourceSet descriptorSet_;

        private Texture _cubeTexture;
        private GraphicsBuffer _uniformBuffer;
        private WorldViewProjection _wvp;
        public ScenePass(string name = "main")
        {
            Name = name;

            Recreate();

            _cubeTexture = ResourceCache.Load<Texture>("IndustryForgedDark512.ktx").Result;
            _uniformBuffer = UniformBuffer.Create<WorldViewProjection>(1);

            descriptorSetLayout_ = new ResourceLayout(
                new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                new DescriptorSetLayoutBinding(1, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment)
            );

            descriptorSet_ = new ResourceSet(descriptorSetLayout_, _uniformBuffer, _cubeTexture);

            pipeline_ = new Pipeline
            {
                PipelineLayoutInfo = new PipelineLayoutCreateInfo(new[] { descriptorSetLayout_ .descriptorSetLayout}),
            };

        }

        protected override void Recreate()
        {
            var renderer = Get<Renderer>();
            attachments = new[]
            {
                // Color attachment.
                new AttachmentDescription
                {
                    Format = Graphics.Swapchain.Format,
                    Samples = SampleCounts.Count1,
                    LoadOp = AttachmentLoadOp.Clear,
                    StoreOp = AttachmentStoreOp.Store,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.PresentSrcKhr
                },
                // Depth attachment.
                new AttachmentDescription
                {
                    Format = Graphics.DepthStencilBuffer.Format,
                    Samples = SampleCounts.Count1,
                    LoadOp = AttachmentLoadOp.Clear,
                    StoreOp = AttachmentStoreOp.DontCare,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
                }
            };

            subpasses = new[]
            {
                new SubpassDescription(
                    new[] { new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal) },
                    new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal))
            };

            var dependencies = new[]
            {
                new SubpassDependency
                {
                    SrcSubpass = Constant.SubpassExternal,
                    DstSubpass = 0,
                    SrcStageMask = PipelineStages.BottomOfPipe,
                    DstStageMask = PipelineStages.ColorAttachmentOutput,
                    SrcAccessMask = Accesses.MemoryRead,
                    DstAccessMask = Accesses.ColorAttachmentRead | Accesses.ColorAttachmentWrite,
                    DependencyFlags = Dependencies.ByRegion
                },
                new SubpassDependency
                {
                    SrcSubpass = 0,
                    DstSubpass = Constant.SubpassExternal,
                    SrcStageMask = PipelineStages.ColorAttachmentOutput,
                    DstStageMask = PipelineStages.BottomOfPipe,
                    SrcAccessMask = Accesses.ColorAttachmentRead | Accesses.ColorAttachmentWrite,
                    DstAccessMask = Accesses.MemoryRead,
                    DependencyFlags = Dependencies.ByRegion
                }
            };

            var createInfo = new RenderPassCreateInfo(subpasses, attachments, dependencies);
            renderPass_ = Graphics.ToDisposeFrame(Graphics.Device.CreateRenderPass(createInfo));
            framebuffer_ = Graphics.ToDisposeFrame(CreateFramebuffers());

        }

        protected Framebuffer[] CreateFramebuffers()
        {
            var framebuffers = new Framebuffer[Graphics.SwapchainImages.Length];
            for (int i = 0; i < Graphics.SwapchainImages.Length; i++)
            {
                framebuffers[i] = CreateFramebuffer(
                    new[] {
                        Graphics.SwapchainImageViews[i], Graphics.DepthStencilBuffer.View
                    },

                    Graphics.Width, Graphics.Height
                );
            }

            return framebuffers;
        }

        private DescriptorPool CreateDescriptorPool()
        {
            var descriptorPoolSizes = new[]
            {
                new DescriptorPoolSize(DescriptorType.UniformBuffer, 1),
                new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 1)
            };
            return Graphics.CreateDescriptorPool(descriptorPoolSizes);
        }



        protected override void OnDraw(RenderView view, CommandBuffer cmdBuffer)
        {
            if(view.Camera)
            {
                _wvp.World = Matrix.Identity;
                _wvp.View = view.Camera.View;
                Matrix.Invert(ref _wvp.View, out _wvp.ViewInv);
                _wvp.ViewProj = _wvp.View * view.Camera.Projection;

                IntPtr ptr = _uniformBuffer.Map(0, Interop.SizeOf<WorldViewProjection>());
                Interop.Write(ptr, ref _wvp);
                _uniformBuffer.Unmap();
            }


            foreach (var drawable in view.drawables_)
            {
                for(int i = 0; i < drawable.Batches.Length; i++)
                {
                    ref SourceBatch batch = ref drawable.Batches[i];
                    this.DrawBatch(cmdBuffer, ref batch, descriptorSet_);
                }
            }
        }
    }


}
