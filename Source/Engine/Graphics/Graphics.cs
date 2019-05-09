using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VulkanCore;
using VulkanCore.Khr;
using VulkanCore.Mvk;

namespace SharpGame
{
    public partial class Graphics : Object
    {
        private readonly static Stack<IDisposable> _toDisposePermanent = new Stack<IDisposable>();
        private readonly static Stack<IDisposable> _toDisposeFrame = new Stack<IDisposable>();
        
        public IGameWindow GameWindow { get; private set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public Image[] SwapchainImages { get; private set; }
        public ImageView[] SwapchainImageViews { get; private set; }
        public CommandBuffer[] PrimaryCmdBuffers { get; private set; }
        public CommandBuffer WorkCmdBuffer => PrimaryCmdBuffers[WorkContext];
        public CommandBufferPool[] SecondaryCmdBuffers { get; private set; }
        public CommandBufferPool WorkCmdBuffers => SecondaryCmdBuffers[WorkContext];
        public Fence[] SubmitFences { get; private set; }
        public Semaphore ImageAvailableSemaphore { get; private set; }
        public Semaphore RenderingFinishedSemaphore { get; private set; }

        public Texture DepthStencilBuffer => depthStencilBuffer_;
        private Texture depthStencilBuffer_;

        internal static DescriptorPoolManager DescriptorPoolManager { get; private set; }
        public bool LeftHand { get; set; } = true;

        public Graphics(IGameWindow host)
        {
            GameWindow = host;

            Width = host.Width;
            Height = host.Height;
#if DEBUG
            const bool debug = true;
#else
            const bool debug = false;
#endif
            // Calling ToDispose here registers the resource to be automatically disposed on exit.
            Instance = CreateInstance(debug);
            DebugReportCallback = CreateDebugReportCallback(debug);
            Surface = CreateSurface();
            CreateDevice(Instance, Surface, GameWindow.Platform);

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

            CreateSwapchainImages();

            // Create a command buffer for each swapchain image.
            PrimaryCmdBuffers = GraphicsCommandPool.AllocateBuffers(
                new CommandBufferAllocateInfo(CommandBufferLevel.Primary, SwapchainImages.Length));
            // Create a fence for each commandbuffer so that we can wait before using it again
            SubmitFences = new Fence[SwapchainImages.Length];
            for (int i = 0; i < SubmitFences.Length; i++)
                SubmitFences[i] = CreateFence(new FenceCreateInfo(FenceCreateFlags.Signaled));

            SecondaryCmdBuffers = new CommandBufferPool[2];

            SecondaryCmdBuffers[0] = new CommandBufferPool(
                SecondaryCommandPool[0].AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Secondary, 10)));

            SecondaryCmdBuffers[1] = new CommandBufferPool(
                SecondaryCommandPool[1].AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Secondary, 10)));

            renderThreadID_ = System.Threading.Thread.CurrentThread.ManagedThreadId;
            DescriptorPoolManager = new DescriptorPoolManager();
        }


        public static T ToDispose<T>(T disposable)
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

        public static T ToDisposeFrame<T>(T disposable)
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

        protected override void Destroy()
        {
            Device.WaitIdle();

            while (_toDisposeFrame.Count > 0)
                _toDisposeFrame.Pop().Dispose();

            while (_toDisposePermanent.Count > 0)
                _toDisposePermanent.Pop().Dispose();

            GPUObject.DisposeAll();

            ComputeCommandPool.Dispose();
            GraphicsCommandPool.Dispose();
            DescriptorPoolManager.DestroyAll();
            Device.Dispose();
            DebugReportCallback.Dispose();
            Surface.Dispose();

            Instance.Dispose();
        }

        public void Resize(int w, int h)
        {
            Device.WaitIdle();

            Width = w;
            Height = h;

            // Dispose all frame dependent resources.
            while (_toDisposeFrame.Count > 0)
                _toDisposeFrame.Pop().Dispose();

            // Reset all the command buffers allocated from the pools.
            GraphicsCommandPool.Reset();
            ComputeCommandPool.Reset();
            SecondaryCommandPool[0].Reset();
            SecondaryCommandPool[1].Reset();

            CreateSwapchainImages();

            depthStencilBuffer_ = CreateDepthStencil(Width, Height);

            GPUObject.RecreateAll();
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

        private void CreateSwapchainImages()
        {
            Swapchain = ToDisposeFrame(CreateSwapchain());
            SwapchainImages = Swapchain.GetImages();
            SwapchainImageViews = ToDisposeFrame(CreateImageViews());

            depthStencilBuffer_ = CreateDepthStencil(Width, Height);

        }

        private ImageView[] CreateImageViews()
        {
            var imageViews = new ImageView[SwapchainImages.Length];
            for (int i = 0; i < SwapchainImages.Length; i++)
            {
                imageViews[i] = SwapchainImages[i].CreateView(new ImageViewCreateInfo(
                    Swapchain.Format,
                    new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1)));
            }

            return imageViews;
        }

        public Texture CreateDepthStencil(int width, int height)
        {
            Format[] validFormats =
            {
                Format.D32SFloatS8UInt,
                Format.D32SFloat,
                Format.D24UNormS8UInt,
                Format.D16UNormS8UInt,
                Format.D16UNorm
            };

            Format? potentialFormat = validFormats.FirstOrDefault(
                validFormat =>
                {
                    FormatProperties formatProps = PhysicalDevice.GetFormatProperties(validFormat);
                    return (formatProps.OptimalTilingFeatures & FormatFeatures.DepthStencilAttachment) > 0;
                });

            if (!potentialFormat.HasValue)
                throw new InvalidOperationException("Required depth stencil format not supported.");

            Format format = potentialFormat.Value;
            Image image = Device.CreateImage(new ImageCreateInfo
            {
                ImageType = ImageType.Image2D,
                Format = format,
                Extent = new Extent3D(width, height, 1),
                MipLevels = 1,
                ArrayLayers = 1,
                Samples = SampleCounts.Count1,
                Tiling = ImageTiling.Optimal,
                Usage = ImageUsages.DepthStencilAttachment | ImageUsages.TransferSrc
            });

            MemoryRequirements memReq = image.GetMemoryRequirements();

            int heapIndex = MemoryProperties.MemoryTypes.IndexOf(
                memReq.MemoryTypeBits, VulkanCore.MemoryProperties.DeviceLocal);
            DeviceMemory memory = Device.AllocateMemory(new MemoryAllocateInfo(memReq.Size, heapIndex));
            image.BindMemory(memory);

            ImageView view = image.CreateView(new ImageViewCreateInfo(format,
                new ImageSubresourceRange(ImageAspects.Depth | ImageAspects.Stencil, 0, 1, 0, 1)));

            return ToDisposeFrame(new Texture(image, memory, view, format));
        }

        public static Sampler CreateSampler()
        {
            var createInfo = new SamplerCreateInfo
            {
                MagFilter = Filter.Linear,
                MinFilter = Filter.Linear,
                MipmapMode = SamplerMipmapMode.Linear
            };
            // We also enable anisotropic filtering. Because that feature is optional, it must be
            // checked if it is supported by the device.
            if (Features.SamplerAnisotropy)
            {
                createInfo.AnisotropyEnable = true;
                createInfo.MaxAnisotropy = Properties.Limits.MaxSamplerAnisotropy;
            }
            else
            {
                createInfo.MaxAnisotropy = 1.0f;
            }
            return ToDispose(Device.CreateSampler(createInfo));
        }

        public static Fence CreateFence(FenceCreateInfo createInfo = default)
        {
            return ToDispose(Device.CreateFence(createInfo));
        }

        internal static DescriptorSetLayout CreateDescriptorSetLayout(params DescriptorSetLayoutBinding[] bindings)
        {
            return ToDispose(Device.CreateDescriptorSetLayout(new DescriptorSetLayoutCreateInfo(bindings)));
        }
        
        #region MULTITHREADED

        private int currentContext_;
        public int WorkContext => SingleThreaded ? imageIndex : currentContext_;
        //public int RenderContext => 1 - currentContext_;

        private int currentFrame_;
        public int CurrentFrame => currentFrame_;

        private int renderThreadID_;
        public bool IsRenderThread => renderThreadID_ == System.Threading.Thread.CurrentThread.ManagedThreadId;

        private System.Threading.Semaphore renderSem_ = new System.Threading.Semaphore(0, 1);
        private System.Threading.Semaphore mainSem_ = new System.Threading.Semaphore(0, 1);
        private long waitSubmit_;
        private long waitRender_;

        public bool SingleThreaded { get; set; } = false;

        private List<Action> commands_ = new List<Action>();

        public void Post(Action action) { commands_.Add(action); }

        public void Frame()
        {
            RenderSemWait();
            FrameNoRenderWait();
        }

        public void Close()
        {
            MainSemWait();
            RenderSemPost();
        }

        int imageIndex = 0;

        public int BeginRender()
        {
            imageIndex = Swapchain.AcquireNextImage(semaphore: ImageAvailableSemaphore);

            if (MainSemWait())
            {
                return imageIndex;
            }

            return imageIndex;
        }

        public void EndRender()
        {
            RenderSemPost();
        }

        void SwapContext()
        {
            currentFrame_++;
            currentContext_ = 1 - currentContext_;
            //Console.WriteLine("===============SwapContext : {0}", currentContext_);
        }

        public void FrameNoRenderWait()
        {
            SwapContext();
            // release render thread
            MainSemPost();
        }

        public void MainSemPost()
        {
            if (!SingleThreaded)
            {
                mainSem_.Release();
            }
        }

        bool MainSemWait()
        {
            if (SingleThreaded)
            {
                return true;
            }

            long curTime = Stopwatch.GetTimestamp();
            bool ok = mainSem_.WaitOne(-1);
            if (ok)
            {
                waitSubmit_ = (long)((Stopwatch.GetTimestamp() - curTime) * Timer.MilliSecondsPerCount);
                return true;
            }

            return false;
        }

        void RenderSemPost()
        {
            if (!SingleThreaded)
            {
                renderSem_.Release();
            }
        }

        void RenderSemWait()
        {
            if (!SingleThreaded)
            {
                long curTime = Stopwatch.GetTimestamp();
                bool ok = renderSem_.WaitOne();                
                waitRender_ = (long)((Stopwatch.GetTimestamp() - curTime) * Timer.MilliSecondsPerCount);
            }
        }
        #endregion
    }
}
