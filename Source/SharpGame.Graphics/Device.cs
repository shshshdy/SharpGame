﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGame
{
    using static Vulkan;

    public unsafe static class Device
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
        private static VkPipelineCache pipelineCache;
        private static DebugReportCallbackExt debugReportCallbackExt;
        private static UTF8String engineName = "SharpGame";
        private static List<string> supportedExtensions = new List<string>();

        private static CStringList instanceExtensions = new CStringList();
        private static CStringList deviceExtensions = new CStringList();

        public static VkDevice Create(Settings settings, VkPhysicalDeviceFeatures enabledFeatures, CStringList enabledExtensions,
            VkQueueFlags requestedQueueTypes = VkQueueFlags.Graphics | VkQueueFlags.Compute | VkQueueFlags.Transfer)
        {
            instanceExtensions.Add(Vulkan.KHRSurfaceExtensionName);
            instanceExtensions.Add(Vulkan.KHRGetPhysicalDeviceProperties2ExtensionName);

            enabledExtensions.Add(Vulkan.KHRMaintenance1ExtensionName);
            enabledExtensions.Add(Vulkan.EXTInlineUniformBlockExtensionName);

            //enabledExtensions.Add(Strings.VK_DESCRIPTOR_BINDING_PARTIALLY_BOUND_BIT_EXT);

            CreateInstance(settings);

            // Physical Device
            var physicalDevices = Vulkan.vkEnumeratePhysicalDevices(VkInstance);

            // TODO: Implement arg parsing, etc.
            int selectedDevice = 0;

            PhysicalDevice = physicalDevices[selectedDevice];
            Debug.Assert(PhysicalDevice.Handle != IntPtr.Zero);
           
            vkGetPhysicalDeviceProperties(PhysicalDevice, out VkPhysicalDeviceProperties properties);
            Properties = properties;

            vkGetPhysicalDeviceFeatures(PhysicalDevice, out VkPhysicalDeviceFeatures features);
            Features = features;

            if (features.tessellationShader)
            {
                enabledFeatures.tessellationShader = true;
            }
               
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
            vkGetPhysicalDeviceMemoryProperties(PhysicalDevice, out memoryProperties);
            MemoryProperties = memoryProperties;
            
            var qf = Vulkan.vkGetPhysicalDeviceQueueFamilyProperties(PhysicalDevice);
            QueueFamilyProperties.Add(qf);

            var extensions = Vulkan.vkEnumerateDeviceExtensionProperties(PhysicalDevice);
            
            foreach(var ext in extensions)
            {
                string strExt = UTF8String.FromPointer(ext.extensionName);
                //enabledExtensions.Add((IntPtr)ext.extensionName);
                supportedExtensions.Add(strExt);
            }
              
            device = CreateLogicalDevice(Features, enabledExtensions, true, requestedQueueTypes);

            if (device != VkDevice.Null)
            {
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
                instanceExtensions.Add("VK_KHR_win32_surface");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                instanceExtensions.Add("VK_KHR_xlib_surface");
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
                    instanceExtensions.Add(Vulkan.EXTDebugReportExtensionName);
                }
                instanceCreateInfo.enabledExtensionCount = instanceExtensions.Count;
                instanceCreateInfo.ppEnabledExtensionNames = (byte**)instanceExtensions.Data;
            }

            using CStringList enabledLayerNames = new CStringList { "VK_LAYER_KHRONOS_validation" };

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

        static VkDevice CreateLogicalDevice(VkPhysicalDeviceFeatures enabledFeatures, CStringList enabledExtensions,
            bool useSwapChain = true, VkQueueFlags requestedQueueTypes = VkQueueFlags.Graphics | VkQueueFlags.Compute | VkQueueFlags.Transfer)
        {
            using Vector<VkDeviceQueueCreateInfo> queueCreateInfos = new Vector<VkDeviceQueueCreateInfo>();
            float defaultQueuePriority = 0.0f;

            // Graphics queue
            if ((requestedQueueTypes & VkQueueFlags.Graphics) != 0)
            {
                QFGraphics = GetQueueFamilyIndex(VkQueueFlags.Graphics);
                var queueInfo = new VkDeviceQueueCreateInfo
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
                    var queueInfo = new VkDeviceQueueCreateInfo
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
                    var queueInfo = new VkDeviceQueueCreateInfo
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
            using CStringList deviceExtensions = new CStringList(enabledExtensions);
            if (useSwapChain)
            {
                // If the device will be used for presenting to a display via a swapchain we need to request the swapchain extension
                deviceExtensions.Add(Vulkan.KHRSwapchainExtensionName);
            }

            var deviceCreateInfo = new VkDeviceCreateInfo
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
                //VkDebugReportFlagsEXT.PerformanceWarning |
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
                vkGetPhysicalDeviceFormatProperties(PhysicalDevice, format, out VkFormatProperties formatProps);
                // VkFormat must support depth stencil attachment for optimal tiling
                if ((formatProps.optimalTilingFeatures & VkFormatFeatureFlags.DepthStencilAttachment) != 0)
                {
                    return format;
                }
            }

            return VkFormat.Undefined;
        }

        public static bool IsDepthFormat(this VkFormat format)
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
            vkGetPhysicalDeviceFormatProperties(PhysicalDevice, format, out pFeatures);
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

        public static VkDeviceMemory AllocateMemory(ref VkMemoryAllocateInfo pAllocateInfo)
        {
            VkDeviceMemory pMemory;
            VulkanUtil.CheckResult(vkAllocateMemory(device, Utilities.AsPtr(ref pAllocateInfo), null, &pMemory));
            return pMemory;
        }

        public static void FreeMemory(VkDeviceMemory memory)
        {
            vkFreeMemory(device, memory, null);
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
                    if ((MemoryProperties.GetMemoryType(i).propertyFlags & properties) == properties)
                    {
                        return i;

                    }
                }
                typeBits >>= 1;
            }

            return 0;

        }

        public static void FlushMappedMemoryRanges(uint memoryRangeCount, ref VkMappedMemoryRange pMemoryRanges)
        {
            VulkanUtil.CheckResult(vkFlushMappedMemoryRanges(device, memoryRangeCount, Utilities.AsPtr(ref pMemoryRanges)));
        }

        public static void InvalidateMappedMemoryRanges(uint memoryRangeCount, ref VkMappedMemoryRange pMemoryRanges)
        {
            VulkanUtil.CheckResult(vkInvalidateMappedMemoryRanges(device, memoryRangeCount, Utilities.AsPtr(ref pMemoryRanges)));
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

        public static void BindBufferMemory(VkBuffer buffer, VkDeviceMemory memory, ulong memoryOffset)
        {
            VulkanUtil.CheckResult(vkBindBufferMemory(device, buffer, memory, memoryOffset));
        }

        public static VkImage CreateImage(ref VkImageCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateImage(device, Utilities.AsPtr(ref pCreateInfo), null, out VkImage pImage));
            return pImage;
        }

        public static VkImageView CreateImageView(ref VkImageViewCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateImageView(device, Utilities.AsPtr(ref pCreateInfo), null, out VkImageView pView));
            return pView;
        }

        public static void GetImageMemoryRequirements(Image image, out VkMemoryRequirements pMemoryRequirements)
        {
            vkGetImageMemoryRequirements(device, image, out pMemoryRequirements);
        }

        public static void BindImageMemory(VkImage image, VkDeviceMemory memory, ulong offset)
        {
            VulkanUtil.CheckResult(vkBindImageMemory(device, image, memory, offset));
        }

        public static VkSampler CreateSampler(ref VkSamplerCreateInfo vkSamplerCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateSampler(device, Utilities.AsPtr(ref vkSamplerCreateInfo), null, out VkSampler vkSampler));
            return vkSampler;
        }

        public static VkFramebuffer CreateFramebuffer(ref VkFramebufferCreateInfo framebufferCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateFramebuffer(device, Utilities.AsPtr(ref framebufferCreateInfo), null, out VkFramebuffer framebuffer));
            return framebuffer;
        }

        public static VkRenderPass CreateRenderPass(ref VkRenderPassCreateInfo createInfo)
        {
            VulkanUtil.CheckResult(vkCreateRenderPass(device, Utilities.AsPtr(ref createInfo), null, out VkRenderPass pRenderPass));
            return pRenderPass;
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

        public static void AllocateCommandBuffers(VkCommandPool cmdPool, VkCommandBufferLevel level, uint count, VkCommandBuffer* cmdBuffers)
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

        public static VkShaderModule CreateShaderModule(ref VkShaderModuleCreateInfo shaderModuleCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateShaderModule(device, Utilities.AsPtr(ref shaderModuleCreateInfo), null, out VkShaderModule shaderModule));
            return shaderModule;
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

        public static VkDescriptorPool CreateDescriptorPool(ref VkDescriptorPoolCreateInfo pCreateInfo)
        {
            VulkanUtil.CheckResult(vkCreateDescriptorPool(device, Utilities.AsPtr(ref pCreateInfo), null, out VkDescriptorPool pDescriptorPool));
            return pDescriptorPool;
        }

        public static void DestroyDescriptorPool(VkDescriptorPool descriptorPool)
        {
            vkDestroyDescriptorPool(device, descriptorPool, null);
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
