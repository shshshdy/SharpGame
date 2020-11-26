using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGame
{
    using static Vulkan;

    public unsafe class Device
    {
        public const ulong DEFAULT_FENCE_TIMEOUT = 100000000000;
        public static VkInstance VkInstance { get; private set; }
        public static VkDevice Handle => device;
        public static VkPhysicalDevice PhysicalDevice { get; private set; }
        public static VkPhysicalDeviceProperties Properties { get; private set; }
        public static VkPhysicalDeviceFeatures Features { get; private set; }
        public static VkPhysicalDeviceMemoryProperties MemoryProperties { get; private set; }
        public static Vector<VkQueueFamilyProperties> QueueFamilyProperties { get; } = new Vector<VkQueueFamilyProperties>();
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

        private static Vector<IntPtr> instanceExtensions = new Vector<IntPtr>(8);
        private static CStringList deviceExtensions = new CStringList();

        public static VkDevice Create(Settings settings, VkPhysicalDeviceFeatures enabledFeatures, Vector<IntPtr> enabledExtensions,
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
            vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, null);
            Debug.Assert(queueFamilyCount > 0);
            QueueFamilyProperties.Resize(queueFamilyCount);
            vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, QueueFamilyProperties.DataPtr);

            // Get list of supported extensions
            uint extCount = 0;
            vkEnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &extCount, null);
            if (extCount > 0)
            {
                VkExtensionProperties* extensions = stackalloc VkExtensionProperties[(int)extCount];
                if (vkEnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &extCount, extensions) == VkResult.Success)
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

            device = CreateLogicalDevice(Features, enabledExtensions);

            queue = GetDeviceQueue(QFGraphics, 0);

            if (device != VkDevice.Null)
            {
                // Create a default command pool for graphics command buffers
                commandPool = CreateCommandPool(QFGraphics);

                VkPipelineCacheCreateInfo pipelineCacheCreateInfo = new VkPipelineCacheCreateInfo()
                {
                    sType = VkStructureType.PipelineCacheCreateInfo
                };

                VulkanUtil.CheckResult(vkCreatePipelineCache(device, &pipelineCacheCreateInfo, null, out pipelineCache));

            }

            return device;
        }

        static VkInstance CreateInstance(Settings settings)
        {
            bool enableValidation = settings.Validation;

            vkInitialize().CheckResult();

            var appInfo = new VkApplicationInfo
            {
                sType = VkStructureType.ApplicationInfo,
                pApplicationName = settings.ApplicationName,
                applicationVersion = new VkVersion(1, 0, 0),
                pEngineName = engineName,
                engineVersion = new VkVersion(1, 0, 0),
                apiVersion = vkEnumerateInstanceVersion()
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

            VkInstanceCreateInfo instanceCreateInfo = new VkInstanceCreateInfo { sType = VkStructureType.InstanceCreateInfo };
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

            using Vector<IntPtr> enabledLayerNames = new Vector<IntPtr> { Strings.StandardValidationLayeName };

            if (enableValidation)
            {
                instanceCreateInfo.enabledLayerCount = enabledLayerNames.Count;
                instanceCreateInfo.ppEnabledLayerNames = (byte**)enabledLayerNames.Data;
            }

            VulkanUtil.CheckResult(vkCreateInstance(&instanceCreateInfo, null, out VkInstance instance));
            VkInstance = instance;

            vkLoadInstance(VkInstance);

            if (settings.Validation)
            {
                debugReportCallbackExt = CreateDebugReportCallback();
            }

            return instance;
        }

        static VkDevice CreateLogicalDevice(VkPhysicalDeviceFeatures enabledFeatures, Vector<IntPtr> enabledExtensions,
            bool useSwapChain = true, VkQueueFlags requestedQueueTypes = VkQueueFlags.Graphics | VkQueueFlags.Compute | VkQueueFlags.Transfer)
        {
            using Vector<VkDeviceQueueCreateInfo> queueCreateInfos = new Vector<VkDeviceQueueCreateInfo>();
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
                QFGraphics = (uint)IntPtr.Zero;
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
            using Vector<IntPtr> deviceExtensions = new Vector<IntPtr>(enabledExtensions);
            if (useSwapChain)
            {
                // If the device will be used for presenting to a display via a swapchain we need to request the swapchain extension
                deviceExtensions.Add(Strings.VK_KHR_SWAPCHAIN_EXTENSION_NAME);
            }

            VkDeviceCreateInfo deviceCreateInfo = new VkDeviceCreateInfo
            {
                sType = VkStructureType.DeviceCreateInfo,
                queueCreateInfoCount = queueCreateInfos.Count,
                pQueueCreateInfos = queueCreateInfos.DataPtr,
                pEnabledFeatures = &enabledFeatures
            };

            if (deviceExtensions.Count > 0)
            {
                deviceCreateInfo.enabledExtensionCount = deviceExtensions.Count;
                deviceCreateInfo.ppEnabledExtensionNames = (byte**)deviceExtensions.Data;
            }

            return Vulkan.CreateDevice(PhysicalDevice, &deviceCreateInfo);

        }

        private static DebugReportCallbackExt CreateDebugReportCallback()
        {
            return new DebugReportCallbackExt(VkInstance,
                //VkDebugReportFlagsEXT.Information | 
                //VkDebugReportFlagsEXT.Debug |
                VkDebugReportFlagsEXT.Warning |
                VkDebugReportFlagsEXT.PerformanceWarning |
                VkDebugReportFlagsEXT.Error,
                (args) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[{args.Flags}][{args.LayerPrefix}]");
                    System.Diagnostics.Debug.WriteLine("\t" + args.Message);

                    return args.Flags.HasFlag(VkDebugReportFlagsEXT.Error);
                });
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

        public static VkFormat GetSupportedDepthFormat()
        {
            // Since all depth formats may be optional, we need to find a suitable depth format to use
            // Start with the highest precision packed format
            List<VkFormat> depthFormats = new List<VkFormat>()
            {
                VkFormat.D32SFloatS8UInt,
                VkFormat.D32SFloat,
                VkFormat.D24UNormS8UInt,
                VkFormat.D16UNormS8UInt,
                VkFormat.D16UNorm,
            };

            foreach (VkFormat format in depthFormats)
            {
                VkFormatProperties formatProps;
                vkGetPhysicalDeviceFormatProperties(PhysicalDevice, format, out formatProps);
                // VkFormat must support depth stencil attachment for optimal tiling
                if ((formatProps.optimalTilingFeatures & VkFormatFeatureFlags.DepthStencilAttachment) != 0)
                {
                    return (VkFormat)format;
                }
            }

            return VkFormat.Undefined;
        }

        public static bool IsDepthFormat(VkFormat format)
        {
            switch (format)
            {
                case VkFormat.D32SFloatS8UInt:
                case VkFormat.D32SFloat:
                case VkFormat.D24UNormS8UInt:
                case VkFormat.D16UNormS8UInt:
                case VkFormat.D16UNorm:
                    return true;
            }

            return false;
        }

        public static void GetPhysicalDeviceFormatProperties(VkFormat format, out VkFormatProperties pFeatures)
        {
            vkGetPhysicalDeviceFormatProperties(PhysicalDevice, (VkFormat)format, out pFeatures);
        }

        public delegate VkResult vkCmdPushDescriptorSetKHRDelegate(VkCommandBuffer commandBuffer, VkPipelineBindPoint pipelineBindPoint, VkPipelineLayout layout, uint set, uint descriptorWriteCount, VkWriteDescriptorSet* pDescriptorWrites);

        public static vkCmdPushDescriptorSetKHRDelegate CmdPushDescriptorSetKHR;
        private static void vkCmdPushDescriptorSetKHR()
        {
            CmdPushDescriptorSetKHR = device.GetProc<vkCmdPushDescriptorSetKHRDelegate>(nameof(vkCmdPushDescriptorSetKHR));
        }

        public static VkSwapchainKHR CreateSwapchainKHR(ref VkSwapchainCreateInfoKHR pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateSwapchainKHR(device, Utilities.AsPtr(ref pCreateInfo), null, out VkSwapchainKHR pSwapchain));
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

        public static VkResult AcquireNextImageKHR(VkSwapchainKHR swapchain, ulong timeout, VkSemaphore semaphore, VkFence fence, out uint pImageIndex)
        {
            return vkAcquireNextImageKHR(device, swapchain, timeout, semaphore, fence, out pImageIndex);
        }

        public static VkSemaphore CreateSemaphore(VkSemaphoreCreateFlags flags = 0)
        {
            var semaphoreCreateInfo = new VkSemaphoreCreateInfo
            {
                sType = VkStructureType.SemaphoreCreateInfo
            };
            semaphoreCreateInfo.flags = flags;
            VulkanUtil.CheckResult(vkCreateSemaphore(device, &semaphoreCreateInfo, null, out VkSemaphore pSemaphore));
            return pSemaphore;
        }

        public static void Destroy(VkSemaphore semaphore)
        {
            vkDestroySemaphore(device, semaphore, null);
        }

        public static VkEvent CreateEvent(ref VkEventCreateInfo pCreateInfo)
        {
            vkCreateEvent(device, Utilities.AsPtr(ref pCreateInfo), null, out VkEvent pEvent);
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
            vkCreateFence(device, Utilities.AsPtr(ref pCreateInfo), null, out VkFence pFence);
            return pFence;
        }

        public static VkResult GetFenceStatus(VkFence fence)
        {
            return vkGetFenceStatus(device, fence);
        }

        public static void ResetFences(uint fenceCount, ref VkFence pFences)
        {
            VulkanUtil.CheckResult(vkResetFences(device, fenceCount, Utilities.AsPtr(ref pFences)));
        }

        public static void WaitForFences(uint fenceCount, ref VkFence pFences, VkBool32 waitAll, ulong timeout)
        {
            VulkanUtil.CheckResult(vkWaitForFences(device, fenceCount, Utilities.AsPtr(ref pFences), waitAll, timeout));
        }

        public static void Destroy(VkFence fence)
        {
            vkDestroyFence(device, fence, null);
        }

        public static VkQueryPool CreateQueryPool(ref VkQueryPoolCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateQueryPool(device, Utilities.AsPtr(ref pCreateInfo), null, out VkQueryPool pQueryPool));
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
            VulkanUtil.CheckResult(vkCreateImage(device, Utilities.AsPtr(ref pCreateInfo), null, out VkImage pImage));
            return pImage;
        }

        public static void Destroy(VkImage image)
        {
            vkDestroyImage(device, image, null);
        }

        public static VkImageView CreateImageView(ref VkImageViewCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateImageView(device, Utilities.AsPtr(ref pCreateInfo), null, out VkImageView pView));
            return pView;
        }

        public static void Destroy(VkImageView imageView)
        {
            vkDestroyImageView(device, imageView, null);
        }

        public static VkSampler CreateSampler(ref VkSamplerCreateInfo vkSamplerCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateSampler(device, Utilities.AsPtr(ref vkSamplerCreateInfo), null, out VkSampler vkSampler));
            return vkSampler;
        }

        public static void Destroy(VkSampler sampler)
        {
            vkDestroySampler(device, sampler, null);
        }

        public static VkFramebuffer CreateFramebuffer(ref VkFramebufferCreateInfo framebufferCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateFramebuffer(device, Utilities.AsPtr(ref framebufferCreateInfo), null, out VkFramebuffer framebuffer));
            return framebuffer;
        }

        public static void Destroy(VkFramebuffer framebuffer)
        {
            vkDestroyFramebuffer(device, framebuffer, null);
        }

        public static VkRenderPass CreateRenderPass(ref VkRenderPassCreateInfo createInfo)
        {
            VulkanUtil.CheckResult(vkCreateRenderPass(device, Utilities.AsPtr(ref createInfo), null, out VkRenderPass pRenderPass));
            return pRenderPass;
        }

        public static void Destroy(VkRenderPass renderPass)
        {
            vkDestroyRenderPass(device, renderPass, null);
        }

        public static VkBuffer CreateBuffer(ref VkBufferCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateBuffer(device, Utilities.AsPtr(ref pCreateInfo), null, out VkBuffer buffer));
            return buffer;
        }

        public static VkBufferView CreateBufferView(ref VkBufferViewCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateBufferView(device, Utilities.AsPtr(ref pCreateInfo), null, out VkBufferView pView));
            return pView;
        }

        public static void GetBufferMemoryRequirements(VkBuffer buffer, out VkMemoryRequirements pMemoryRequirements)
        {
            vkGetBufferMemoryRequirements(device, buffer, out pMemoryRequirements);
        }

        public static VkDeviceMemory AllocateMemory(ref VkMemoryAllocateInfo pAllocateInfo)
        {
            VkDeviceMemory pMemory;
            VulkanUtil.CheckResult(vkAllocateMemory(device, Utilities.AsPtr(ref pAllocateInfo), null, &pMemory));
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
            VulkanUtil.CheckResult(vkFlushMappedMemoryRanges(device, memoryRangeCount, Utilities.AsPtr(ref pMemoryRanges)));
        }

        public static void InvalidateMappedMemoryRanges(uint memoryRangeCount, ref VkMappedMemoryRange pMemoryRanges)
        {
            VulkanUtil.CheckResult(vkInvalidateMappedMemoryRanges(device, memoryRangeCount, Utilities.AsPtr(ref pMemoryRanges)));
        }

        public static void GetImageMemoryRequirements(Image image, out VkMemoryRequirements pMemoryRequirements)
        {
            vkGetImageMemoryRequirements(device, image.handle, out pMemoryRequirements);
        }

        public static void BindImageMemory(VkImage image, VkDeviceMemory memory, ulong offset)
        {
            VulkanUtil.CheckResult(vkBindImageMemory(device, image, memory, offset));
        }

        public static VkCommandPool CreateCommandPool(uint queueFamilyIndex, VkCommandPoolCreateFlags createFlags = VkCommandPoolCreateFlags.ResetCommandBuffer)
        {
            VkCommandPoolCreateInfo cmdPoolInfo = new VkCommandPoolCreateInfo
            {
                sType = VkStructureType.CommandPoolCreateInfo
            };
            cmdPoolInfo.queueFamilyIndex = queueFamilyIndex;
            cmdPoolInfo.flags = createFlags;
            VulkanUtil.CheckResult(vkCreateCommandPool(device, &cmdPoolInfo, null, out VkCommandPool cmdPool));
            return cmdPool;
        }

        public static void AllocateCommandBuffers(VkCommandPool cmdPool, VkCommandBufferLevel level,
            uint count, VkCommandBuffer* cmdBuffers)
        {
            VkCommandBufferAllocateInfo cmdBufAllocateInfo = new VkCommandBufferAllocateInfo
            {
                sType = VkStructureType.CommandBufferAllocateInfo
            };
            cmdBufAllocateInfo.commandPool = cmdPool;
            cmdBufAllocateInfo.level = level;
            cmdBufAllocateInfo.commandBufferCount = count;

            VulkanUtil.CheckResult(vkAllocateCommandBuffers(device, &cmdBufAllocateInfo, cmdBuffers));
        }

        public static void ResetCommandPool(VkCommandPool cmdPool, VkCommandPoolResetFlags flags)
        {
            vkResetCommandPool(device, cmdPool, flags);
        }

        public static void FreeCommandBuffers(VkCommandPool cmdPool, uint count, VkCommandBuffer* cmdBuffers)
        {
            vkFreeCommandBuffers(device, cmdPool, count, cmdBuffers);
        }

        public static IntPtr MapMemory(VkDeviceMemory memory, ulong offset, ulong size, VkMemoryMapFlags flags)
        {
            void* mappedLocal;
            vkMapMemory(device, memory, offset, size, flags, &mappedLocal);
            return (IntPtr)mappedLocal;
        }

        public static void UnmapMemory(VkDeviceMemory memory)
        {
            vkUnmapMemory(device, memory);
        }

        public static uint GetMemoryType(uint typeBits, VkMemoryPropertyFlags properties)
        {
            for (uint i = 0; i < MemoryProperties.memoryTypeCount; i++)
            {
                if ((typeBits & 1) == 1)
                {
                    if ((((VkMemoryPropertyFlags)MemoryProperties.GetMemoryType(i).propertyFlags) & properties) == properties)
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
            VulkanUtil.CheckResult(vkCreateShaderModule(device, Utilities.AsPtr(ref shaderModuleCreateInfo), null, out VkShaderModule shaderModule));
            return shaderModule;
        }

        public static void Destroy(VkShaderModule shaderModule)
        {
            vkDestroyShaderModule(device, shaderModule, null);
        }

        public static VkPipeline CreateGraphicsPipeline(ref VkGraphicsPipelineCreateInfo pCreateInfos)
        {
            VkPipeline pPipelines;
            VulkanUtil.CheckResult(vkCreateGraphicsPipelines(device, pipelineCache, 1, Utilities.AsPtr(ref pCreateInfos), null, &pPipelines));
            return pPipelines;
        }

        public static VkPipelineLayout CreatePipelineLayout(ref VkPipelineLayoutCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreatePipelineLayout(device, Utilities.AsPtr(ref pCreateInfo), null, out VkPipelineLayout pPipelineLayout));
            return pPipelineLayout;
        }

        public static VkPipeline CreateComputePipeline(ref VkComputePipelineCreateInfo pCreateInfos)
        {
            VkPipeline pPipelines;
            VulkanUtil.CheckResult(vkCreateComputePipelines(device, pipelineCache, 1, Utilities.AsPtr(ref pCreateInfos), null, &pPipelines));
            return pPipelines;
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
            VulkanUtil.CheckResult(vkCreateDescriptorPool(device, Utilities.AsPtr(ref pCreateInfo), null, out VkDescriptorPool pDescriptorPool));
            return pDescriptorPool;
        }

        public static void DestroyDescriptorPool(VkDescriptorPool descriptorPool)
        {
            vkDestroyDescriptorPool(device, descriptorPool, null);
        }

        public static void DestroyPipelineLayout(VkPipelineLayout pipelineLayout)
        {
            vkDestroyPipelineLayout(device, pipelineLayout, null);
        }

        public static VkDescriptorSetLayout CreateDescriptorSetLayout(ref VkDescriptorSetLayoutCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateDescriptorSetLayout(device, Utilities.AsPtr(ref pCreateInfo), null, out var setLayout));
            return setLayout;
        }

        public static void DestroyDescriptorSetLayout(VkDescriptorSetLayout descriptorSetLayout)
        {
            vkDestroyDescriptorSetLayout(device, descriptorSetLayout, null);
        }

        public static VkDescriptorSet AllocateDescriptorSets(ref VkDescriptorSetAllocateInfo pAllocateInfo)
        {
            VkDescriptorSet pDescriptorSets;
            VulkanUtil.CheckResult(vkAllocateDescriptorSets(device, Utilities.AsPtr(ref pAllocateInfo), &pDescriptorSets));
            return pDescriptorSets;
        }

        public static void AllocateDescriptorSets(ref VkDescriptorSetAllocateInfo pAllocateInfo, VkDescriptorSet* pDescriptorSets)
        {
            VulkanUtil.CheckResult(vkAllocateDescriptorSets(device, Utilities.AsPtr(ref pAllocateInfo), pDescriptorSets));
        }

        public static void UpdateDescriptorSets(uint descriptorWriteCount, ref VkWriteDescriptorSet pDescriptorWrites, uint descriptorCopyCount, IntPtr pDescriptorCopies)
        {
            vkUpdateDescriptorSets(device, descriptorWriteCount, Utilities.AsPtr(ref pDescriptorWrites), descriptorCopyCount, (VkCopyDescriptorSet*)pDescriptorCopies);
        }

        public static void FreeDescriptorSets(VkDescriptorPool descriptorPool, uint descriptorSetCount, ref VkDescriptorSet pDescriptorSets)
        {
            VulkanUtil.CheckResult(vkFreeDescriptorSets(device, descriptorPool, descriptorSetCount, Utilities.AsPtr(ref pDescriptorSets)));
        }

    }

}
