using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VulkanCore;
using VulkanCore.Ext;
using VulkanCore.Khr;
using VulkanCore.Mvk;

namespace SharpGame
{

    public partial class Graphics : IDisposable
    {
        private readonly Stack<IDisposable> _toDisposePermanent = new Stack<IDisposable>();
        private readonly Stack<IDisposable> _toDisposeFrame = new Stack<IDisposable>();

        private bool _initializingPermanent;

        public IPlatform Host { get; private set; }

        public Instance Instance { get; private set; }
        protected DebugReportCallbackExt DebugReportCallback { get; private set; }

        public SurfaceKhr Surface { get; private set; }
        public SwapchainKhr Swapchain { get; private set; }
        public Image[] SwapchainImages { get; private set; }
        public CommandBuffer[] CommandBuffers { get; private set; }
        public Fence[] SubmitFences { get; private set; }

        public Semaphore ImageAvailableSemaphore { get; private set; }
        public Semaphore RenderingFinishedSemaphore { get; private set; }

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

        public void Initialize(IPlatform host)
        {
            Host = host;
#if DEBUG
            const bool debug = true;
#else
            const bool debug = false;
#endif
            _initializingPermanent = true;
            // Calling ToDispose here registers the resource to be automatically disposed on exit.
            Instance = CreateInstance(debug);
            DebugReportCallback = CreateDebugReportCallback(debug);
            Surface = CreateSurface();

            Create(Instance, Surface, host.Platform);

            ImageAvailableSemaphore = ToDispose(Device.CreateSemaphore());
            RenderingFinishedSemaphore = ToDispose(Device.CreateSemaphore());

            if (host.Platform == Platform.MacOS)
            {
                //Setup MoltenVK specific device configuration.
                MVKDeviceConfiguration deviceConfig = Device.GetMVKDeviceConfiguration();
                deviceConfig.DebugMode = debug;
                deviceConfig.PerformanceTracking = debug;
                deviceConfig.PerformanceLoggingFrameCount = debug ? 300 : 0;
                Device.SetMVKDeviceConfiguration(deviceConfig);
            }

            _initializingPermanent = false;
            // Calling ToDispose here registers the resource to be automatically disposed on events
            // such as window resize.
            Swapchain = ToDisposeFrame(CreateSwapchain());
            // Acquire underlying images of the freshly created swapchain.
            SwapchainImages = Swapchain.GetImages();
            // Create a command buffer for each swapchain image.
            CommandBuffers = GraphicsCommandPool.AllocateBuffers(
                new CommandBufferAllocateInfo(CommandBufferLevel.Primary, SwapchainImages.Length));
            // Create a fence for each commandbuffer so that we can wait before using it again
            _initializingPermanent = true; //We need our fences to be there permanently
            SubmitFences = new Fence[SwapchainImages.Length];
            for (int i = 0; i < SubmitFences.Length; i++)
                ToDispose(SubmitFences[i] = Device.CreateFence(new FenceCreateInfo(FenceCreateFlags.Signaled)));

        }

        void Create(Instance instance, SurfaceKhr surface, Platform platform)
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
                    case Platform.Android:
                        return true;
                    case Platform.Win32:
                        return physicalDevice.GetWin32PresentationSupportKhr(queueFamilyIndex);
                    case Platform.MacOS:
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

        public void Resize()
        {
            // Dispose all frame dependent resources.
            while (_toDisposeFrame.Count > 0)
                _toDisposeFrame.Pop().Dispose();

            // Reset all the command buffers allocated from the pools.
            GraphicsCommandPool.Reset();
            ComputeCommandPool.Reset();

            Device.WaitIdle();

            // Reinitialize frame dependent resources.
            Swapchain = ToDispose(CreateSwapchain());
            SwapchainImages = Swapchain.GetImages();

        }


        public virtual void Draw(Timer timer)
        {
            // Acquire an index of drawing image for this frame.
            int imageIndex = Swapchain.AcquireNextImage(semaphore: ImageAvailableSemaphore);

            // Use a fence to wait until the command buffer has finished execution before using it again
            SubmitFences[imageIndex].Wait();
            SubmitFences[imageIndex].Reset();

            // Submit recorded commands to graphics queue for execution.
            GraphicsQueue.Submit(
                ImageAvailableSemaphore,
                PipelineStages.ColorAttachmentOutput,
                CommandBuffers[imageIndex],
                RenderingFinishedSemaphore,
                SubmitFences[imageIndex]
            );

            // Present the color output to screen.
            PresentQueue.PresentKhr(RenderingFinishedSemaphore, Swapchain, imageIndex);
        }

        private Instance CreateInstance(bool debug)
        {
            // Specify standard validation layers.
            string surfaceExtension;
            switch (Host.Platform)
            {
                case Platform.Android:
                    surfaceExtension = Constant.InstanceExtension.KhrAndroidSurface;
                    break;
                case Platform.Win32:
                    surfaceExtension = Constant.InstanceExtension.KhrWin32Surface;
                    break;
                case Platform.MacOS:
                    surfaceExtension = Constant.InstanceExtension.MvkMacOSSurface;
                    break;
                default:
                    throw new NotImplementedException();
            }

            var createInfo = new InstanceCreateInfo();

            //Currently MoltenVK (used for MacOS) doesn't support the debug layer.
            if (debug && Host.Platform != Platform.MacOS)
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
            return new Instance(createInfo);
        }

        private DebugReportCallbackExt CreateDebugReportCallback(bool debug)
        {
            //Currently MoltenVK (used for MacOS) doesn't support the debug layer.
            if (!debug || Host.Platform == Platform.MacOS) return null;

            // Attach debug callback.
            var debugReportCreateInfo = new DebugReportCallbackCreateInfoExt(
                DebugReportFlagsExt.All,
                args =>
                {
                    Debug.WriteLine($"[{args.Flags}][{args.LayerPrefix}] {args.Message}");
                    return args.Flags.HasFlag(DebugReportFlagsExt.Error);
                }
            );
            return Instance.CreateDebugReportCallbackExt(debugReportCreateInfo);
        }

        private SurfaceKhr CreateSurface()
        {
            // Create surface.
            switch (Host.Platform)
            {
                case Platform.Android:
                    return Instance.CreateAndroidSurfaceKhr(new AndroidSurfaceCreateInfoKhr(Host.WindowHandle));
                case Platform.Win32:
                    return Instance.CreateWin32SurfaceKhr(new Win32SurfaceCreateInfoKhr(Host.InstanceHandle, Host.WindowHandle));
                case Platform.MacOS:
                    return Instance.CreateMacOSSurfaceMvk(new MacOSSurfaceCreateInfoMvk(Host.WindowHandle));
                default:
                    throw new NotImplementedException();
            }
        }

        private SwapchainKhr CreateSwapchain()
        {
            SurfaceCapabilitiesKhr capabilities = PhysicalDevice.GetSurfaceCapabilitiesKhr(Surface);
            SurfaceFormatKhr[] formats = PhysicalDevice.GetSurfaceFormatsKhr(Surface);
            PresentModeKhr[] presentModes = PhysicalDevice.GetSurfacePresentModesKhr(Surface);
            Format format = formats.Length == 1 && formats[0].Format == Format.Undefined
                ? Format.B8G8R8A8UNorm
                : formats[0].Format;
            PresentModeKhr presentMode =
                presentModes.Contains(PresentModeKhr.Mailbox) ? PresentModeKhr.Mailbox :
                presentModes.Contains(PresentModeKhr.FifoRelaxed) ? PresentModeKhr.FifoRelaxed :
                presentModes.Contains(PresentModeKhr.Fifo) ? PresentModeKhr.Fifo :
                PresentModeKhr.Immediate;

            return Device.CreateSwapchainKhr(new SwapchainCreateInfoKhr(
                surface: Surface,
                imageFormat: format,
                imageExtent: capabilities.CurrentExtent,
                preTransform: capabilities.CurrentTransform,
                presentMode: presentMode));
        }

        public T ToDispose<T>(T disposable)
        {
            var toDispose = _initializingPermanent ? _toDisposePermanent : _toDisposeFrame;
            switch (disposable)
            {
                case IEnumerable<IDisposable> sequence:
                    foreach (var element in sequence)
                        toDispose.Push(element);
                    break;
                case IDisposable element:
                    toDispose.Push(element);
                    break;
            }
            return disposable;
        }

        public T ToDisposeFrame<T>(T disposable)
        {
            var toDispose = _toDisposeFrame;
            switch (disposable)
            {
                case IEnumerable<IDisposable> sequence:
                    foreach (var element in sequence)
                        toDispose.Push(element);
                    break;
                case IDisposable element:
                    toDispose.Push(element);
                    break;
            }
            return disposable;
        }


        public virtual void Dispose()
        {
            Device.WaitIdle();

            while (_toDisposeFrame.Count > 0)
                _toDisposeFrame.Pop().Dispose();

            while (_toDisposePermanent.Count > 0)
                _toDisposePermanent.Pop().Dispose();

            ComputeCommandPool.Dispose();
            GraphicsCommandPool.Dispose();
            Device.Dispose();

            DebugReportCallback.Dispose();
            Surface.Dispose();

            Instance.Dispose();
        }

    }
}
