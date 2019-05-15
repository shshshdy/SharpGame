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
        public bool Validation { get; set; } = false;
        public bool Fullscreen { get; set; } = false;
        public bool VSync { get; set; } = false;
    }

    public unsafe partial class Graphics
    {
        public Settings Settings { get; } = new Settings();
        public VkInstance Instance { get; protected set; }
        public VkPhysicalDevice physicalDevice { get; protected set; }
        public static vksVulkanDevice Device { get; protected set; }
        public VkPhysicalDeviceFeatures enabledFeatures { get; protected set; }
        public NativeList<IntPtr> EnabledExtensions { get; } = new NativeList<IntPtr>();

        public static VkDevice device { get; protected set; }

        public VkQueue queue { get; protected set; }
        public VkFormat DepthFormat { get; protected set; }
        public VulkanSwapchain Swapchain { get; } = new VulkanSwapchain();

        public uint width;
        public uint height;

        public VkPipelineCache pipelineCache => _pipelineCache;
        public static NativeList<VkFramebuffer> frameBuffers { get; protected set; } = new NativeList<VkFramebuffer>();
        public VkPhysicalDeviceMemoryProperties DeviceMemoryProperties { get; set; }
        public VkPhysicalDeviceProperties DeviceProperties { get; protected set; }
        public VkPhysicalDeviceFeatures DeviceFeatures { get; protected set; }

        public VkCommandPool cmdPool => _cmdPool;

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
        private VkPipelineCache _pipelineCache;
        private VkCommandPool _cmdPool;

        public uint currentBuffer;

        public void InitVulkan()
        {
            VkResult err;
            err = CreateInstance(false);
            if (err != VkResult.Success)
            {
                throw new InvalidOperationException("Could not create Vulkan instance. Error: " + err);
            }

            if (Settings.Validation)
            {

            }

            // Physical Device
            uint gpuCount = 0;
            Util.CheckResult(vkEnumeratePhysicalDevices(Instance, &gpuCount, null));
            Debug.Assert(gpuCount > 0);
            // Enumerate devices
            IntPtr* physicalDevices = stackalloc IntPtr[(int)gpuCount];
            err = vkEnumeratePhysicalDevices(Instance, &gpuCount, (VkPhysicalDevice*)physicalDevices);
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

            // Store properties (including limits) and features of the phyiscal Device
            // So examples can check against them and see if a feature is actually supported
            VkPhysicalDeviceProperties deviceProperties;
            vkGetPhysicalDeviceProperties(physicalDevice, &deviceProperties);
            DeviceProperties = deviceProperties;

            VkPhysicalDeviceFeatures deviceFeatures;
            vkGetPhysicalDeviceFeatures(physicalDevice, &deviceFeatures);
            DeviceFeatures = deviceFeatures;

            // Gather physical Device memory properties
            VkPhysicalDeviceMemoryProperties deviceMemoryProperties;
            vkGetPhysicalDeviceMemoryProperties(physicalDevice, &deviceMemoryProperties);
            DeviceMemoryProperties = deviceMemoryProperties;

            // Derived examples can override this to set actual features (based on above readings) to enable for logical device creation
            getEnabledFeatures();

            // Vulkan Device creation
            // This is handled by a separate class that gets a logical Device representation
            // and encapsulates functions related to a Device
            Device = new vksVulkanDevice(physicalDevice);
            VkResult res = Device.CreateLogicalDevice(enabledFeatures, EnabledExtensions);
            if (res != VkResult.Success)
            {
                throw new InvalidOperationException("Could not create Vulkan Device.");
            }
            device = Device.LogicalDevice;

            // Get a graphics queue from the Device
            VkQueue queue;
            vkGetDeviceQueue(device, Device.QFIndices.Graphics, 0, &queue);
            this.queue = queue;

            // Find a suitable depth format
            VkFormat depthFormat;
            uint validDepthFormat = Tools.getSupportedDepthFormat(physicalDevice, &depthFormat);
            Debug.Assert(validDepthFormat == True);
            DepthFormat = depthFormat;

            Swapchain.Connect(Instance, physicalDevice, device);

            // Create synchronization objects
            VkSemaphoreCreateInfo semaphoreCreateInfo = Initializers.semaphoreCreateInfo();
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
            submitInfo = Initializers.SubmitInfo();
            submitInfo.pWaitDstStageMask = (VkPipelineStageFlags*)submitPipelineStages.Data;
            submitInfo.waitSemaphoreCount = 1;
            submitInfo.pWaitSemaphores = &GetSemaphoresPtr()->PresentComplete;
            submitInfo.signalSemaphoreCount = 1;
            submitInfo.pSignalSemaphores = &GetSemaphoresPtr()->RenderComplete;
        }

        protected virtual void getEnabledFeatures()
        {
            // Used in some derived classes.
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
            Instance = instance;
            return result;
        }

        public void Initialize()
        {

            if (Device.EnableDebugMarkers)
            {
                // vks::debugmarker::setup(Device);
            }

            CreateCommandPool();
            SetupSwapChain();
            createCommandBuffers();
            SetupDepthStencil();
            SetupRenderPass();
            CreatePipelineCache();
            SetupFrameBuffer();
        }

        private void CreatePipelineCache()
        {
            VkPipelineCacheCreateInfo pipelineCacheCreateInfo = VkPipelineCacheCreateInfo.New();
            Util.CheckResult(vkCreatePipelineCache(device, ref pipelineCacheCreateInfo, null, out _pipelineCache));
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
                frameBufferCreateInfo.width = width;
                frameBufferCreateInfo.height = height;
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

            this.width = width;
            this.height = height;
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

        public void InitSwapchain(IntPtr wnd)
        {
            Swapchain.InitSurface(wnd);
        }

        public void Resize(uint w, uint h)
        {
            width = w;
            height = h;


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

        public void prepareFrame()
        {
            // Acquire the next image from the swap chaing
            Util.CheckResult(Swapchain.AcquireNextImage(semaphores[0].PresentComplete, ref currentBuffer));
        }

        public void submitFrame()
        {
            // Command buffer to be sumitted to the queue
            submitInfo.commandBufferCount = 1;
            submitInfo.pCommandBuffers = (VkCommandBuffer*)drawCmdBuffers.GetAddress(currentBuffer);

            // Submit to queue
            Util.CheckResult(vkQueueSubmit(queue, 1, ref submitInfo, VkFence.Null));


            Util.CheckResult(Swapchain.QueuePresent(queue, currentBuffer, semaphores[0].RenderComplete));

            Util.CheckResult(vkQueueWaitIdle(queue));
        }

        private void CreateCommandPool()
        {
            VkCommandPoolCreateInfo cmdPoolInfo = VkCommandPoolCreateInfo.New();
            cmdPoolInfo.queueFamilyIndex = Swapchain.QueueNodeIndex;
            cmdPoolInfo.flags = VkCommandPoolCreateFlags.ResetCommandBuffer;
            Util.CheckResult(vkCreateCommandPool(device, &cmdPoolInfo, null, out _cmdPool));
        }

        protected void createCommandBuffers()
        {
            // Create one command buffer for each swap chain image and reuse for rendering
            drawCmdBuffers.Resize(Swapchain.ImageCount);
            drawCmdBuffers.Count = Swapchain.ImageCount;

            VkCommandBufferAllocateInfo cmdBufAllocateInfo =
                Initializers.CommandBufferAllocateInfo(cmdPool, VkCommandBufferLevel.Primary, drawCmdBuffers.Count);

            Util.CheckResult(vkAllocateCommandBuffers(device, ref cmdBufAllocateInfo, (VkCommandBuffer*)drawCmdBuffers.Data));
        }

        protected bool checkCommandBuffers()
        {
            foreach (var cmdBuffer in drawCmdBuffers)
            {
                if (cmdBuffer == NullHandle)
                {
                    return false;
                }
            }
            return true;
        }

        protected void destroyCommandBuffers()
        {
            vkFreeCommandBuffers(device, cmdPool, drawCmdBuffers.Count, drawCmdBuffers.Data);
        }

        protected virtual void SetupDepthStencil()
        {
            VkImageCreateInfo image = VkImageCreateInfo.New();
            image.imageType = VkImageType.Image2D;
            image.format = DepthFormat;
            image.extent = new VkExtent3D() { width = width, height = height, depth = 1 };
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

        public Texture createTexture(uint w, uint h, uint bytesPerPixel, byte* tex2DDataPtr)
        {
            VkFormat format = VkFormat.R8g8b8a8Unorm;
            VkFormatProperties formatProperties;
            var texture = new Texture
            {
                width = w,
                height = h,
                mipLevels = 1
            };

            uint totalBytes = bytesPerPixel * w * h;

            // Get Device properites for the requested texture format
            vkGetPhysicalDeviceFormatProperties(physicalDevice, format, &formatProperties);

            // Only use linear tiling if requested (and supported by the Device)
            // Support for linear tiling is mostly limited, so prefer to use
            // optimal tiling instead
            // On most implementations linear tiling will only support a very
            // limited amount of formats and features (mip maps, cubemaps, arrays, etc.)
            uint useStaging = 1;

            VkMemoryAllocateInfo memAllocInfo = Initializers.memoryAllocateInfo();
            VkMemoryRequirements memReqs = new VkMemoryRequirements();

            if (useStaging == 1)
            {
                // Create a host-visible staging buffer that contains the raw image data
                VkBuffer stagingBuffer;
                VkDeviceMemory stagingMemory;

                VkBufferCreateInfo bufferCreateInfo = Initializers.bufferCreateInfo();
                bufferCreateInfo.size = totalBytes;
                // This buffer is used as a transfer source for the buffer copy
                bufferCreateInfo.usage = VkBufferUsageFlags.TransferSrc;
                bufferCreateInfo.sharingMode = VkSharingMode.Exclusive;

                Util.CheckResult(vkCreateBuffer(Graphics.device, &bufferCreateInfo, null, &stagingBuffer));

                // Get memory requirements for the staging buffer (alignment, memory type bits)
                vkGetBufferMemoryRequirements(Graphics.device, stagingBuffer, &memReqs);

                memAllocInfo.allocationSize = memReqs.size;
                // Get memory type index for a host visible buffer
                memAllocInfo.memoryTypeIndex = Graphics.Device.getMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

                Util.CheckResult(vkAllocateMemory(Graphics.device, &memAllocInfo, null, &stagingMemory));
                Util.CheckResult(vkBindBufferMemory(Graphics.device, stagingBuffer, stagingMemory, 0));

                // Copy texture data into staging buffer
                byte* data;
                Util.CheckResult(vkMapMemory(Graphics.device, stagingMemory, 0, memReqs.size, 0, (void**)&data));
                Unsafe.CopyBlock(data, tex2DDataPtr, totalBytes);
                vkUnmapMemory(Graphics.device, stagingMemory);

                // Setup buffer copy regions for each mip level
                NativeList<VkBufferImageCopy> bufferCopyRegions = new NativeList<VkBufferImageCopy>();
                uint offset = 0;

                for (uint i = 0; i < texture.mipLevels; i++)
                {
                    VkBufferImageCopy bufferCopyRegion = new VkBufferImageCopy();
                    bufferCopyRegion.imageSubresource.aspectMask = VkImageAspectFlags.Color;
                    bufferCopyRegion.imageSubresource.mipLevel = i;
                    bufferCopyRegion.imageSubresource.baseArrayLayer = 0;
                    bufferCopyRegion.imageSubresource.layerCount = 1;
                    bufferCopyRegion.imageExtent.width = w;// tex2D.Faces[0].Mipmaps[i].Width;
                    bufferCopyRegion.imageExtent.height = h;// tex2D.Faces[0].Mipmaps[i].Height;
                    bufferCopyRegion.imageExtent.depth = 1;
                    bufferCopyRegion.bufferOffset = offset;

                    bufferCopyRegions.Add(bufferCopyRegion);

                    //   offset += tex2D.Faces[0].Mipmaps[i].SizeInBytes;
                }

                // Create optimal tiled target image
                VkImageCreateInfo imageCreateInfo = Initializers.imageCreateInfo();
                imageCreateInfo.imageType = VkImageType.Image2D;
                imageCreateInfo.format = format;
                imageCreateInfo.mipLevels = texture.mipLevels;
                imageCreateInfo.arrayLayers = 1;
                imageCreateInfo.samples = VkSampleCountFlags.Count1;
                imageCreateInfo.tiling = VkImageTiling.Optimal;
                imageCreateInfo.sharingMode = VkSharingMode.Exclusive;
                // Set initial layout of the image to undefined
                imageCreateInfo.initialLayout = VkImageLayout.Undefined;
                imageCreateInfo.extent = new VkExtent3D { width = texture.width, height = texture.height, depth = 1 };
                imageCreateInfo.usage = VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled;

                Util.CheckResult(vkCreateImage(Graphics.device, &imageCreateInfo, null, out texture.image));

                vkGetImageMemoryRequirements(Graphics.device, texture.image, &memReqs);

                memAllocInfo.allocationSize = memReqs.size;
                memAllocInfo.memoryTypeIndex = Graphics.Device.getMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

                Util.CheckResult(vkAllocateMemory(Graphics.device, &memAllocInfo, null, out texture.deviceMemory));
                Util.CheckResult(vkBindImageMemory(Graphics.device, texture.image, texture.deviceMemory, 0));

                VkCommandBuffer copyCmd = Graphics.Device.createCommandBuffer(VkCommandBufferLevel.Primary, true);

                // Image barrier for optimal image

                // The sub resource range describes the regions of the image we will be transition
                VkImageSubresourceRange subresourceRange = new VkImageSubresourceRange();
                // Image only contains color data
                subresourceRange.aspectMask = VkImageAspectFlags.Color;
                // Start at first mip level
                subresourceRange.baseMipLevel = 0;
                // We will transition on all mip levels
                subresourceRange.levelCount = texture.mipLevels;
                // The 2D texture only has one layer
                subresourceRange.layerCount = 1;

                // Optimal image will be used as destination for the copy, so we must transfer from our
                // initial undefined image layout to the transfer destination layout
                setImageLayout(
                    copyCmd,
                    texture.image,
                     VkImageAspectFlags.Color,
                     VkImageLayout.Undefined,
                     VkImageLayout.TransferDstOptimal,
                    subresourceRange);

                // Copy mip levels from staging buffer
                vkCmdCopyBufferToImage(
                    copyCmd,
                    stagingBuffer,
                    texture.image,
                     VkImageLayout.TransferDstOptimal,
                    bufferCopyRegions.Count,
                    bufferCopyRegions.Data);

                // Change texture image layout to shader read after all mip levels have been copied
                texture.imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
                setImageLayout(
                    copyCmd,
                    texture.image,
                    VkImageAspectFlags.Color,
                    VkImageLayout.TransferDstOptimal,
                    texture.imageLayout,
                    subresourceRange);

                Graphics.Device.flushCommandBuffer(copyCmd, queue, true);

                // Clean up staging resources
                vkFreeMemory(Graphics.device, stagingMemory, null);
                vkDestroyBuffer(Graphics.device, stagingBuffer, null);
            }

            // Create sampler
            // In Vulkan textures are accessed by samplers
            // This separates all the sampling information from the 
            // texture data
            // This means you could have multiple sampler objects
            // for the same texture with different settings
            // Similar to the samplers available with OpenGL 3.3
            VkSamplerCreateInfo sampler = Initializers.samplerCreateInfo();
            sampler.magFilter = VkFilter.Linear;
            sampler.minFilter = VkFilter.Linear;
            sampler.mipmapMode = VkSamplerMipmapMode.Linear;
            sampler.addressModeU = VkSamplerAddressMode.Repeat;
            sampler.addressModeV = VkSamplerAddressMode.Repeat;
            sampler.addressModeW = VkSamplerAddressMode.Repeat;
            sampler.mipLodBias = 0.0f;
            sampler.compareOp = VkCompareOp.Never;
            sampler.minLod = 0.0f;
            // Set max level-of-detail to mip level count of the texture
            sampler.maxLod = (useStaging == 1) ? (float)texture.mipLevels : 0.0f;
            // Enable anisotropic filtering
            // This feature is optional, so we must check if it's supported on the Device
            if (Graphics.Device.features.samplerAnisotropy == 1)
            {
                // Use max. level of anisotropy for this example
                sampler.maxAnisotropy = Graphics.Device.properties.limits.maxSamplerAnisotropy;
                sampler.anisotropyEnable = True;
            }
            else
            {
                // The Device does not support anisotropic filtering
                sampler.maxAnisotropy = 1.0f;
                sampler.anisotropyEnable = False;
            }
            sampler.borderColor = VkBorderColor.FloatOpaqueWhite;
            Util.CheckResult(vkCreateSampler(Graphics.device, ref sampler, null, out texture.sampler));

            // Create image view
            // Textures are not directly accessed by the shaders and
            // are abstracted by image views containing additional
            // information and sub resource ranges
            VkImageViewCreateInfo view = Initializers.imageViewCreateInfo();
            view.viewType = VkImageViewType.Image2D;
            view.format = format;
            view.components = new VkComponentMapping { r = VkComponentSwizzle.R, g = VkComponentSwizzle.G, b = VkComponentSwizzle.B, a = VkComponentSwizzle.A };
            // The subresource range describes the set of mip levels (and array layers) that can be accessed through this image view
            // It's possible to create multiple image views for a single image referring to different (and/or overlapping) ranges of the image
            view.subresourceRange.aspectMask = VkImageAspectFlags.Color;
            view.subresourceRange.baseMipLevel = 0;
            view.subresourceRange.baseArrayLayer = 0;
            view.subresourceRange.layerCount = 1;
            // Linear tiling usually won't support mip maps
            // Only set mip map count if optimal tiling is used
            view.subresourceRange.levelCount = (useStaging == 1) ? texture.mipLevels : 1;
            // The view will be based on the texture's image
            view.image = texture.image;
            Util.CheckResult(vkCreateImageView(Graphics.device, &view, null, out texture.view));
            return texture;
        }

        // Create an image memory barrier for changing the layout of
        // an image and put it into an active command buffer
        void setImageLayout(
            VkCommandBuffer cmdBuffer,
            VkImage image,
            VkImageAspectFlags aspectMask,
            VkImageLayout oldImageLayout,
            VkImageLayout newImageLayout,
            VkImageSubresourceRange subresourceRange)
        {
            // Create an image barrier object
            VkImageMemoryBarrier imageMemoryBarrier = Initializers.imageMemoryBarrier(); ;
            imageMemoryBarrier.oldLayout = oldImageLayout;
            imageMemoryBarrier.newLayout = newImageLayout;
            imageMemoryBarrier.image = image;
            imageMemoryBarrier.subresourceRange = subresourceRange;

            // Only sets masks for layouts used in this example
            // For a more complete version that can be used with other layouts see vks::tools::setImageLayout

            // Source layouts (old)
            switch (oldImageLayout)
            {
                case VkImageLayout.Undefined:
                    // Only valid as initial layout, memory contents are not preserved
                    // Can be accessed directly, no source dependency required
                    imageMemoryBarrier.srcAccessMask = 0;
                    break;
                case VkImageLayout.Preinitialized:
                    // Only valid as initial layout for linear images, preserves memory contents
                    // Make sure host writes to the image have been finished
                    imageMemoryBarrier.srcAccessMask = VkAccessFlags.HostWrite;
                    break;
                case VkImageLayout.TransferDstOptimal:
                    // Old layout is transfer destination
                    // Make sure any writes to the image have been finished
                    imageMemoryBarrier.srcAccessMask = VkAccessFlags.TransferWrite;
                    break;
            }

            // Target layouts (new)
            switch (newImageLayout)
            {
                case VkImageLayout.TransferSrcOptimal:
                    // Transfer source (copy, blit)
                    // Make sure any reads from the image have been finished
                    imageMemoryBarrier.dstAccessMask = VkAccessFlags.TransferRead;
                    break;
                case VkImageLayout.TransferDstOptimal:
                    // Transfer destination (copy, blit)
                    // Make sure any writes to the image have been finished
                    imageMemoryBarrier.dstAccessMask = VkAccessFlags.TransferWrite;
                    break;
                case VkImageLayout.ShaderReadOnlyOptimal:
                    // Shader read (sampler, input attachment)
                    imageMemoryBarrier.dstAccessMask = VkAccessFlags.ShaderRead;
                    break;
            }

            // Put barrier on top of pipeline
            VkPipelineStageFlags srcStageFlags = VkPipelineStageFlags.TopOfPipe;
            VkPipelineStageFlags destStageFlags = VkPipelineStageFlags.TopOfPipe;

            // Put barrier inside setup command buffer
            vkCmdPipelineBarrier(
                cmdBuffer,
                srcStageFlags,
                destStageFlags,
                VkDependencyFlags.None,
                0, null,
                0, null,
                1, &imageMemoryBarrier);
        }

    }
}
