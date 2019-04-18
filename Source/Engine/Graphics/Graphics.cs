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

    public partial class Graphics : Object
    {
        private readonly Stack<IDisposable> _toDisposePermanent = new Stack<IDisposable>();
        private readonly Stack<IDisposable> _toDisposeFrame = new Stack<IDisposable>();
        
        public IPlatform Host { get; private set; }

        public int Width => Host.Width;
        public int Height => Host.Height;

        public Instance Instance { get; private set; }
        protected DebugReportCallbackExt DebugReportCallback { get; private set; }
        public SurfaceKhr Surface { get; private set; }
        public SwapchainKhr Swapchain { get; private set; }
        public Image[] SwapchainImages { get; private set; }
        public CommandBuffer[] CommandBuffers { get; private set; }
        public Fence[] SubmitFences { get; private set; }

        public Semaphore ImageAvailableSemaphore { get; private set; }
        public Semaphore RenderingFinishedSemaphore { get; private set; }

        private RenderPass _renderPass;
        public RenderPass MainRenderPass => _renderPass;

        public void Initialize(IPlatform host)
        {
            Host = host;
#if DEBUG
            const bool debug = true;
#else
            const bool debug = false;
#endif

            // Calling ToDispose here registers the resource to be automatically disposed on exit.
            Instance = CreateInstance(debug);
            DebugReportCallback = CreateDebugReportCallback(debug);
            Surface = CreateSurface();
            CreateContext(Instance, Surface, Host.Platform);

            ImageAvailableSemaphore = ToDispose(Device.CreateSemaphore());
            RenderingFinishedSemaphore = ToDispose(Device.CreateSemaphore());

            if (host.Platform == PlatformType.MacOS)
            {
                //Setup MoltenVK specific device configuration.
                MVKDeviceConfiguration deviceConfig = Device.GetMVKDeviceConfiguration();
                deviceConfig.DebugMode = debug;
                deviceConfig.PerformanceTracking = debug;
                deviceConfig.PerformanceLoggingFrameCount = debug ? 300 : 0;
                Device.SetMVKDeviceConfiguration(deviceConfig);
            }

            // Calling ToDispose here registers the resource to be automatically disposed on events
            // such as window resize.
            Swapchain = ToDisposeFrame(CreateSwapchain());
            // Acquire underlying images of the freshly created swapchain.
            SwapchainImages = Swapchain.GetImages();
            // Create a command buffer for each swapchain image.
            CommandBuffers = GraphicsCommandPool.AllocateBuffers(
                new CommandBufferAllocateInfo(CommandBufferLevel.Primary, SwapchainImages.Length));
            // Create a fence for each commandbuffer so that we can wait before using it again
            SubmitFences = new Fence[SwapchainImages.Length];
            for (int i = 0; i < SubmitFences.Length; i++)
                ToDispose(SubmitFences[i] = Device.CreateFence(new FenceCreateInfo(FenceCreateFlags.Signaled)));

            _renderPass = CreateRenderPass();
        }

        public RenderPass CreateRenderPass()
        {
            var subpasses = new[]
            {
                new SubpassDescription(new[] { new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal) })
            };
            var attachments = new[]
            {
                new AttachmentDescription
                {
                    Samples = SampleCounts.Count1,
                    Format = Swapchain.Format,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.PresentSrcKhr,
                    LoadOp = AttachmentLoadOp.Clear,
                    StoreOp = AttachmentStoreOp.Store,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare
                }
            };

            var createInfo = new RenderPassCreateInfo(subpasses, attachments);
            var rp = Device.CreateRenderPass(createInfo);
            _toDisposePermanent.Push(rp);
            return rp;
        }

        public void Resize()
        {
            Device.WaitIdle();

            // Dispose all frame dependent resources.
            while (_toDisposeFrame.Count > 0)
                _toDisposeFrame.Pop().Dispose();

            //DeviceObject.DisposeAll();

            // Reset all the command buffers allocated from the pools.
            GraphicsCommandPool.Reset();
            ComputeCommandPool.Reset();

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

            var createInfo = new InstanceCreateInfo();

            //Currently MoltenVK (used for MacOS) doesn't support the debug layer.
            if (debug && Host.Platform != PlatformType.MacOS)
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
            if (!debug || Host.Platform == PlatformType.MacOS) return null;

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
                case PlatformType.Android:
                    return Instance.CreateAndroidSurfaceKhr(new AndroidSurfaceCreateInfoKhr(Host.WindowHandle));
                case PlatformType.Win32:
                    return Instance.CreateWin32SurfaceKhr(new Win32SurfaceCreateInfoKhr(Host.InstanceHandle, Host.WindowHandle));
                case PlatformType.MacOS:
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
            switch (disposable)
            {
                case IEnumerable<IDisposable> sequence:
                    foreach (var element in sequence)
                        _toDisposePermanent.Push(element);
                    break;
                case IDisposable element:
                    _toDisposePermanent.Push(element);
                    break;
            }

            return disposable;
        }

        public T ToDisposeFrame<T>(T disposable)
        {
            switch (disposable)
            {
                case IEnumerable<IDisposable> sequence:
                    foreach (var element in sequence)
                        _toDisposeFrame.Push(element);
                    break;
                case IDisposable element:
                    _toDisposeFrame.Push(element);
                    break;
            }
            return disposable;
        }


        public override void Dispose()
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
