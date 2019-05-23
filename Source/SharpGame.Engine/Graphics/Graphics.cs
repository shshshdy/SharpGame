using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;

using static Vulkan.VulkanNative;

namespace SharpGame
{
    public struct Semaphores
    {
        public VkSemaphore PresentComplete;
        public VkSemaphore RenderComplete;
    }

    public class Settings
    {
        public bool Validation { get; set; } = true;
        public bool Fullscreen { get; set; } = false;
        public bool VSync { get; set; } = false;
    }

    public unsafe partial class Graphics : System<Graphics>
    {
        public Settings Settings { get; } = new Settings();
        public VkInstance VkInstance { get; protected set; }
        public VkPhysicalDeviceFeatures enabledFeatures;
        public NativeList<IntPtr> EnabledExtensions { get; } = new NativeList<IntPtr>();

        public static VkDevice device { get; protected set; }
        public static VkQueue queue { get; protected set; }
        public Format DepthFormat { get; protected set; }
        public Swapchain Swapchain { get; } = new Swapchain();

        public int Width { get; private set; }
        public int Height { get; private set; }

        public int ImageCount => Swapchain.ImageCount;

        private Framebuffer[] framebuffers;
        public Framebuffer[] Framebuffers => framebuffers;

        internal static DescriptorPoolManager DescriptorPoolManager { get; private set; }

        private CommandBufferPool primaryCmdPool;
        private CommandBufferPool[] secondaryCmdPool;

        public CommandBuffer RenderCmdBuffer => primaryCmdPool.CommandBuffers[currentImage];

        //todo: multithread
        public CommandBufferPool WorkCmdPool => secondaryCmdPool[workThread];

        private RenderPass renderPass;
        public RenderPass RenderPass => renderPass;

        public int workThread = 0;

        public uint currentImage;

        private NativeList<Semaphores> semaphores = new NativeList<Semaphores>(1, 1);
        private DepthStencil depthStencil;

        public NativeList<VkPipelineStageFlags> submitPipelineStages = new NativeList<VkPipelineStageFlags>() { VkPipelineStageFlags.ColorAttachmentOutput };

        private VkSubmitInfo submitInfo;

        GraphicsBuffer[] positionBuffer = new GraphicsBuffer[2];
        GraphicsBuffer[] instanceBuffer = new GraphicsBuffer[2];
        GraphicsBuffer[] transistBuffer = new GraphicsBuffer[2];

        public Graphics()
        {
            //Settings.Validation = false;

            enabledFeatures.samplerAnisotropy = True;
            VkInstance = Device.CreateInstance(Settings);
            device = Device.Init(enabledFeatures, EnabledExtensions);
           
            // Get a graphics queue from the Device
            queue = Device.GetDeviceQueue(Device.QFIndices.Graphics, 0);

            DepthFormat = Device.GetSupportedDepthFormat();            

            // Create synchronization objects
            Semaphores* pSem = (Semaphores*)semaphores.GetAddress(0);
            pSem->PresentComplete = Device.CreateSemaphore();
            pSem->RenderComplete = Device.CreateSemaphore();

            // Set up submit info structure
            // Semaphores will stay the same during application lifetime
            // Command buffer submission info is set by each example
            submitInfo = VkSubmitInfo.New();
            submitInfo.pWaitDstStageMask = (VkPipelineStageFlags*)submitPipelineStages.Data;
            submitInfo.waitSemaphoreCount = 1;
            submitInfo.pWaitSemaphores = &pSem->PresentComplete;
            submitInfo.signalSemaphoreCount = 1;
            submitInfo.pSignalSemaphores = &pSem->RenderComplete;
        }


        protected override void Destroy()
        {
            Device.Shutdown();

            base.Destroy();
        }

        public void Init(IntPtr wnd)
        {
            Swapchain.InitSurface(wnd);

            if (Device.EnableDebugMarkers)
            {
                // vks::debugmarker::setup(Device);
            }

            DescriptorPoolManager = new DescriptorPoolManager();

            CreateSwapChain();
            CreateDepthStencil();
            CreateRenderPass();
            CreateFrameBuffer();

            CreateCommandPool();
            CreateCommandBuffers();

        }

        public void Resize(int w, int h)
        {
            Width = w;
            Height = h;

            // Ensure all operations on the device have been finished before destroying resources
            WaitIdle();

            CreateSwapChain();
            // Recreate the frame buffers
            CreateDepthStencil();

            CreateFrameBuffer();

            // Command buffers need to be recreated as they may store
            // references to the recreated frame buffer
            CreateCommandBuffers();

        }

        protected void CreateFrameBuffer()
        {
            if(Framebuffers != null)
            {
                for (int i = 0; i < Framebuffers.Length; i++)
                {
                    Framebuffers[i].Dispose();
                }
            }

            framebuffers = CreateSwapChainFramebuffers(renderPass);
        }

        private void CreateSwapChain()
        {
            uint width, height;
            Swapchain.Create(&width, &height, Settings.VSync);

            Width = (int)width;
            Height = (int)height;
        }

        public void CreateRenderPass()
        {
            AttachmentDescription[] attachments =
            {
                // Color attachment
                new AttachmentDescription(Swapchain.ColorFormat, finalLayout : ImageLayout.PresentSrcKHR),
                // Depth attachment
                new AttachmentDescription(DepthFormat, finalLayout : ImageLayout.DepthStencilAttachmentOptimal)
            };

            SubpassDescription[] subpassDescription =
            {
                new SubpassDescription
                {
                    pipelineBindPoint = PipelineBindPoint.Graphics,

                    pColorAttachments = new []
                    {
                        new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                    },

                    pDepthStencilAttachment = new []
                    {
                        new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal)
                    },

                }
            };

            // Subpass dependencies for layout transitions
            SubpassDependency[] dependencies = 
            {
                new SubpassDependency
                {
                    srcSubpass = SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = PipelineStageFlags.BottomOfPipe,
                    dstStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    srcAccessMask = AccessFlags.MemoryRead,
                    dstAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dependencyFlags = DependencyFlags.ByRegion
                },

                new SubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = SubpassExternal,
                    srcStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = PipelineStageFlags.BottomOfPipe,
                    srcAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dstAccessMask = AccessFlags.MemoryRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },
            };

            RenderPassCreateInfo renderPassInfo = new RenderPassCreateInfo(attachments, subpassDescription, dependencies);
            
            renderPass = new RenderPass(ref renderPassInfo);           
        }

        public Framebuffer[] CreateSwapChainFramebuffers(RenderPass vkRenderPass)
        {
            VkImageView* attachments = stackalloc VkImageView[2];
            // Depth/Stencil attachment is the same for all frame buffers
            attachments[1] = depthStencil.view;

            var frameBufferCreateInfo = new FramebufferCreateInfo
            {
                renderPass = vkRenderPass,
                attachmentCount = 2,
                pAttachments = attachments,
                width = Width,
                height = Height,
                layers = 1
            };

            // Create frame buffers for every swap chain image
            var framebuffers = new Framebuffer[Swapchain.ImageCount];
            for (uint i = 0; i < framebuffers.Length; i++)
            {
                attachments[0] = Swapchain.Buffers[i].View;
                framebuffers[i] = CreateFramebuffer(ref frameBufferCreateInfo);
            }

            return framebuffers;
        }

        public Framebuffer CreateFramebuffer(ref FramebufferCreateInfo framebufferCreateInfo)
        {
            var fb = new Framebuffer(ref framebufferCreateInfo);
            return fb;
        }

        private void CreateCommandPool()
        {
            primaryCmdPool = new CommandBufferPool(Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer);
            secondaryCmdPool = new CommandBufferPool[2]
            {
                new CommandBufferPool(Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer),
                new CommandBufferPool(Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer)
            };
            
        }

        protected void CreateCommandBuffers()
        {
            primaryCmdPool.Allocate(CommandBufferLevel.Primary, (uint)Swapchain.ImageCount);

            foreach (var cmdPool in secondaryCmdPool)
            {
                cmdPool.Allocate(CommandBufferLevel.Secondary, 8);
            }
        }

        protected void CreateDepthStencil()
        {
            if (depthStencil != null)
            {
                depthStencil.Dispose();
                depthStencil = null;
            }

            depthStencil = new DepthStencil(Width, Height, DepthFormat);

        }

        public void WaitIdle()
        {
            vkDeviceWaitIdle(device);
        }

        public void BeginRender()
        {
            // Acquire the next image from the swap chaing
            Util.CheckResult(Swapchain.AcquireNextImage(semaphores[0].PresentComplete, ref currentImage));
        }

        public void EndRender()
        {
            // Command buffer to be sumitted to the queue
            submitInfo.commandBufferCount = 1;
            submitInfo.pCommandBuffers = (VkCommandBuffer*)primaryCmdPool.GetAddress(currentImage); //(VkCommandBuffer*)drawCmdBuffers.GetAddress(currentBuffer);

            // Submit to queue
            Util.CheckResult(vkQueueSubmit(queue, 1, ref submitInfo, VkFence.Null));

            Util.CheckResult(Swapchain.QueuePresent(queue, currentImage, semaphores[0].RenderComplete));

            Util.CheckResult(vkQueueWaitIdle(queue));
        }



    }
}
