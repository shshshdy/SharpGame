using System;
using VulkanCore;
using VulkanCore.Khr;

namespace SharpGame
{
    /// <summary>
    /// Encapsulates Vulkan <see cref="VulkanCore.PhysicalDevice"/> and <see cref="VulkanCore.Device"/> and exposes queues
    /// and a command pool for rendering tasks.
    /// </summary>
    public partial class Graphics
    {
        public void CreateContext(Instance instance, SurfaceKhr surface, PlatformType platform)
        {
            // Find graphics and presentation capable physical device(s) that support
            // the provided surface for platform.
            int graphicsQueueFamilyIndex = -1;
            int computeQueueFamilyIndex = -1;
            int presentQueueFamilyIndex = -1;
            foreach (PhysicalDevice physicalDevice in instance.EnumeratePhysicalDevices())
            {
                QueueFamilyProperties[] queueFamilyProperties = physicalDevice.GetQueueFamilyProperties();
                for (int i = 0; i < queueFamilyProperties.Length; i++)
                {
                    if (queueFamilyProperties[i].QueueFlags.HasFlag(Queues.Graphics))
                    {
                        if (graphicsQueueFamilyIndex == -1) graphicsQueueFamilyIndex = i;
                        if (computeQueueFamilyIndex == -1) computeQueueFamilyIndex = i;

                        if (physicalDevice.GetSurfaceSupportKhr(i, surface) &&
                            GetPresentationSupport(physicalDevice, i))
                        {
                            presentQueueFamilyIndex = i;
                        }

                        if (graphicsQueueFamilyIndex != -1 &&
                            computeQueueFamilyIndex != -1 &&
                            presentQueueFamilyIndex != -1)
                        {
                            PhysicalDevice = physicalDevice;
                            break;
                        }
                    }
                }
                if (PhysicalDevice != null) break;
            }

            bool GetPresentationSupport(PhysicalDevice physicalDevice, int queueFamilyIndex)
            {
                switch (platform)
                {
                    case PlatformType.Android:
                        return true;
                    case PlatformType.Win32:
                        return physicalDevice.GetWin32PresentationSupportKhr(queueFamilyIndex);
                    case PlatformType.MacOS:
                        return true;
                    default:
                        throw new NotImplementedException();
                }
            }

            if (PhysicalDevice == null)
                throw new InvalidOperationException("No suitable physical device found.");

            // Store memory properties of the physical device.
            MemoryProperties = PhysicalDevice.GetMemoryProperties();
            Features = PhysicalDevice.GetFeatures();
            Properties = PhysicalDevice.GetProperties();

            // Create a logical device.
            bool sameGraphicsAndPresent = graphicsQueueFamilyIndex == presentQueueFamilyIndex;
            var queueCreateInfos = new DeviceQueueCreateInfo[sameGraphicsAndPresent ? 1 : 2];
            queueCreateInfos[0] = new DeviceQueueCreateInfo(graphicsQueueFamilyIndex, 1, 1.0f);
            if (!sameGraphicsAndPresent)
                queueCreateInfos[1] = new DeviceQueueCreateInfo(presentQueueFamilyIndex, 1, 1.0f);

            var deviceCreateInfo = new DeviceCreateInfo(
                queueCreateInfos,
                new[] { Constant.DeviceExtension.KhrSwapchain },
                Features);
            Device = PhysicalDevice.CreateDevice(deviceCreateInfo);

            // Get queue(s).
            GraphicsQueue = Device.GetQueue(graphicsQueueFamilyIndex);
            ComputeQueue = computeQueueFamilyIndex == graphicsQueueFamilyIndex
                ? GraphicsQueue
                : Device.GetQueue(computeQueueFamilyIndex);
            PresentQueue = presentQueueFamilyIndex == graphicsQueueFamilyIndex
                ? GraphicsQueue
                : Device.GetQueue(presentQueueFamilyIndex);

            // Create command pool(s).
            GraphicsCommandPool = Device.CreateCommandPool(new CommandPoolCreateInfo(graphicsQueueFamilyIndex));
            ComputeCommandPool = Device.CreateCommandPool(new CommandPoolCreateInfo(computeQueueFamilyIndex));
        }

        public PhysicalDevice PhysicalDevice { get; private set; }
        public Device Device { get; private set; }
        public PhysicalDeviceMemoryProperties MemoryProperties { get; private set; }
        public PhysicalDeviceFeatures Features { get; private set; }
        public PhysicalDeviceProperties Properties { get; private set; }
        public Queue GraphicsQueue { get; private set; }
        public Queue ComputeQueue { get; private set; }
        public Queue PresentQueue { get; private set; }
        public CommandPool GraphicsCommandPool { get; private set; }
        public CommandPool ComputeCommandPool { get; private set; }

    }
}
