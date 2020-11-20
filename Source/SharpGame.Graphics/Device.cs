using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGame
{
    using Vulkan;
    using static Vulkan.VulkanNative;

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
        public static bool EnableDebugMarkers { get; internal set; }
        public static uint QFGraphics { get; private set; }
        public static uint QFCompute { get; private set; }
        public static uint QFTransfer { get; private set; }

        public static int MaxPushConstantsSize => (int)Device.Properties.limits.maxPushConstantsSize;

        private static VkDevice device;
        private static VkQueue queue;
        private static VkCommandPool commandPool;
        private static VkPipelineCache pipelineCache;
        private static DebugReportCallbackExt debugReportCallbackExt;
        private static UTF8String engineName = "SharpGame";
        private static List<string> supportedExtensions = new List<string>();

        private static NativeList<IntPtr> instanceExtensions = new NativeList<IntPtr>(8);

        public static VkDevice Create(Settings settings, VkPhysicalDeviceFeatures enabledFeatures, NativeList<IntPtr> enabledExtensions,
            bool useSwapChain = true, VkQueueFlags requestedQueueTypes = VkQueueFlags.Graphics | VkQueueFlags.Compute | VkQueueFlags.Transfer)
        {
            instanceExtensions.Add(Strings.VK_KHR_SURFACE_EXTENSION_NAME);
            instanceExtensions.Add(Strings.VK_KHR_GET_PHYSICAL_DEVICE_PROPERTIES_2_EXTENSION_NAME);

            enabledExtensions.Add(Strings.VK_KHR_MAINTENANCE1_EXTENSION_NAME);
            enabledExtensions.Add(Strings.VK_EXT_INLINE_UNIFORM_BLOCK_EXTENSION_NAME);

            //enabledExtensions.Add(Strings.VK_DESCRIPTOR_BINDING_PARTIALLY_BOUND_BIT_EXT);

            CreateInstance(settings);

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

            if (features.multiDrawIndirect)
            {
                enabledFeatures.multiDrawIndirect = true;
            }
            // Enable anisotropic filtering if supported
            if (features.samplerAnisotropy)
            {
                enabledFeatures.samplerAnisotropy = true;
            }
            // Enable texture compression  
            if (features.textureCompressionBC)
            {
                enabledFeatures.textureCompressionBC = true;
            }
            else if (features.textureCompressionASTC_LDR)
            {
                enabledFeatures.textureCompressionASTC_LDR = true;
            }
            else if (features.textureCompressionETC2)
            {
                enabledFeatures.textureCompressionETC2 = true;
            }

            if (features.sparseBinding && features.sparseResidencyImage2D)
            {
                enabledFeatures.shaderResourceResidency = true;
                enabledFeatures.shaderResourceMinLod = true;
                enabledFeatures.sparseBinding = true;
                enabledFeatures.sparseResidencyImage2D = true;
            }
            else
            {
                Log.Warn("Sparse binding not supported");
            }


            // Memory properties are used regularly for creating all kinds of buffers
            VkPhysicalDeviceMemoryProperties memoryProperties;
            vkGetPhysicalDeviceMemoryProperties(physicalDevice, out memoryProperties);
            MemoryProperties = memoryProperties;
            // Queue family properties, used for setting up requested queues upon device creation
            uint queueFamilyCount = 0;
            vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref queueFamilyCount, null);
            Debug.Assert(queueFamilyCount > 0);
            QueueFamilyProperties.Resize(queueFamilyCount);
            vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, QueueFamilyProperties.DataPtr);

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
                        string strExt = UTF8String.FromPointer(ext.extensionName);
                        //enabledExtensions.Add((IntPtr)ext.extensionName);
                        supportedExtensions.Add(strExt);
                    }
                }
            }

            VulkanUtil.CheckResult(CreateLogicalDevice(Features, enabledExtensions));
            queue = GetDeviceQueue(QFGraphics, 0);

            vkCmdPushDescriptorSetKHR();

            return device;
        }

        static VkInstance CreateInstance(Settings settings)
        {
            bool enableValidation = settings.Validation;

            VkApplicationInfo appInfo = new VkApplicationInfo()
            {
                sType = VkStructureType.ApplicationInfo,
                apiVersion = new Version(1, 0, 0),
                pApplicationName = settings.ApplicationName,
                pEngineName = engineName,
            };


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

            using NativeList<IntPtr> enabledLayerNames = new NativeList<IntPtr> { Strings.StandardValidationLayeName };

            if (enableValidation)
            {
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

        static VkResult CreateLogicalDevice(VkPhysicalDeviceFeatures enabledFeatures, NativeList<IntPtr> enabledExtensions,
            bool useSwapChain = true, VkQueueFlags requestedQueueTypes = VkQueueFlags.Graphics | VkQueueFlags.Compute | VkQueueFlags.Transfer)
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
                    QFGraphics = GetQueueFamilyIndex(VkQueueFlags.Graphics);
                    VkDeviceQueueCreateInfo queueInfo = new VkDeviceQueueCreateInfo
                    {
                        sType = VkStructureType.DeviceQueueCreateInfo,
                        queueFamilyIndex = QFGraphics,
                        queueCount = 1,
                        pQueuePriorities = &defaultQueuePriority
                    };
                    queueCreateInfos.Add(queueInfo);
                }
                else
                {
                    QFGraphics = (uint)NullHandle;
                }

                // Dedicated compute queue
                if ((requestedQueueTypes & VkQueueFlags.Compute) != 0)
                {
                    QFCompute = GetQueueFamilyIndex(VkQueueFlags.Compute);
                    if (QFCompute != QFGraphics)
                    {
                        // If compute family index differs, we need an additional queue create info for the compute queue
                        VkDeviceQueueCreateInfo queueInfo = new VkDeviceQueueCreateInfo
                        {
                            sType = VkStructureType.DeviceQueueCreateInfo,
                            queueFamilyIndex = QFCompute,
                            queueCount = 1,
                            pQueuePriorities = &defaultQueuePriority
                        };
                        queueCreateInfos.Add(queueInfo);
                    }
                }
                else
                {
                    // Else we use the same queue
                    QFCompute = QFGraphics;
                }

                // Dedicated transfer queue
                if ((requestedQueueTypes & VkQueueFlags.Transfer) != 0)
                {
                    QFTransfer = GetQueueFamilyIndex(VkQueueFlags.Transfer);
                    if (QFTransfer != QFGraphics && QFTransfer != QFCompute)
                    {
                        // If compute family index differs, we need an additional queue create info for the transfer queue
                        VkDeviceQueueCreateInfo queueInfo = new VkDeviceQueueCreateInfo
                        {
                            sType = VkStructureType.DeviceQueueCreateInfo,
                            queueFamilyIndex = QFTransfer,
                            queueCount = 1,
                            pQueuePriorities = &defaultQueuePriority
                        };
                        queueCreateInfos.Add(queueInfo);
                    }
                }
                else
                {
                    // Else we use the same queue
                    QFTransfer = QFGraphics;
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
                    deviceCreateInfo.pQueueCreateInfos = queueCreateInfos.DataPtr;
                    deviceCreateInfo.pEnabledFeatures = &enabledFeatures;

                    if (deviceExtensions.Count > 0)
                    {
                        deviceCreateInfo.enabledExtensionCount = deviceExtensions.Count;
                        deviceCreateInfo.ppEnabledExtensionNames = (byte**)deviceExtensions.Data;
                    }

                    VkResult result = vkCreateDevice(PhysicalDevice, &deviceCreateInfo, null, out device);
                    if (result == VkResult.Success)
                    {
                        // Create a default command pool for graphics command buffers
                        commandPool = CreateCommandPool(QFGraphics);
                    }

                    VkPipelineCacheCreateInfo pipelineCacheCreateInfo = VkPipelineCacheCreateInfo.New();
                    VulkanUtil.CheckResult(vkCreatePipelineCache(device, ref pipelineCacheCreateInfo, null, out pipelineCache));
                    return result;
                }
            }

        }

        private static DebugReportCallbackExt CreateDebugReportCallback()
        {
            // Attach debug callback.
            var debugReportCreateInfo = new DebugReportCallbackCreateInfoExt(
                //VkDebugReportFlagsEXT.InformationEXT |
                VkDebugReportFlagsEXT.WarningEXT |
                VkDebugReportFlagsEXT.PerformanceWarningEXT |
                VkDebugReportFlagsEXT.ErrorEXT |
                VkDebugReportFlagsEXT.DebugEXT,
                (args) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[{args.Flags}][{args.LayerPrefix}]");
                    System.Diagnostics.Debug.WriteLine("\t" + args.Message);
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

        public static void WaitIdle()
        {
            VulkanUtil.CheckResult(vkDeviceWaitIdle(device));
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

        public static bool IsDepthFormat(Format format)
        {
            switch (format)
            {
                case Format.D32SfloatS8Uint:
                case Format.D32Sfloat:
                case Format.D24UnormS8Uint:
                case Format.D16UnormS8Uint:
                case Format.D16Unorm:
                    return true;
            }

            return false;
        }

        public static void GetPhysicalDeviceFormatProperties(VkFormat format, out VkFormatProperties pFeatures)
        {
            vkGetPhysicalDeviceFormatProperties(PhysicalDevice, format, out pFeatures);
        }

        public delegate VkResult vkCmdPushDescriptorSetKHRDelegate(VkCommandBuffer commandBuffer, VkPipelineBindPoint pipelineBindPoint, VkPipelineLayout layout, uint set, uint descriptorWriteCount, VkWriteDescriptorSet* pDescriptorWrites);

        public static vkCmdPushDescriptorSetKHRDelegate CmdPushDescriptorSetKHR;
        private static void vkCmdPushDescriptorSetKHR()
        {
            CmdPushDescriptorSetKHR = device.GetProc<vkCmdPushDescriptorSetKHRDelegate>(nameof(vkCmdPushDescriptorSetKHR));
        }

        public static VkSwapchainKHR CreateSwapchainKHR(ref VkSwapchainCreateInfoKHR pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateSwapchainKHR(device, ref pCreateInfo, null, out VkSwapchainKHR pSwapchain));
            return pSwapchain;
        }

        public static void DestroySwapchainKHR(VkSwapchainKHR swapchain)
        {
            vkDestroySwapchainKHR(device, swapchain, null);
        }

        public static void GetSwapchainImagesKHR(VkSwapchainKHR swapchain, uint* pSwapchainImageCount, VkImage* pSwapchainImages)
        {
            VulkanUtil.CheckResult(vkGetSwapchainImagesKHR(device, swapchain, pSwapchainImageCount, pSwapchainImages));
        }

        public static VkResult AcquireNextImageKHR(VkSwapchainKHR swapchain, ulong timeout, VkSemaphore semaphore, VkFence fence, ref uint pImageIndex)
        {
            return vkAcquireNextImageKHR(device, swapchain, timeout, semaphore, fence, ref pImageIndex);
        }

        public static VkSemaphore CreateSemaphore(uint flags = 0)
        {
            var semaphoreCreateInfo = VkSemaphoreCreateInfo.New();
            semaphoreCreateInfo.flags = flags;
            VulkanUtil.CheckResult(vkCreateSemaphore(device, ref semaphoreCreateInfo, null, out VkSemaphore pSemaphore));
            return pSemaphore;
        }

        public static void Destroy(VkSemaphore semaphore)
        {
            vkDestroySemaphore(device, semaphore, null);
        }

        public static VkEvent CreateEvent(ref VkEventCreateInfo pCreateInfo)
        {
            vkCreateEvent(device, ref pCreateInfo, null, out VkEvent pEvent);
            return pEvent;
        }

        public static VkResult GetEventStatus(VkEvent evt)
        {
            return vkGetEventStatus(device, evt);
        }

        public static void SetEvent(VkEvent evt)
        {
            VulkanUtil.CheckResult(vkSetEvent(device, evt));
        }

        public static void ResetEvent(VkEvent evt)
        {
            VulkanUtil.CheckResult(vkResetEvent(device, evt));
        }

        public static void Destroy(VkEvent @event)
        {
            vkDestroyEvent(device, @event, null);
        }

        public static VkFence CreateFence(ref VkFenceCreateInfo pCreateInfo)
        {
            vkCreateFence(device, ref pCreateInfo, null, out VkFence pFence);
            return pFence;
        }

        public static VkResult GetFenceStatus(VkFence fence)
        {
            return vkGetFenceStatus(device, fence);
        }

        public static void ResetFences(uint fenceCount, ref VkFence pFences)
        {
            VulkanUtil.CheckResult(vkResetFences(device, fenceCount, ref pFences));
        }

        public static void WaitForFences(uint fenceCount, ref VkFence pFences, VkBool32 waitAll, ulong timeout)
        {
            VulkanUtil.CheckResult(vkWaitForFences(device, fenceCount, ref pFences, waitAll, timeout));
        }

        public static void Destroy(VkFence fence)
        {
            vkDestroyFence(device, fence, null);
        }

        public static VkQueryPool CreateQueryPool(ref VkQueryPoolCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateQueryPool(device, ref pCreateInfo, null, out VkQueryPool pQueryPool));
            return pQueryPool;
        }

        public static void GetQueryPoolResults(VkQueryPool queryPool, uint firstQuery, uint queryCount, UIntPtr dataSize, void* pData, ulong stride, VkQueryResultFlags flags)
        {
            VulkanUtil.CheckResult(vkGetQueryPoolResults(device, queryPool, firstQuery, queryCount, dataSize, pData, stride, flags));
        }

        public static void DestroyQueryPool(ref VkQueryPool queryPool)
        {
            vkDestroyQueryPool(device, queryPool, null);
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

        public static VkBufferView CreateBufferView(ref VkBufferViewCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateBufferView(device, ref pCreateInfo, null, out VkBufferView pView));
            return pView;
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

        public static bool MemoryTypeNeedsStaging(uint memoryTypeIndex)
        {
            VkMemoryPropertyFlags flags = MemoryProperties.GetMemoryType(memoryTypeIndex).propertyFlags;
            return (flags & VkMemoryPropertyFlags.HostVisible) == 0;
        }

        public static void FlushMappedMemoryRanges(uint memoryRangeCount, ref VkMappedMemoryRange pMemoryRanges)
        {
            VulkanUtil.CheckResult(vkFlushMappedMemoryRanges(device, memoryRangeCount, ref pMemoryRanges));
        }

        public static void InvalidateMappedMemoryRanges(uint memoryRangeCount, ref VkMappedMemoryRange pMemoryRanges)
        {
            VulkanUtil.CheckResult(vkInvalidateMappedMemoryRanges(device, memoryRangeCount, ref pMemoryRanges));
        }

        public static void GetImageMemoryRequirements(VkImage image, out VkMemoryRequirements pMemoryRequirements)
        {
            vkGetImageMemoryRequirements(device, image, out pMemoryRequirements);
        }

        public static void BindImageMemory(VkImage image, VkDeviceMemory memory, ulong offset)
        {
            VulkanUtil.CheckResult(vkBindImageMemory(device, image, memory, offset));
        }

        public static VkCommandPool CreateCommandPool(uint queueFamilyIndex, VkCommandPoolCreateFlags createFlags = VkCommandPoolCreateFlags.ResetCommandBuffer)
        {
            VkCommandPoolCreateInfo cmdPoolInfo = VkCommandPoolCreateInfo.New();
            cmdPoolInfo.queueFamilyIndex = queueFamilyIndex;
            cmdPoolInfo.flags = createFlags;
            VulkanUtil.CheckResult(vkCreateCommandPool(device, &cmdPoolInfo, null, out VkCommandPool cmdPool));
            return cmdPool;
        }

        public static void AllocateCommandBuffers(VkCommandPool cmdPool, VkCommandBufferLevel level,
            uint count, VkCommandBuffer* cmdBuffers)
        {
            VkCommandBufferAllocateInfo cmdBufAllocateInfo = VkCommandBufferAllocateInfo.New();
            cmdBufAllocateInfo.commandPool = cmdPool;
            cmdBufAllocateInfo.level = level;
            cmdBufAllocateInfo.commandBufferCount = count;

            VulkanUtil.CheckResult(vkAllocateCommandBuffers(device, ref cmdBufAllocateInfo, cmdBuffers));
        }

        public static void ResetCommandPool(VkCommandPool cmdPool, VkCommandPoolResetFlags flags)
        {
            vkResetCommandPool(device, cmdPool, flags);
        }

        public static void FreeCommandBuffers(VkCommandPool cmdPool, uint count, VkCommandBuffer* cmdBuffers)
        {
            vkFreeCommandBuffers(device, cmdPool, count, cmdBuffers);
        }

        public static IntPtr MapMemory(VkDeviceMemory memory, ulong offset, ulong size, uint flags)
        {
            void* mappedLocal;
            vkMapMemory(device, memory, offset, size, flags, &mappedLocal);
            return (IntPtr)mappedLocal;
        }

        public static void UnmapMemory(VkDeviceMemory memory)
        {
            vkUnmapMemory(device, memory);
        }

        public static uint GetMemoryType(uint typeBits, MemoryPropertyFlags properties)
        {
            for (uint i = 0; i < MemoryProperties.memoryTypeCount; i++)
            {
                if ((typeBits & 1) == 1)
                {
                    if ((((MemoryPropertyFlags)MemoryProperties.GetMemoryType(i).propertyFlags) & properties) == properties)
                    {
                        return i;

                    }
                }
                typeBits >>= 1;
            }

            return 0;

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
            VulkanUtil.CheckResult(vkCreateGraphicsPipelines(device, pipelineCache, 1, ref pCreateInfos, IntPtr.Zero, out VkPipeline pPipelines));
            return pPipelines;
        }

        public static VkPipelineLayout CreatePipelineLayout(ref VkPipelineLayoutCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreatePipelineLayout(device, ref pCreateInfo, null, out VkPipelineLayout pPipelineLayout));
            return pPipelineLayout;
        }

        public static VkPipeline CreateComputePipeline(ref VkComputePipelineCreateInfo pCreateInfos)
        {
            VulkanUtil.CheckResult(vkCreateComputePipelines(device, pipelineCache, 1, ref pCreateInfos, IntPtr.Zero, out VkPipeline pPipelines));
            return pPipelines;
        }

        public static void DestroyPipeline(VkPipeline pipeline)
        {
            vkDestroyPipeline(device, pipeline, null);
        }

        public static void DestroyBuffer(VkBuffer buffer)
        {
            vkDestroyBuffer(device, buffer, null);
        }

        public static void DestroyBufferView(VkBufferView view)
        {
            vkDestroyBufferView(device, view, null);
        }

        public static void FreeMemory(VkDeviceMemory memory)
        {
            vkFreeMemory(device, memory, null);
        }

        public static VkDescriptorPool CreateDescriptorPool(ref VkDescriptorPoolCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateDescriptorPool(device, ref pCreateInfo, null, out VkDescriptorPool pDescriptorPool));
            return pDescriptorPool;
        }

        public static void DestroyDescriptorPool(VkDescriptorPool descriptorPool)
        {
            vkDestroyDescriptorPool(device, descriptorPool, IntPtr.Zero);
        }

        public static void DestroyPipelineLayout(VkPipelineLayout pipelineLayout)
        {
            vkDestroyPipelineLayout(device, pipelineLayout, null);
        }

        public static VkDescriptorSetLayout CreateDescriptorSetLayout(ref VkDescriptorSetLayoutCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateDescriptorSetLayout(device, ref pCreateInfo, null, out var setLayout));
            return setLayout;
        }

        public static void DestroyDescriptorSetLayout(VkDescriptorSetLayout descriptorSetLayout)
        {
            vkDestroyDescriptorSetLayout(device, descriptorSetLayout, null);
        }

        public static VkDescriptorSet AllocateDescriptorSets(ref VkDescriptorSetAllocateInfo pAllocateInfo)
        {
            VulkanUtil.CheckResult(vkAllocateDescriptorSets(device, ref pAllocateInfo, out VkDescriptorSet pDescriptorSets));
            return pDescriptorSets;
        }

        public static void AllocateDescriptorSets(ref VkDescriptorSetAllocateInfo pAllocateInfo, VkDescriptorSet* pDescriptorSets)
        {
            VulkanUtil.CheckResult(vkAllocateDescriptorSets(device, ref pAllocateInfo, pDescriptorSets));
        }

        public static void UpdateDescriptorSets(uint descriptorWriteCount, ref VkWriteDescriptorSet pDescriptorWrites, uint descriptorCopyCount, IntPtr pDescriptorCopies)
        {
            vkUpdateDescriptorSets(device, descriptorWriteCount, ref pDescriptorWrites, descriptorCopyCount, pDescriptorCopies);
        }

        public static void FreeDescriptorSets(VkDescriptorPool descriptorPool, uint descriptorSetCount, ref VkDescriptorSet pDescriptorSets)
        {
            VulkanUtil.CheckResult(vkFreeDescriptorSets(device, descriptorPool, descriptorSetCount, ref pDescriptorSets));
        }

    }

}
