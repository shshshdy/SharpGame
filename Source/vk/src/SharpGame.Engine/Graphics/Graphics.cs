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

    public struct DepthStencil
    {
        public VkImage Image;
        public VkDeviceMemory Mem;
        public VkImageView View;
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
        public VkPhysicalDevice physicalDevice { get; protected set; }
        public VkPhysicalDeviceFeatures enabledFeatures { get; protected set; }
        public NativeList<IntPtr> EnabledExtensions { get; } = new NativeList<IntPtr>();

        public static VkDevice device { get; protected set; }

        public static VkQueue queue { get; protected set; }
        public static VkFormat DepthFormat { get; protected set; }
        public VulkanSwapchain Swapchain { get; } = new VulkanSwapchain();

        public static uint Width { get; private set; }
        public static uint Height { get; private set; }

        public static NativeList<VkFramebuffer> frameBuffers { get; protected set; } = new NativeList<VkFramebuffer>();

        internal static DescriptorPoolManager DescriptorPoolManager { get; private set; }

        public VkCommandPool cmdPool => _cmdPool;

        public CommandBufferPool commandBufferPool;

        public CommandBuffer RenderCmdBuffer => commandBufferPool.CommandBuffers[currentBuffer];

        public static NativeList<VkCommandBuffer> drawCmdBuffers { get; protected set; } = new NativeList<VkCommandBuffer>();
        public static VkRenderPass renderPass => _renderPass;

        public NativeList<Semaphores> semaphores = new NativeList<Semaphores>(1, 1);
        public Semaphores* GetSemaphoresPtr() => (Semaphores*)semaphores.GetAddress(0);
        public DepthStencil DepthStencil;
        public VkSubmitInfo submitInfo;
        public NativeList<VkPipelineStageFlags> submitPipelineStages = CreateSubmitPipelineStages();
        private static NativeList<VkPipelineStageFlags> CreateSubmitPipelineStages()
            => new NativeList<VkPipelineStageFlags>() { VkPipelineStageFlags.ColorAttachmentOutput };
        protected static VkRenderPass _renderPass;
        private VkCommandPool _cmdPool;

        public uint currentBuffer;

        DebugReportCallbackExt debugReportCallbackExt;
        public Graphics()
        {
            VkResult err;
            err = CreateInstance(false);
            if (err != VkResult.Success)
            {
                throw new InvalidOperationException("Could not create Vulkan instance. Error: " + err);
            }

            if (Settings.Validation)
            {
                debugReportCallbackExt = CreateDebugReportCallback();

            }

            // Physical Device
            uint gpuCount = 0;
            Util.CheckResult(vkEnumeratePhysicalDevices(VkInstance, &gpuCount, null));
            Debug.Assert(gpuCount > 0);
            // Enumerate devices
            IntPtr* physicalDevices = stackalloc IntPtr[(int)gpuCount];
            err = vkEnumeratePhysicalDevices(VkInstance, &gpuCount, (VkPhysicalDevice*)physicalDevices);
            if (err != VkResult.Success)
            {
                throw new InvalidOperationException("Could not enumerate physical devices.");
            }

            // GPU selection

            // Select physical Device to be used for the Vulkan example
            // Defaults to the first Device unless specified by command line

            uint selectedDevice = 0;
            // TODO: Implement arg parsing, etc.

            physicalDevice = ((VkPhysicalDevice*)physicalDevices)[selectedDevice];

            // Vulkan Device creation
            // This is handled by a separate class that gets a logical Device representation
            // and encapsulates functions related to a Device
            Device.Init(physicalDevice);
            VkResult res = Device.CreateLogicalDevice(enabledFeatures, EnabledExtensions);
            if (res != VkResult.Success)
            {
                throw new InvalidOperationException("Could not create Vulkan Device.");
            }

            device = Device.LogicalDevice;

            // Get a graphics queue from the Device
            VkQueue queue;
            vkGetDeviceQueue(device, Device.QFIndices.Graphics, 0, &queue);
            Graphics.queue = queue;

            // Find a suitable depth format
            VkFormat depthFormat;
            uint validDepthFormat = Tools.getSupportedDepthFormat(physicalDevice, &depthFormat);
            Debug.Assert(validDepthFormat == True);
            DepthFormat = depthFormat;

            Swapchain.Connect(VkInstance, physicalDevice, device);

            // Create synchronization objects
            VkSemaphoreCreateInfo semaphoreCreateInfo = Builder.SemaphoreCreateInfo();
            // Create a semaphore used to synchronize image presentation
            // Ensures that the image is displayed before we start submitting new commands to the queu
            Util.CheckResult(vkCreateSemaphore(device, &semaphoreCreateInfo, null, &GetSemaphoresPtr()->PresentComplete));
            // Create a semaphore used to synchronize command submission
            // Ensures that the image is not presented until all commands have been sumbitted and executed
            Util.CheckResult(vkCreateSemaphore(device, &semaphoreCreateInfo, null, &GetSemaphoresPtr()->RenderComplete));
            // Create a semaphore used to synchronize command submission
            // Ensures that the image is not presented until all commands for the text overlay have been sumbitted and executed
            // Will be inserted after the render complete semaphore if the text overlay is enabled
            Util.CheckResult(vkCreateSemaphore(device, &semaphoreCreateInfo, null, &GetSemaphoresPtr()->TextOverlayComplete));

            // Set up submit info structure
            // Semaphores will stay the same during application lifetime
            // Command buffer submission info is set by each example
            submitInfo = Builder.SubmitInfo();
            submitInfo.pWaitDstStageMask = (VkPipelineStageFlags*)submitPipelineStages.Data;
            submitInfo.waitSemaphoreCount = 1;
            submitInfo.pWaitSemaphores = &GetSemaphoresPtr()->PresentComplete;
            submitInfo.signalSemaphoreCount = 1;
            submitInfo.pSignalSemaphores = &GetSemaphoresPtr()->RenderComplete;
        }

        private DebugReportCallbackExt CreateDebugReportCallback()
        {
            // Attach debug callback.
            var debugReportCreateInfo = new DebugReportCallbackCreateInfoExt(
                VkDebugReportFlagsEXT.InformationEXT|
                VkDebugReportFlagsEXT.WarningEXT |
                VkDebugReportFlagsEXT.PerformanceWarningEXT |
                VkDebugReportFlagsEXT.ErrorEXT |
                VkDebugReportFlagsEXT.DebugEXT,
                args =>
                {
                    Debug.WriteLine($"[{args.Flags}][{args.LayerPrefix}] {args.Message}");
                    return args.Flags.HasFlag(DebugReportFlagsExt.Error);
                }
            );
            return new DebugReportCallbackExt(VkInstance, ref debugReportCreateInfo);
        }

        protected override void Destroy()
        {
            debugReportCallbackExt?.Dispose();

            base.Destroy();

        }
        
        private VkResult CreateInstance(bool enableValidation)
        {
            Settings.Validation = enableValidation;

            VkApplicationInfo appInfo = new VkApplicationInfo()
            {
                sType = VkStructureType.ApplicationInfo,
                apiVersion = new Version(1, 0, 0),
                //pApplicationName = Name,
                //pEngineName = Name,
            };

            NativeList<IntPtr> instanceExtensions = new NativeList<IntPtr>(2);
            instanceExtensions.Add(Strings.VK_KHR_SURFACE_EXTENSION_NAME);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                instanceExtensions.Add(Strings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                instanceExtensions.Add(Strings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            VkInstanceCreateInfo instanceCreateInfo = VkInstanceCreateInfo.New();
            instanceCreateInfo.pApplicationInfo = &appInfo;

            if (instanceExtensions.Count > 0)
            {
                if (enableValidation)
                {
                    instanceExtensions.Add(Strings.VK_EXT_DEBUG_REPORT_EXTENSION_NAME);
                }
                instanceCreateInfo.enabledExtensionCount = instanceExtensions.Count;
                instanceCreateInfo.ppEnabledExtensionNames = (byte**)instanceExtensions.Data;
            }
            
            if (enableValidation)
            {
                NativeList<IntPtr> enabledLayerNames = new NativeList<IntPtr>(1);
                enabledLayerNames.Add(Strings.StandardValidationLayeName);
                instanceCreateInfo.enabledLayerCount = enabledLayerNames.Count;
                instanceCreateInfo.ppEnabledLayerNames = (byte**)enabledLayerNames.Data;
            }

            VkInstance instance;
            VkResult result = vkCreateInstance(&instanceCreateInfo, null, &instance);
            VkInstance = instance;
            return result;
        }
        
        protected virtual void SetupFrameBuffer()
        {
            using (NativeList<VkImageView> attachments = new NativeList<VkImageView>(2))
            {
                attachments.Count = 2;
                // Depth/Stencil attachment is the same for all frame buffers
                attachments[1] = DepthStencil.View;

                VkFramebufferCreateInfo frameBufferCreateInfo = VkFramebufferCreateInfo.New();
                frameBufferCreateInfo.renderPass = renderPass;
                frameBufferCreateInfo.attachmentCount = 2;
                frameBufferCreateInfo.pAttachments = (VkImageView*)attachments.Data;
                frameBufferCreateInfo.width = Width;
                frameBufferCreateInfo.height = Height;
                frameBufferCreateInfo.layers = 1;

                // Create frame buffers for every swap chain image
                frameBuffers.Count = (Swapchain.ImageCount);
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

            Width = width;
            Height = height;
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
                attachments[1].format = DepthFormat;
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
            createCommandBuffers();
            SetupDepthStencil();
            SetupRenderPass();
            SetupFrameBuffer();
        }

        public void Resize(uint w, uint h)
        {
            Width = w;
            Height = h;


            // Ensure all operations on the device have been finished before destroying resources
            vkDeviceWaitIdle(device);

            SetupSwapChain();

            // Recreate the frame buffers

            vkDestroyImageView(device, DepthStencil.View, null);
            vkDestroyImage(device, DepthStencil.Image, null);
            vkFreeMemory(device, DepthStencil.Mem, null);
            SetupDepthStencil();

            for (uint i = 0; i < frameBuffers.Count; i++)
            {
                vkDestroyFramebuffer(device, frameBuffers[i], null);
            }
            SetupFrameBuffer();

            // Command buffers need to be recreated as they may store
            // references to the recreated frame buffer
            destroyCommandBuffers();
            createCommandBuffers();

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

            //VkCommandPoolCreateInfo cmdPoolInfo = VkCommandPoolCreateInfo.New();
            //cmdPoolInfo.queueFamilyIndex = Swapchain.QueueNodeIndex;
            //cmdPoolInfo.flags = VkCommandPoolCreateFlags.ResetCommandBuffer;
            //Util.CheckResult(vkCreateCommandPool(device, &cmdPoolInfo, null, out _cmdPool));
        }

        protected void createCommandBuffers()
        {
            commandBufferPool.Allocate(Swapchain.ImageCount);
            /*
            // Create one command buffer for each swap chain image and reuse for rendering
            drawCmdBuffers.Resize(Swapchain.ImageCount);
            drawCmdBuffers.Count = Swapchain.ImageCount;

            VkCommandBufferAllocateInfo cmdBufAllocateInfo =
                Builder.CommandBufferAllocateInfo(cmdPool, VkCommandBufferLevel.Primary, drawCmdBuffers.Count);

            Util.CheckResult(vkAllocateCommandBuffers(device, ref cmdBufAllocateInfo, (VkCommandBuffer*)drawCmdBuffers.Data));
            */
        }

        protected void destroyCommandBuffers()
        {
            commandBufferPool.Free();
            //vkFreeCommandBuffers(device, cmdPool, drawCmdBuffers.Count, drawCmdBuffers.Data);
        }

        protected void SetupDepthStencil()
        {
            VkImageCreateInfo image = VkImageCreateInfo.New();
            image.imageType = VkImageType.Image2D;
            image.format = DepthFormat;
            image.extent = new VkExtent3D() { width = Width, height = Height, depth = 1 };
            image.mipLevels = 1;
            image.arrayLayers = 1;
            image.samples = VkSampleCountFlags.Count1;
            image.tiling = VkImageTiling.Optimal;
            image.usage = (VkImageUsageFlags.DepthStencilAttachment | VkImageUsageFlags.TransferSrc);
            image.flags = 0;

            VkMemoryAllocateInfo mem_alloc = VkMemoryAllocateInfo.New();
            mem_alloc.allocationSize = 0;
            mem_alloc.memoryTypeIndex = 0;

            VkImageViewCreateInfo depthStencilView = VkImageViewCreateInfo.New();
            depthStencilView.viewType = VkImageViewType.Image2D;
            depthStencilView.format = DepthFormat;
            depthStencilView.flags = 0;
            depthStencilView.subresourceRange = new VkImageSubresourceRange();
            depthStencilView.subresourceRange.aspectMask = (VkImageAspectFlags.Depth | VkImageAspectFlags.Stencil);
            depthStencilView.subresourceRange.baseMipLevel = 0;
            depthStencilView.subresourceRange.levelCount = 1;
            depthStencilView.subresourceRange.baseArrayLayer = 0;
            depthStencilView.subresourceRange.layerCount = 1;

            Util.CheckResult(vkCreateImage(device, &image, null, out DepthStencil.Image));
            vkGetImageMemoryRequirements(device, DepthStencil.Image, out VkMemoryRequirements memReqs);
            mem_alloc.allocationSize = memReqs.size;
            mem_alloc.memoryTypeIndex = Device.getMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);
            Util.CheckResult(vkAllocateMemory(device, &mem_alloc, null, out DepthStencil.Mem));
            Util.CheckResult(vkBindImageMemory(device, DepthStencil.Image, DepthStencil.Mem, 0));

            depthStencilView.image = DepthStencil.Image;
            Util.CheckResult(vkCreateImageView(device, ref depthStencilView, null, out DepthStencil.View));
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
