using System;
using System.Diagnostics;
using System.Linq;
using Vulkan;

namespace SharpGame
{
    public class CommandBufferPool
    {
        VkCommandBuffer[] commandBuffers_;
        int currentIndex_;
        public CommandBufferPool(VkCommandBuffer[] commandBuffers)
        {
            commandBuffers_ = commandBuffers;
        }

        public VkCommandBuffer Get()
        {
            int idx = currentIndex_++;
            return commandBuffers_[idx % commandBuffers_.Length];
        }
    }

    public partial class Graphics
    {
        internal static VkInstance Instance { get; private set; }
        protected static VkDebugReportCallbackEXT DebugReportCallback { get; private set; }
        internal static VkSurfaceKHR Surface { get; private set; }
        internal static VkSwapchainKHR Swapchain { get; private set; }

        internal static VkPhysicalDevice PhysicalDevice { get; private set; }
        internal static VkDevice Device { get; private set; }
        internal static VkPhysicalDeviceMemoryProperties MemoryProperties { get; private set; }
        internal static VkPhysicalDeviceFeatures Features { get; private set; }
        internal static VkPhysicalDeviceProperties Properties { get; private set; }

        public VkQueue GraphicsQueue { get; private set; }
        public VkQueue ComputeQueue { get; private set; }
        public VkQueue PresentQueue { get; private set; }
        public VkCommandPool GraphicsCommandPool { get; private set; }
        public VkCommandPool ComputeCommandPool { get; private set; }
        public VkCommandPool[] SecondaryCommandPool { get; private set; }

        private VkInstance CreateInstance(bool debug)
        {
            // Specify standard validation layers.
            string surfaceExtension;
            switch (GameWindow.Platform)
            {
                case PlatformType.Android:
                    surfaceExtension = Constant.InstanceExtension.KhrAndroidSurface;
                    break;
                case PlatformType.Win32:
                    surfaceExtension = Constant.InstanceExtension.KhrWin32Surface;
                    break;
                case PlatformType.MacOS:
                    surfaceExtension = Constant.InstanceExtension.MvkMacOSSurface;
                    break;
                default:
                    throw new NotImplementedException();
            }

            var createInfo = new VkInstanceCreateInfo();

            //Currently MoltenVK (used for MacOS) doesn't support the debug layer.
            if (debug && GameWindow.Platform != PlatformType.MacOS)
            {
                var availableLayers = Instance.EnumerateLayerProperties();
                createInfo.EnabledLayerNames = new[] { Constant.InstanceLayer.LunarGStandardValidation }
                    .Where(availableLayers.Contains)
                    .ToArray();
                createInfo.EnabledExtensionNames = new[]
                {
                    Constant.InstanceExtension.KhrSurface,
                    surfaceExtension,
                    Constant.InstanceExtension.ExtDebugReport
                };
            }
            else
            {
                createInfo.EnabledExtensionNames = new[]
                {
                    Constant.InstanceExtension.KhrSurface,
                    surfaceExtension,
                };
            }
            return new VkInstance(createInfo);
        }

        private VkDebugReportCallbackEXT CreateDebugReportCallback(bool debug)
        {
            //Currently MoltenVK (used for MacOS) doesn't support the debug layer.
            if (!debug || GameWindow.Platform == PlatformType.MacOS) return 0;
            VkDebugReportCallbackCreateInfoEXT
            // Attach debug callback.
            var debugReportCreateInfo = new DebugReportCallbackCreateInfoExt(
                DebugReportFlagsExt.All,
                args =>
                {
                    Debug.WriteLine($"[{args.Flags}][{args.LayerPrefix}] {args.Message}");
                    return args.Flags.HasFlag(DebugReportFlagsExt.Error);
                }
            );

            vkCreateDebugReportCallbackEXT(Instance, )
            return Instance.CreateDebugReportCallbackExt(debugReportCreateInfo);
        }

        private VkSurfaceKHR CreateSurface()
        {
            // Create surface.
            switch (GameWindow.Platform)
            {
                case PlatformType.Android:
                    return Instance.CreateAndroidSurfaceKhr(new AndroidSurfaceCreateInfoKhr(GameWindow.WindowHandle));
                case PlatformType.Win32:
                    return Instance.CreateWin32SurfaceKhr(new Win32SurfaceCreateInfoKhr(GameWindow.InstanceHandle, GameWindow.WindowHandle));
                case PlatformType.MacOS:
                    return Instance.CreateMacOSSurfaceMvk(new MacOSSurfaceCreateInfoMvk(GameWindow.WindowHandle));
                default:
                    throw new NotImplementedException();
            }
        }


        public void CreateDevice(VkInstance instance, SurfaceKhr surface, PlatformType platform)
        {
            // Find graphics and presentation capable physical device(s) that support
            // the provided surface for platform.
            int graphicsQueueFamilyIndex = -1;
            int computeQueueFamilyIndex = -1;
            int presentQueueFamilyIndex = -1;
            foreach (VkPhysicalDevice physicalDevice in instance.EnumeratePhysicalDevices())
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


            GraphicsQueue = Device.GetQueue(graphicsQueueFamilyIndex);
            ComputeQueue = computeQueueFamilyIndex == graphicsQueueFamilyIndex
                ? GraphicsQueue
                : Device.GetQueue(computeQueueFamilyIndex);
            PresentQueue = presentQueueFamilyIndex == graphicsQueueFamilyIndex
                ? GraphicsQueue
                : Device.GetQueue(presentQueueFamilyIndex);

            // Create command pool(s).
            GraphicsCommandPool = Device.CreateCommandPool(new CommandPoolCreateInfo(graphicsQueueFamilyIndex, CommandPoolCreateFlags.ResetCommandBuffer | CommandPoolCreateFlags.Transient));
            ComputeCommandPool = Device.CreateCommandPool(new CommandPoolCreateInfo(computeQueueFamilyIndex, CommandPoolCreateFlags.ResetCommandBuffer | CommandPoolCreateFlags.Transient));
            SecondaryCommandPool = new CommandPool[2];
            SecondaryCommandPool[0] = Device.CreateCommandPool(new CommandPoolCreateInfo(graphicsQueueFamilyIndex, CommandPoolCreateFlags.ResetCommandBuffer | CommandPoolCreateFlags.Transient));
            SecondaryCommandPool[1] = Device.CreateCommandPool(new CommandPoolCreateInfo(graphicsQueueFamilyIndex, CommandPoolCreateFlags.ResetCommandBuffer | CommandPoolCreateFlags.Transient));

        }

    }
}
