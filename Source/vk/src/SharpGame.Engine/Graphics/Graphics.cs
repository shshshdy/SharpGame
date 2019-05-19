using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid.Sdl2;
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
        public static Format DepthFormat { get; protected set; }
        public Swapchain Swapchain { get; } = new Swapchain();

        public static int Width { get; private set; }
        public static int Height { get; private set; }

        public static NativeList<VkFramebuffer> frameBuffers { get; protected set; } = new NativeList<VkFramebuffer>();

        internal static DescriptorPoolManager DescriptorPoolManager { get; private set; }

        public CommandBufferPool commandBufferPool;

        public CommandBuffer RenderCmdBuffer => commandBufferPool.CommandBuffers[currentBuffer];

        public static NativeList<VkCommandBuffer> drawCmdBuffers { get; protected set; } = new NativeList<VkCommandBuffer>();
        public static VkRenderPass renderPass => _renderPass;

        public uint currentBuffer;

        private NativeList<Semaphores> semaphores = new NativeList<Semaphores>(1, 1);
        private DepthStencil depthStencil;

        public NativeList<VkPipelineStageFlags> submitPipelineStages = CreateSubmitPipelineStages();
        private static NativeList<VkPipelineStageFlags> CreateSubmitPipelineStages()
            => new NativeList<VkPipelineStageFlags>() { VkPipelineStageFlags.ColorAttachmentOutput };
        protected static VkRenderPass _renderPass;

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

        protected virtual void SetupFrameBuffer()
        {
            using (NativeList<VkImageView> attachments = new NativeList<VkImageView>(2))
            {
                attachments.Count = 2;
                // Depth/Stencil attachment is the same for all frame buffers
                attachments[1] = depthStencil.view;

                VkFramebufferCreateInfo frameBufferCreateInfo = VkFramebufferCreateInfo.New();
                frameBufferCreateInfo.renderPass = renderPass;
                frameBufferCreateInfo.attachmentCount = 2;
                frameBufferCreateInfo.pAttachments = (VkImageView*)attachments.Data;
                frameBufferCreateInfo.width = (uint)Width;
                frameBufferCreateInfo.height = (uint)Height;
                frameBufferCreateInfo.layers = 1;

                // Create frame buffers for every swap chain image
                frameBuffers.Count = Swapchain.ImageCount;

                for (uint i = 0; i < frameBuffers.Count; i++)
                {
                    attachments[0] = Swapchain.Buffers[i].View;
                    Util.CheckResult(vkCreateFramebuffer(device, ref frameBufferCreateInfo, null, (VkFramebuffer*)Unsafe.AsPointer(ref frameBuffers[i])));
                }
            }
        }

        private void SetupSwapChain()
        {
            uint width, height;
            Swapchain.Create(&width, &height, Settings.VSync);

            Width = (int)width;
            Height = (int)height;
        }

        protected virtual void SetupRenderPass()
        {
            using (NativeList<VkAttachmentDescription> attachments = new NativeList<VkAttachmentDescription>())
            {
                attachments.Count = 2;
                // Color attachment
                attachments[0] = new VkAttachmentDescription();
                attachments[0].format = Swapchain.ColorFormat;
                attachments[0].samples = VkSampleCountFlags.Count1;
                attachments[0].loadOp = VkAttachmentLoadOp.Clear;
                attachments[0].storeOp = VkAttachmentStoreOp.Store;
                attachments[0].stencilLoadOp = VkAttachmentLoadOp.DontCare;
                attachments[0].stencilStoreOp = VkAttachmentStoreOp.DontCare;
                attachments[0].initialLayout = VkImageLayout.Undefined;
                attachments[0].finalLayout = VkImageLayout.PresentSrcKHR;
                // Depth attachment
                attachments[1] = new VkAttachmentDescription();
                attachments[1].format = (VkFormat)DepthFormat;
                attachments[1].samples = VkSampleCountFlags.Count1;
                attachments[1].loadOp = VkAttachmentLoadOp.Clear;
                attachments[1].storeOp = VkAttachmentStoreOp.Store;
                attachments[1].stencilLoadOp = VkAttachmentLoadOp.DontCare;
                attachments[1].stencilStoreOp = VkAttachmentStoreOp.DontCare;
                attachments[1].initialLayout = VkImageLayout.Undefined;
                attachments[1].finalLayout = VkImageLayout.DepthStencilAttachmentOptimal;

                VkAttachmentReference colorReference = new VkAttachmentReference();
                colorReference.attachment = 0;
                colorReference.layout = VkImageLayout.ColorAttachmentOptimal;

                VkAttachmentReference depthReference = new VkAttachmentReference();
                depthReference.attachment = 1;
                depthReference.layout = VkImageLayout.DepthStencilAttachmentOptimal;

                VkSubpassDescription subpassDescription = new VkSubpassDescription();
                subpassDescription.pipelineBindPoint = VkPipelineBindPoint.Graphics;
                subpassDescription.colorAttachmentCount = 1;
                subpassDescription.pColorAttachments = &colorReference;
                subpassDescription.pDepthStencilAttachment = &depthReference;
                subpassDescription.inputAttachmentCount = 0;
                subpassDescription.pInputAttachments = null;
                subpassDescription.preserveAttachmentCount = 0;
                subpassDescription.pPreserveAttachments = null;
                subpassDescription.pResolveAttachments = null;

                // Subpass dependencies for layout transitions
                using (NativeList<VkSubpassDependency> dependencies = new NativeList<VkSubpassDependency>(2))
                {
                    dependencies.Count = 2;

                    dependencies[0].srcSubpass = SubpassExternal;
                    dependencies[0].dstSubpass = 0;
                    dependencies[0].srcStageMask = VkPipelineStageFlags.BottomOfPipe;
                    dependencies[0].dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
                    dependencies[0].srcAccessMask = VkAccessFlags.MemoryRead;
                    dependencies[0].dstAccessMask = (VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite);
                    dependencies[0].dependencyFlags = VkDependencyFlags.ByRegion;

                    dependencies[1].srcSubpass = 0;
                    dependencies[1].dstSubpass = SubpassExternal;
                    dependencies[1].srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
                    dependencies[1].dstStageMask = VkPipelineStageFlags.BottomOfPipe;
                    dependencies[1].srcAccessMask = (VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite);
                    dependencies[1].dstAccessMask = VkAccessFlags.MemoryRead;
                    dependencies[1].dependencyFlags = VkDependencyFlags.ByRegion;

                    VkRenderPassCreateInfo renderPassInfo = new VkRenderPassCreateInfo();
                    renderPassInfo.sType = VkStructureType.RenderPassCreateInfo;
                    renderPassInfo.attachmentCount = attachments.Count;
                    renderPassInfo.pAttachments = (VkAttachmentDescription*)attachments.Data.ToPointer();
                    renderPassInfo.subpassCount = 1;
                    renderPassInfo.pSubpasses = &subpassDescription;
                    renderPassInfo.dependencyCount = dependencies.Count;
                    renderPassInfo.pDependencies = (VkSubpassDependency*)dependencies.Data;

                    Util.CheckResult(vkCreateRenderPass(device, &renderPassInfo, null, out _renderPass));
                }
            }
        }

        public void CreateSwapchain(IntPtr wnd)
        {
            Swapchain.InitSurface(wnd);

            if (Device.EnableDebugMarkers)
            {
                // vks::debugmarker::setup(Device);
            }

            DescriptorPoolManager = new DescriptorPoolManager();

            CreateCommandPool();
            SetupSwapChain();
            CreateCommandBuffers();
            SetupDepthStencil();
            SetupRenderPass();
            SetupFrameBuffer();
        }

        public void Resize(int w, int h)
        {
            Width = w;
            Height = h;

            // Ensure all operations on the device have been finished before destroying resources
            WaitIdle();

            SetupSwapChain();
            // Recreate the frame buffers
            SetupDepthStencil();

            for (uint i = 0; i < frameBuffers.Count; i++)
            {
                vkDestroyFramebuffer(device, frameBuffers[i], null);
            }

            SetupFrameBuffer();

            // Command buffers need to be recreated as they may store
            // references to the recreated frame buffer
            DestroyCommandBuffers();
            CreateCommandBuffers();

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
            submitInfo.pCommandBuffers = (VkCommandBuffer*)commandBufferPool.GetAddress(currentBuffer); //(VkCommandBuffer*)drawCmdBuffers.GetAddress(currentBuffer);

            // Submit to queue
            Util.CheckResult(vkQueueSubmit(queue, 1, ref submitInfo, VkFence.Null));

            Util.CheckResult(Swapchain.QueuePresent(queue, currentBuffer, semaphores[0].RenderComplete));

            Util.CheckResult(vkQueueWaitIdle(queue));
        }

        private void CreateCommandPool()
        {
            commandBufferPool = new CommandBufferPool(Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer);
        }

        protected void CreateCommandBuffers()
        {
            commandBufferPool.Allocate(Swapchain.ImageCount);
        }

        protected void DestroyCommandBuffers()
        {
            commandBufferPool.Free();
        }

        protected void SetupDepthStencil()
        {
            if(depthStencil != null)
            {
                depthStencil.Dispose();
                depthStencil = null;
            }

            depthStencil = new DepthStencil(Width, Height, DepthFormat);

        }

        public static VkPipelineShaderStageCreateInfo loadShader(string fileName, VkShaderStageFlags stage)
        {
            VkPipelineShaderStageCreateInfo shaderStage = VkPipelineShaderStageCreateInfo.New();
            shaderStage.stage = stage;
            shaderStage.module = Tools.loadShader(fileName, device, stage);
            shaderStage.pName = Strings.main; // todo : make param
            Debug.Assert(shaderStage.module.Handle != 0);
            return shaderStage;
        }


    }
}
