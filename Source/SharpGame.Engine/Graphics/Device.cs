// This code has been adapted from the "Vulkan" C++ example repository, by Sascha Willems: https://github.com/SaschaWillems/Vulkan
// It is a direct translation from the original C++ code and style, with as little transformation as possible.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
    public unsafe class Device
    {
        public const ulong DEFAULT_FENCE_TIMEOUT = 100000000000;
        public static VkInstance VkInstance { get; private set; }
        public static VkPhysicalDevice PhysicalDevice { get; private set; }
        public static VkPhysicalDeviceProperties Properties { get; private set; }
        public static VkPhysicalDeviceFeatures Features { get; private set; }
        public static VkPhysicalDeviceMemoryProperties MemoryProperties { get; private set; }
        public static NativeList<VkQueueFamilyProperties> QueueFamilyProperties { get; } = new NativeList<VkQueueFamilyProperties>();
        public static List<string> SuppertedExcentions { get; } = new List<string>();
        public static VkDevice LogicalDevice => device;
        public static bool EnableDebugMarkers { get; internal set; }

        public static QueueFamilyIndices QFIndices;
        private static VkDevice device;
        private static VkQueue queue;
        private static VkCommandPool commandPool;
        private static VkPipelineCache pipelineCache;
        private static DebugReportCallbackExt debugReportCallbackExt;
        private static CString engineName = "SharpGame";
        public static VkInstance CreateInstance(Settings settings)
        {
            bool enableValidation = settings.Validation;

            VkApplicationInfo appInfo = new VkApplicationInfo()
            {
                sType = VkStructureType.ApplicationInfo,
                apiVersion = new Version(1, 0, 0),
                pApplicationName = settings.ApplicationName,
                pEngineName = engineName,
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
                NativeList<IntPtr> enabledLayerNames = new NativeList<IntPtr>(1)
                {
                    Strings.StandardValidationLayeName
                };
                instanceCreateInfo.enabledLayerCount = enabledLayerNames.Count;
                instanceCreateInfo.ppEnabledLayerNames = (byte**)enabledLayerNames.Data;
            }

            VkInstance instance;
            VulkanUtil.CheckResult(vkCreateInstance(&instanceCreateInfo, null, &instance));
            VkInstance = instance;

            if (settings.Validation)
            {
                debugReportCallbackExt = CreateDebugReportCallback();
            }

            return instance;
        }

        public static VkDevice Init(VkPhysicalDeviceFeatures enabledFeatures, NativeList<IntPtr> enabledExtensions,
            bool useSwapChain = true, VkQueueFlags requestedQueueTypes = VkQueueFlags.Graphics | VkQueueFlags.Compute)
        {
            // Physical Device
            uint gpuCount = 0;
            VulkanUtil.CheckResult(vkEnumeratePhysicalDevices(VkInstance, &gpuCount, null));
            Debug.Assert(gpuCount > 0);
            // Enumerate devices
            IntPtr* physicalDevices = stackalloc IntPtr[(int)gpuCount];

            VkResult err = vkEnumeratePhysicalDevices(VkInstance, &gpuCount, (VkPhysicalDevice*)physicalDevices);
            if (err != VkResult.Success)
            {
                throw new InvalidOperationException("Could not enumerate physical devices.");
            }

            // GPU selection

            // Select physical Device to be used for the Vulkan example
            // Defaults to the first Device unless specified by command line

            uint selectedDevice = 0;
            // TODO: Implement arg parsing, etc.

            var physicalDevice = ((VkPhysicalDevice*)physicalDevices)[selectedDevice];

            Debug.Assert(physicalDevice.Handle != IntPtr.Zero);
            PhysicalDevice = physicalDevice;
            vkGetPhysicalDeviceProperties(physicalDevice, out VkPhysicalDeviceProperties properties);
            Properties = properties;

            // Features should be checked by the examples before using them
            vkGetPhysicalDeviceFeatures(physicalDevice, out VkPhysicalDeviceFeatures features);
            Features = features;
            // Memory properties are used regularly for creating all kinds of buffers
            VkPhysicalDeviceMemoryProperties memoryProperties;
            vkGetPhysicalDeviceMemoryProperties(physicalDevice, out memoryProperties);
            MemoryProperties = memoryProperties;
            // Queue family properties, used for setting up requested queues upon device creation
            uint queueFamilyCount = 0;
            vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref queueFamilyCount, null);
            Debug.Assert(queueFamilyCount > 0);
            QueueFamilyProperties.Resize(queueFamilyCount);
            vkGetPhysicalDeviceQueueFamilyProperties(
                physicalDevice,
                &queueFamilyCount,
                (VkQueueFamilyProperties*)QueueFamilyProperties.Data.ToPointer());
            QueueFamilyProperties.Count = queueFamilyCount;

            // Get list of supported extensions
            uint extCount = 0;
            vkEnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, ref extCount, null);
            if (extCount > 0)
            {
                VkExtensionProperties* extensions = stackalloc VkExtensionProperties[(int)extCount];
                if (vkEnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, ref extCount, extensions) == VkResult.Success)
                {
                    for (uint i = 0; i < extCount; i++)
                    {
                        var ext = extensions[i];
                        //string strExt = CString.FromPointer(ext.extensionName);
                        enabledExtensions.Add((IntPtr)ext.extensionName);
                        //supportedExtensions.push_back(ext.extensionName);
                        // TODO: fixed-length char arrays are not being parsed correctly.
                    }
                }
            }

            VulkanUtil.CheckResult(CreateLogicalDevice(enabledFeatures, enabledExtensions));
            queue = GetDeviceQueue(Device.QFIndices.Graphics, 0);
            return device;
        }

        static VkResult CreateLogicalDevice(VkPhysicalDeviceFeatures enabledFeatures, NativeList<IntPtr> enabledExtensions,
            bool useSwapChain = true, VkQueueFlags requestedQueueTypes = VkQueueFlags.Graphics | VkQueueFlags.Compute)
        {
            // Desired queues need to be requested upon logical device creation
            // Due to differing queue family configurations of Vulkan implementations this can be a bit tricky, especially if the application
            // requests different queue types

            using (NativeList<VkDeviceQueueCreateInfo> queueCreateInfos = new NativeList<VkDeviceQueueCreateInfo>())
            {
                float defaultQueuePriority = 0.0f;

                // Graphics queue
                if ((requestedQueueTypes & VkQueueFlags.Graphics) != 0)
                {
                    QFIndices.Graphics = GetQueueFamilyIndex(VkQueueFlags.Graphics);
                    VkDeviceQueueCreateInfo queueInfo = new VkDeviceQueueCreateInfo
                    {
                        sType = VkStructureType.DeviceQueueCreateInfo,
                        queueFamilyIndex = QFIndices.Graphics,
                        queueCount = 1,
                        pQueuePriorities = &defaultQueuePriority
                    };
                    queueCreateInfos.Add(queueInfo);
                }
                else
                {
                    QFIndices.Graphics = (uint)NullHandle;
                }

                // Dedicated compute queue
                if ((requestedQueueTypes & VkQueueFlags.Compute) != 0)
                {
                    QFIndices.Compute = GetQueueFamilyIndex(VkQueueFlags.Compute);
                    if (QFIndices.Compute != QFIndices.Graphics)
                    {
                        // If compute family index differs, we need an additional queue create info for the compute queue
                        VkDeviceQueueCreateInfo queueInfo = new VkDeviceQueueCreateInfo
                        {
                            sType = VkStructureType.DeviceQueueCreateInfo,
                            queueFamilyIndex = QFIndices.Compute,
                            queueCount = 1,
                            pQueuePriorities = &defaultQueuePriority
                        };
                        queueCreateInfos.Add(queueInfo);
                    }
                }
                else
                {
                    // Else we use the same queue
                    QFIndices.Compute = QFIndices.Graphics;
                }

                // Dedicated transfer queue
                if ((requestedQueueTypes & VkQueueFlags.Transfer) != 0)
                {
                    QFIndices.Transfer = GetQueueFamilyIndex(VkQueueFlags.Transfer);
                    if (QFIndices.Transfer != QFIndices.Graphics && QFIndices.Transfer != QFIndices.Compute)
                    {
                        // If compute family index differs, we need an additional queue create info for the transfer queue
                        VkDeviceQueueCreateInfo queueInfo = new VkDeviceQueueCreateInfo
                        {
                            sType = VkStructureType.DeviceQueueCreateInfo,
                            queueFamilyIndex = QFIndices.Transfer,
                            queueCount = 1,
                            pQueuePriorities = &defaultQueuePriority
                        };
                        queueCreateInfos.Add(queueInfo);
                    }
                }
                else
                {
                    // Else we use the same queue
                    QFIndices.Transfer = QFIndices.Graphics;
                }

                // Create the logical device representation
                using (NativeList<IntPtr> deviceExtensions = new NativeList<IntPtr>(enabledExtensions))
                {
                    if (useSwapChain)
                    {
                        // If the device will be used for presenting to a display via a swapchain we need to request the swapchain extension
                        deviceExtensions.Add(Strings.VK_KHR_SWAPCHAIN_EXTENSION_NAME);
                    }

                    VkDeviceCreateInfo deviceCreateInfo = VkDeviceCreateInfo.New();
                    deviceCreateInfo.queueCreateInfoCount = queueCreateInfos.Count;
                    deviceCreateInfo.pQueueCreateInfos = (VkDeviceQueueCreateInfo*)queueCreateInfos.Data.ToPointer();
                    deviceCreateInfo.pEnabledFeatures = &enabledFeatures;

                    if (deviceExtensions.Count > 0)
                    {
                        deviceCreateInfo.enabledExtensionCount = deviceExtensions.Count;
                        deviceCreateInfo.ppEnabledExtensionNames = (byte**)deviceExtensions.Data.ToPointer();
                    }

                    VkResult result = vkCreateDevice(PhysicalDevice, &deviceCreateInfo, null, out device);
                    if (result == VkResult.Success)
                    {
                        // Create a default command pool for graphics command buffers
                        commandPool = CreateCommandPool(QFIndices.Graphics);
                    }

                    VkPipelineCacheCreateInfo pipelineCacheCreateInfo = VkPipelineCacheCreateInfo.New();
                    VulkanUtil.CheckResult(vkCreatePipelineCache(LogicalDevice, ref pipelineCacheCreateInfo, null, out pipelineCache));
                    return result;
                }
            }

        }

        private static DebugReportCallbackExt CreateDebugReportCallback()
        {
            // Attach debug callback.
            var debugReportCreateInfo = new DebugReportCallbackCreateInfoExt(
                VkDebugReportFlagsEXT.InformationEXT |
                VkDebugReportFlagsEXT.WarningEXT |
                VkDebugReportFlagsEXT.PerformanceWarningEXT |
                VkDebugReportFlagsEXT.ErrorEXT |
                VkDebugReportFlagsEXT.DebugEXT,
                (args) =>
                {
                    Debug.WriteLine($"[{args.Flags}][{args.LayerPrefix}] {args.Message}");
                    return args.Flags.HasFlag(DebugReportFlagsExt.Error);
                }, IntPtr.Zero
            );
            return new DebugReportCallbackExt(VkInstance, ref debugReportCreateInfo);
        }

        private static uint GetQueueFamilyIndex(VkQueueFlags queueFlags)
        {
            // Dedicated queue for compute
            // Try to find a queue family index that supports compute but not graphics
            if ((queueFlags & VkQueueFlags.Compute) != 0)
            {
                for (uint i = 0; i < QueueFamilyProperties.Count; i++)
                {
                    if (((QueueFamilyProperties[i].queueFlags & queueFlags) != 0)
                        && (QueueFamilyProperties[i].queueFlags & VkQueueFlags.Graphics) == 0)
                    {
                        return i;
                    }
                }
            }

            // Dedicated queue for transfer
            // Try to find a queue family index that supports transfer but not graphics and compute
            if ((queueFlags & VkQueueFlags.Transfer) != 0)
            {
                for (uint i = 0; i < QueueFamilyProperties.Count; i++)
                {
                    if (((QueueFamilyProperties[i].queueFlags & queueFlags) != 0)
                        && (QueueFamilyProperties[i].queueFlags & VkQueueFlags.Graphics) == 0
                        && (QueueFamilyProperties[i].queueFlags & VkQueueFlags.Compute) == 0)
                    {
                        return i;
                    }
                }
            }

            // For other queue types or if no separate compute queue is present, return the first one to support the requested flags
            for (uint i = 0; i < QueueFamilyProperties.Count; i++)
            {
                if ((QueueFamilyProperties[i].queueFlags & queueFlags) != 0)
                {
                    return i;
                }
            }

            throw new InvalidOperationException("Could not find a matching queue family index");
        }

        public static void Shutdown()
        {
            debugReportCallbackExt?.Dispose();
        }

        public static VkQueue GetDeviceQueue(uint queueFamilyIndex, uint queueIndex)
        {
            vkGetDeviceQueue(device, queueFamilyIndex, queueIndex, out VkQueue pQueue);
            return pQueue;
        }

        public static Format GetSupportedDepthFormat()
        {
            // Since all depth formats may be optional, we need to find a suitable depth format to use
            // Start with the highest precision packed format
            List<VkFormat> depthFormats = new List<VkFormat>()
            {
                VkFormat.D32SfloatS8Uint,
                VkFormat.D32Sfloat,
                VkFormat.D24UnormS8Uint,
                VkFormat.D16UnormS8Uint,
                VkFormat.D16Unorm,
            };

            foreach (VkFormat format in depthFormats)
            {
                VkFormatProperties formatProps;
                vkGetPhysicalDeviceFormatProperties(PhysicalDevice, format, &formatProps);
                // Format must support depth stencil attachment for optimal tiling
                if ((formatProps.optimalTilingFeatures & VkFormatFeatureFlags.DepthStencilAttachment) != 0)
                {
                    return (Format)format;
                }
            }

            return Format.Undefined;
        }

        public static VkSemaphore CreateSemaphore(uint flags = 0)
        {
            var semaphoreCreateInfo = VkSemaphoreCreateInfo.New();
            semaphoreCreateInfo.flags = flags;
            VulkanUtil.CheckResult(vkCreateSemaphore(device, ref semaphoreCreateInfo, null, out VkSemaphore pSemaphore));
            return pSemaphore;
        }

        public static VkImage CreateImage(ref VkImageCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateImage(device, ref pCreateInfo, null, out VkImage pImage));
            return pImage;
        }

        public static void Destroy(VkImage image)
        {
            vkDestroyImage(device, image, null);
        }

        public static VkImageView CreateImageView(ref VkImageViewCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateImageView(device, ref pCreateInfo, null, out VkImageView pView));
            return pView;
        }

        public static void Destroy(VkImageView imageView)
        {
            vkDestroyImageView(device, imageView, null);
        }

        public static VkSampler CreateSampler(ref VkSamplerCreateInfo vkSamplerCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateSampler(device, ref vkSamplerCreateInfo, null, out VkSampler vkSampler));
            return vkSampler;
        }

        public static void Destroy(VkSampler sampler)
        {
            vkDestroySampler(device, sampler, null);
        }

        public static VkFramebuffer CreateFramebuffer(ref VkFramebufferCreateInfo framebufferCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateFramebuffer(device, ref framebufferCreateInfo, null, out VkFramebuffer framebuffer));
            return framebuffer;
        }

        public static void Destroy(VkFramebuffer framebuffer)
        {
            vkDestroyFramebuffer(device, framebuffer, null);
        }

        public static VkRenderPass CreateRenderPass(ref VkRenderPassCreateInfo createInfo)
        {
            VulkanUtil.CheckResult(vkCreateRenderPass(device, ref createInfo, null, out VkRenderPass pRenderPass));
            return pRenderPass;
        }

        public static void Destroy(VkRenderPass renderPass)
        {
            vkDestroyRenderPass(device, renderPass, null);
        }

        public static VkBuffer CreateBuffer(ref VkBufferCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateBuffer(device, ref pCreateInfo, null, out VkBuffer buffer));
            return buffer;
        }

        public static void GetBufferMemoryRequirements(VkBuffer buffer, out VkMemoryRequirements pMemoryRequirements)
        {
            vkGetBufferMemoryRequirements(device, buffer, out pMemoryRequirements);
        }

        public static VkDeviceMemory AllocateMemory(ref VkMemoryAllocateInfo pAllocateInfo)
        {
            VulkanUtil.CheckResult(vkAllocateMemory(device, ref pAllocateInfo, null, out VkDeviceMemory pMemory));
            return pMemory;
        }

        public static void BindBufferMemory(VkBuffer buffer, VkDeviceMemory memory, ulong memoryOffset)
        {
            VulkanUtil.CheckResult(vkBindBufferMemory(device, buffer, memory, memoryOffset));
        }

        public static void CreateBuffer(VkBufferUsageFlags usageFlags, VkMemoryPropertyFlags memoryPropertyFlags, ulong size, VkBuffer* buffer, VkDeviceMemory* memory, void* data = null)
        {
            // Create the buffer handle
            VkBufferCreateInfo bufferCreateInfo = Builder.BufferCreateInfo(usageFlags, size);
            bufferCreateInfo.sharingMode = VkSharingMode.Exclusive;
            VulkanUtil.CheckResult(vkCreateBuffer(LogicalDevice, &bufferCreateInfo, null, buffer));

            // Create the memory backing up the buffer handle
            VkMemoryRequirements memReqs;
            VkMemoryAllocateInfo memAlloc = VkMemoryAllocateInfo.New();
            vkGetBufferMemoryRequirements(LogicalDevice, *buffer, &memReqs);
            memAlloc.allocationSize = memReqs.size;
            // Find a memory type index that fits the properties of the buffer
            memAlloc.memoryTypeIndex = GetMemoryType(memReqs.memoryTypeBits, memoryPropertyFlags);
            VulkanUtil.CheckResult(vkAllocateMemory(LogicalDevice, &memAlloc, null, memory));

            // If a pointer to the buffer data has been passed, map the buffer and copy over the data
            if (data != null)
            {
                void* mapped;
                VulkanUtil.CheckResult(vkMapMemory(LogicalDevice, *memory, 0, size, 0, &mapped));
                Unsafe.CopyBlock(mapped, data, (uint)size);
                // If host coherency hasn't been requested, do a manual flush to make writes visible
                if ((memoryPropertyFlags & VkMemoryPropertyFlags.HostCoherent) == 0)
                {
                    VkMappedMemoryRange mappedRange = VkMappedMemoryRange.New();
                    mappedRange.memory = *memory;
                    mappedRange.offset = 0;
                    mappedRange.size = size;
                    vkFlushMappedMemoryRanges(LogicalDevice, 1, &mappedRange);
                }
                vkUnmapMemory(LogicalDevice, *memory);
            }

            // Attach the memory to the buffer object
            VulkanUtil.CheckResult(vkBindBufferMemory(LogicalDevice, *buffer, *memory, 0));

        }

        /*
        public static void CreateImage(
            int width,
            int height,
            Format format,
            VkImageTiling tiling,
            ImageUsageFlags usage,
            VkMemoryPropertyFlags properties,
            out Image image,
            out VkDeviceMemory memory)
        {
            ImageCreateInfo imageCI = new ImageCreateInfo
            {
                imageType = ImageType.Image2D
            };
            imageCI.extent.width = width;
            imageCI.extent.height = height;
            imageCI.extent.depth = 1;
            imageCI.mipLevels = 1;
            imageCI.arrayLayers = 1;
            imageCI.format = format;
            imageCI.tiling = tiling;
            imageCI.initialLayout = ImageLayout.Preinitialized;
            imageCI.usage = usage;
            imageCI.sharingMode = VkSharingMode.Exclusive;
            imageCI.samples = SampleCountFlags.Count1;

            vkCreateImage(device, ref imageCI, null, out image);

            vkGetImageMemoryRequirements(device, image, out VkMemoryRequirements memRequirements);
            VkMemoryAllocateInfo allocInfo = VkMemoryAllocateInfo.New();
            allocInfo.allocationSize = memRequirements.size;
            allocInfo.memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, properties);
            vkAllocateMemory(device, ref allocInfo, null, out memory);

            vkBindImageMemory(device, image, memory, 0);
        }
        */
        private static uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
        {
            vkGetPhysicalDeviceMemoryProperties(PhysicalDevice, out VkPhysicalDeviceMemoryProperties memProperties);
            for (int i = 0; i < memProperties.memoryTypeCount; i++)
            {
                if (((typeFilter & (1 << i)) != 0)
                    && (memProperties.GetMemoryType((uint)i).propertyFlags & properties) == properties)
                {
                    return (uint)i;
                }
            }

            throw new InvalidOperationException("No suitable memory type.");
        }


        public static void GetImageMemoryRequirements(VkImage image, out VkMemoryRequirements pMemoryRequirements)
        {
            vkGetImageMemoryRequirements(device, image, out pMemoryRequirements);
        }

        public static void BindImageMemory(VkImage image, VkDeviceMemory memory, ulong offset)
        {
            VulkanUtil.CheckResult(vkBindImageMemory(device, image, memory, offset));
        }

        public static void CopyImage(VkImage srcImage, VkImage dstImage, uint width, uint height, uint level = 0)
        {
            VkImageSubresourceLayers subresource = new VkImageSubresourceLayers
            {
                mipLevel = level,
                layerCount = 1,
                aspectMask = VkImageAspectFlags.Color,
                baseArrayLayer = 0
            };

            VkImageCopy region = new VkImageCopy
            {
                dstSubresource = subresource,
                srcSubresource = subresource
            };
            region.extent.width = width;
            region.extent.height = height;
            region.extent.depth = 1;

            VkCommandBuffer copyCmd = BeginOneTimeCommands();
            vkCmdCopyImage(copyCmd, srcImage, VkImageLayout.TransferSrcOptimal, dstImage, VkImageLayout.TransferDstOptimal, 1, ref region);
            EndOneTimeCommands(copyCmd);
        }

        public static void CreateImageView(VkImage image, VkFormat format, out VkImageView imageView)
        {
            VkImageViewCreateInfo imageViewCI = VkImageViewCreateInfo.New();
            imageViewCI.image = image;
            imageViewCI.viewType = VkImageViewType.Image2D;
            imageViewCI.format = format;
            imageViewCI.subresourceRange.aspectMask = VkImageAspectFlags.Color;
            imageViewCI.subresourceRange.baseMipLevel = 0;
            imageViewCI.subresourceRange.levelCount = 1;
            imageViewCI.subresourceRange.baseArrayLayer = 0;
            imageViewCI.subresourceRange.layerCount = 1;

            vkCreateImageView(device, ref imageViewCI, null, out imageView);
        }

        public static void TransitionImageLayout(VkImage image, VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout)
        {
            VkCommandBuffer cb = BeginOneTimeCommands();

            VkImageMemoryBarrier barrier = VkImageMemoryBarrier.New();
            barrier.oldLayout = oldLayout;
            barrier.newLayout = newLayout;
            barrier.srcQueueFamilyIndex = QueueFamilyIgnored;
            barrier.dstQueueFamilyIndex = QueueFamilyIgnored;
            barrier.image = image;
            barrier.subresourceRange.aspectMask = VkImageAspectFlags.Color;
            barrier.subresourceRange.baseMipLevel = 0;
            barrier.subresourceRange.levelCount = 1;
            barrier.subresourceRange.baseArrayLayer = 0;
            barrier.subresourceRange.layerCount = 1;

            vkCmdPipelineBarrier(
                cb,
                VkPipelineStageFlags.TopOfPipe,
                VkPipelineStageFlags.TopOfPipe,
                VkDependencyFlags.None,
                0, null,
                0, null,
                1, &barrier);

            EndOneTimeCommands(cb);
        }


        public static void TransitionImageLayout(VkCommandBuffer cb, VkImage image, VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout)
        {
            VkImageMemoryBarrier barrier = VkImageMemoryBarrier.New();
            barrier.oldLayout = oldLayout;
            barrier.newLayout = newLayout;
            barrier.srcQueueFamilyIndex = QueueFamilyIgnored;
            barrier.dstQueueFamilyIndex = QueueFamilyIgnored;
            barrier.image = image;
            barrier.subresourceRange.aspectMask = VkImageAspectFlags.Color;
            barrier.subresourceRange.baseMipLevel = 0;
            barrier.subresourceRange.levelCount = 1;
            barrier.subresourceRange.baseArrayLayer = 0;
            barrier.subresourceRange.layerCount = 1;

            vkCmdPipelineBarrier(
                cb,
                VkPipelineStageFlags.TopOfPipe,
                VkPipelineStageFlags.TopOfPipe,
                VkDependencyFlags.None,
                0, null,
                0, null,
                1, &barrier);

        }


        public static VkCommandBuffer BeginOneTimeCommands()
        {
            VkCommandBufferAllocateInfo allocInfo = VkCommandBufferAllocateInfo.New();
            allocInfo.commandBufferCount = 1;
            allocInfo.commandPool = commandPool;
            allocInfo.level = VkCommandBufferLevel.Primary;

            vkAllocateCommandBuffers(device, ref allocInfo, out VkCommandBuffer cb);

            VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
            beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;

            vkBeginCommandBuffer(cb, ref beginInfo);

            return cb;
        }

        public static void EndOneTimeCommands(VkCommandBuffer cb)
        {
            vkEndCommandBuffer(cb);

            VkSubmitInfo submitInfo = VkSubmitInfo.New();
            submitInfo.commandBufferCount = 1;
            submitInfo.pCommandBuffers = &cb;

            vkQueueSubmit(queue, 1, &submitInfo, VkFence.Null);
            vkQueueWaitIdle(queue);

            vkFreeCommandBuffers(device, commandPool, 1, ref cb);
        }


        public static VkCommandPool CreateCommandPool(uint queueFamilyIndex, VkCommandPoolCreateFlags createFlags = VkCommandPoolCreateFlags.ResetCommandBuffer)
        {
            VkCommandPoolCreateInfo cmdPoolInfo = VkCommandPoolCreateInfo.New();
            cmdPoolInfo.queueFamilyIndex = queueFamilyIndex;
            cmdPoolInfo.flags = createFlags;
            VulkanUtil.CheckResult(vkCreateCommandPool(device, &cmdPoolInfo, null, out VkCommandPool cmdPool));
            return cmdPool;
        }

        public static VkCommandBuffer CreateCommandBuffer(VkCommandBufferLevel level, bool begin = false)
        {
            VkCommandBufferAllocateInfo cmdBufAllocateInfo = VkCommandBufferAllocateInfo.New();
            cmdBufAllocateInfo.commandPool = commandPool;
            cmdBufAllocateInfo.level = level;
            cmdBufAllocateInfo.commandBufferCount = 1;

            VkCommandBuffer cmdBuffer;
            VulkanUtil.CheckResult(vkAllocateCommandBuffers(device, ref cmdBufAllocateInfo, out cmdBuffer));

            // If requested, also start recording for the new command buffer
            if (begin)
            {
                VkCommandBufferBeginInfo cmdBufInfo = VkCommandBufferBeginInfo.New();
                VulkanUtil.CheckResult(vkBeginCommandBuffer(cmdBuffer, ref cmdBufInfo));
            }

            return cmdBuffer;
        }

        public static void FlushCommandBuffer(VkCommandBuffer commandBuffer, VkQueue queue, bool free = true)
        {
            if (commandBuffer.Handle == NullHandle)
            {
                return;
            }

            VulkanUtil.CheckResult(vkEndCommandBuffer(commandBuffer));

            VkSubmitInfo submitInfo = VkSubmitInfo.New();
            submitInfo.commandBufferCount = 1;
            submitInfo.pCommandBuffers = &commandBuffer;

            // Create fence to ensure that the command buffer has finished executing
            VkFenceCreateInfo fenceInfo = VkFenceCreateInfo.New();
            fenceInfo.flags = VkFenceCreateFlags.None;
            VkFence fence;
            VulkanUtil.CheckResult(vkCreateFence(device, &fenceInfo, null, &fence));

            // Submit to the queue
            VulkanUtil.CheckResult(vkQueueSubmit(queue, 1, &submitInfo, fence));
            // Wait for the fence to signal that command buffer has finished executing
            VulkanUtil.CheckResult(vkWaitForFences(device, 1, &fence, True, DEFAULT_FENCE_TIMEOUT));

            vkDestroyFence(device, fence, null);

            if (free)
            {
                vkFreeCommandBuffers(device, commandPool, 1, &commandBuffer);
            }
        }

        public static void* MapMemory(VkDeviceMemory memory, ulong offset, ulong size, uint flags)
        {
            void* mappedLocal;
            vkMapMemory(device, memory, offset, size, flags, &mappedLocal);
            return mappedLocal;
        }

        public static void UnmapMemory(VkDeviceMemory memory)
        {
            vkUnmapMemory(device, memory);
        }

        public static uint GetMemoryType(uint typeBits, VkMemoryPropertyFlags properties, uint* memTypeFound = null)
        {
            for (uint i = 0; i < MemoryProperties.memoryTypeCount; i++)
            {
                if ((typeBits & 1) == 1)
                {
                    if ((MemoryProperties.GetMemoryType(i).propertyFlags & properties) == properties)
                    {
                        if (memTypeFound != null)
                        {
                            *memTypeFound = True;
                        }
                        return i;
                    }
                }
                typeBits >>= 1;
            }

            if (memTypeFound != null)
            {
                *memTypeFound = False;
                return 0;
            }
            else
            {
                throw new InvalidOperationException("Could not find a matching memory type");
            }
        }

        public static VkShaderModule CreateShaderModule(ref VkShaderModuleCreateInfo shaderModuleCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateShaderModule(device, ref shaderModuleCreateInfo, null, out VkShaderModule shaderModule));
            return shaderModule;
        }

        public static void Destroy(VkShaderModule shaderModule)
        {
            vkDestroyShaderModule(device, shaderModule, IntPtr.Zero);
        }

        public static VkPipeline CreateGraphicsPipeline(ref VkGraphicsPipelineCreateInfo pCreateInfos)
        {
            VulkanUtil.CheckResult(vkCreateGraphicsPipelines(device, pipelineCache,
                1, ref pCreateInfos, IntPtr.Zero, out VkPipeline pPipelines));
            return pPipelines;
        }

        public static VkPipeline CreateComputePipeline(ref VkComputePipelineCreateInfo pCreateInfos)
        {
            VulkanUtil.CheckResult(vkCreateComputePipelines(device, pipelineCache, 1, ref pCreateInfos, IntPtr.Zero, out VkPipeline pPipelines));
            return pPipelines;
        }

        public static void Destroy<T>(ref T handle) where T : struct
        {
            switch (handle)
            {
                case VkPipeline pipeline:
                    if(pipeline != 0)
                    {
                        vkDestroyPipeline(device, pipeline, null);
                        handle = default;
                    }
                    break;
                case VkPipelineLayout pipelineLayout:
                    if (pipelineLayout != 0)
                    {
                        vkDestroyPipelineLayout(device, pipelineLayout, null);
                        handle = default;
                    }
                    break;
            }
        }

        public static void DestroyPipeline(VkPipeline pipeline)
        {
            vkDestroyPipeline(device, pipeline, null);
        }
        
        public static void DestroyBuffer(VkBuffer buffer)
        {
            vkDestroyBuffer(device, buffer, null);
        }

        public static void FreeMemory(VkDeviceMemory memory)
        {
            vkFreeMemory(device, memory, null);
        }

        public static void DestroyPipelineLayout(VkPipelineLayout pipelineLayout)
        {
            vkDestroyPipelineLayout(device, pipelineLayout, null);
        }

        public static void DestroyDescriptorSetLayout(VkDescriptorSetLayout descriptorSetLayout)
        {
            vkDestroyDescriptorSetLayout(device, descriptorSetLayout, null);
        }

        public struct QueueFamilyIndices
        {
            public uint Graphics;
            public uint Compute;
            public uint Transfer;
        }
    }

}
