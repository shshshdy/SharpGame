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
        public VkSemaphore TextOverlayComplete;
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
        public VkPhysicalDeviceFeatures enabledFeatures { get; protected set; }
        public NativeList<IntPtr> EnabledExtensions { get; } = new NativeList<IntPtr>();

        public static VkDevice device { get; protected set; }
        public static VkQueue queue { get; protected set; }
        public Format DepthFormat { get; protected set; }
        public Swapchain Swapchain { get; } = new Swapchain();

        public static int Width { get; private set; }
        public static int Height { get; private set; }

        public static NativeList<VkFramebuffer> FrameBuffers { get; protected set; } = new NativeList<VkFramebuffer>();

        internal static DescriptorPoolManager DescriptorPoolManager { get; private set; }

        private CommandBufferPool primaryCommandPool;
        private CommandBufferPool[] secondaryCommandPool;

        public CommandBuffer RenderCmdBuffer => primaryCommandPool.CommandBuffers[currentBuffer];
        public CommandBuffer WorkCmdBuffer => secondaryCommandPool[0].CommandBuffers[currentBuffer];

        private static VkRenderPass renderPass;
        public static VkRenderPass RenderPass => renderPass;

        public uint currentBuffer;

        private NativeList<Semaphores> semaphores = new NativeList<Semaphores>(1, 1);
        private DepthStencil depthStencil;

        public NativeList<VkPipelineStageFlags> submitPipelineStages = new NativeList<VkPipelineStageFlags>() { VkPipelineStageFlags.ColorAttachmentOutput };

        private VkSubmitInfo submitInfo;

        public Graphics()
        {
            Settings.Validation = false;

            VkInstance = Device.CreateInstance(Settings);
            device = Device.Init(enabledFeatures, EnabledExtensions);
           
            // Get a graphics queue from the Device
            queue = Device.GetDeviceQueue(Device.QFIndices.Graphics, 0);

            DepthFormat = Device.GetSupportedDepthFormat();            

            // Create synchronization objects
            Semaphores* pSem = (Semaphores*)semaphores.GetAddress(0);
            pSem->PresentComplete = Device.CreateSemaphore();
            pSem->RenderComplete = Device.CreateSemaphore();
            pSem->TextOverlayComplete = Device.CreateSemaphore();

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
            CreateFrameBuffer();

            CreateCommandPool();
            CreateCommandBuffers();

            CreateRenderPass();
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
            for (uint i = 0; i < FrameBuffers.Count; i++)
            {
                Device.Destroy(FrameBuffers[i]);
            }

            using (NativeList<VkImageView> attachments = new NativeList<VkImageView>(2))
            {
                attachments.Count = 2;
                // Depth/Stencil attachment is the same for all frame buffers
                attachments[1] = depthStencil.view;

                VkFramebufferCreateInfo frameBufferCreateInfo = VkFramebufferCreateInfo.New();
                frameBufferCreateInfo.renderPass = RenderPass;
                frameBufferCreateInfo.attachmentCount = 2;
                frameBufferCreateInfo.pAttachments = (VkImageView*)attachments.Data;
                frameBufferCreateInfo.width = (uint)Width;
                frameBufferCreateInfo.height = (uint)Height;
                frameBufferCreateInfo.layers = 1;

                // Create frame buffers for every swap chain image
                FrameBuffers.Count = Swapchain.ImageCount;

                for (uint i = 0; i < FrameBuffers.Count; i++)
                {
                    attachments[0] = Swapchain.Buffers[i].View;
                    FrameBuffers[i] = Device.CreateFramebuffer(ref frameBufferCreateInfo);
                }
            }
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
            VkAttachmentDescription[] attachments =
            {
                // Color attachment
                new VkAttachmentDescription
                {
                    format = Swapchain.ColorFormat,
                    samples = VkSampleCountFlags.Count1,
                    loadOp = VkAttachmentLoadOp.Clear,
                    storeOp = VkAttachmentStoreOp.Store,
                    stencilLoadOp = VkAttachmentLoadOp.DontCare,
                    stencilStoreOp = VkAttachmentStoreOp.DontCare,
                    initialLayout = VkImageLayout.Undefined,
                    finalLayout = VkImageLayout.PresentSrcKHR
                },

                // Depth attachment
                new VkAttachmentDescription
                {
                    format = (VkFormat)DepthFormat,
                    samples = VkSampleCountFlags.Count1,
                    loadOp = VkAttachmentLoadOp.Clear,
                    storeOp = VkAttachmentStoreOp.Store,
                    stencilLoadOp = VkAttachmentLoadOp.DontCare,
                    stencilStoreOp = VkAttachmentStoreOp.DontCare,
                    initialLayout = VkImageLayout.Undefined,
                    finalLayout = VkImageLayout.DepthStencilAttachmentOptimal
                }
            };

            VkAttachmentReference colorReference = new VkAttachmentReference
            {
                attachment = 0,
                layout = VkImageLayout.ColorAttachmentOptimal
            };

            VkAttachmentReference depthReference = new VkAttachmentReference
            {
                attachment = 1,
                layout = VkImageLayout.DepthStencilAttachmentOptimal
            };

            VkSubpassDescription subpassDescription = new VkSubpassDescription
            {
                pipelineBindPoint = VkPipelineBindPoint.Graphics,
                colorAttachmentCount = 1,
                pColorAttachments = &colorReference,
                pDepthStencilAttachment = &depthReference,
                inputAttachmentCount = 0,
                pInputAttachments = null,
                preserveAttachmentCount = 0,
                pPreserveAttachments = null,
                pResolveAttachments = null
            };

            // Subpass dependencies for layout transitions
            VkSubpassDependency[] dependencies = 
            {
                new VkSubpassDependency
                {
                    srcSubpass = SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = VkPipelineStageFlags.BottomOfPipe,
                    dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                    srcAccessMask = VkAccessFlags.MemoryRead,
                    dstAccessMask = (VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite),
                    dependencyFlags = VkDependencyFlags.ByRegion
                },

                new VkSubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = SubpassExternal,
                    srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = VkPipelineStageFlags.BottomOfPipe,
                    srcAccessMask = (VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite),
                    dstAccessMask = VkAccessFlags.MemoryRead,
                    dependencyFlags = VkDependencyFlags.ByRegion
                },
            };

            VkRenderPassCreateInfo renderPassInfo = new VkRenderPassCreateInfo
            {
                sType = VkStructureType.RenderPassCreateInfo,
                attachmentCount = (uint)attachments.Length,
                pAttachments = (VkAttachmentDescription*)Utilities.AsPointer(ref attachments[0]),
                subpassCount = 1,
                pSubpasses = &subpassDescription,
                dependencyCount = (uint)dependencies.Length,
                pDependencies = (VkSubpassDependency*)Utilities.AsPointer(ref dependencies[0])
            };

            renderPass = Device.CreateRenderPass(ref renderPassInfo);           
        }

        private void CreateCommandPool()
        {
            primaryCommandPool = new CommandBufferPool(Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer);
            secondaryCommandPool = new CommandBufferPool[2]
            {
                new CommandBufferPool(Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer),
                new CommandBufferPool(Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer)
            };
            
        }

        protected void CreateCommandBuffers()
        {
            primaryCommandPool.Allocate(CommandBufferLevel.Primary, Swapchain.ImageCount);

            foreach (var cmdPool in secondaryCommandPool)
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
            Util.CheckResult(Swapchain.AcquireNextImage(semaphores[0].PresentComplete, ref currentBuffer));
        }

        public void EndRender()
        {
            // Command buffer to be sumitted to the queue
            submitInfo.commandBufferCount = 1;
            submitInfo.pCommandBuffers = (VkCommandBuffer*)primaryCommandPool.GetAddress(currentBuffer); //(VkCommandBuffer*)drawCmdBuffers.GetAddress(currentBuffer);

            // Submit to queue
            Util.CheckResult(vkQueueSubmit(queue, 1, ref submitInfo, VkFence.Null));

            Util.CheckResult(Swapchain.QueuePresent(queue, currentBuffer, semaphores[0].RenderComplete));

            Util.CheckResult(vkQueueWaitIdle(queue));
        }



    }
}
