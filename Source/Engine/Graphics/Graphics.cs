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
        private readonly Stack<IDisposable> _toDisposePermanent = new Stack<IDisposable>();
        private readonly Stack<IDisposable> _toDisposeFrame = new Stack<IDisposable>();
        
        public IPlatform Platform { get; private set; }

        public int Width => Platform.Width;
        public int Height => Platform.Height;

        public Image[] SwapchainImages { get; private set; }
        public ImageView[] SwapchainImageViews { get; private set; }
        public CommandBuffer[] PrimaryCmdBuffers { get; private set; }
        public Fence[] SubmitFences { get; private set; }

        public Semaphore ImageAvailableSemaphore { get; private set; }
        public Semaphore RenderingFinishedSemaphore { get; private set; }

        private System.Threading.Semaphore mainSem_;
        
        public Graphics(IPlatform host)
        {
            Platform = host;
#if DEBUG
            const bool debug = true;
#else
            const bool debug = false;
#endif

            // Calling ToDispose here registers the resource to be automatically disposed on exit.
            Instance = CreateInstance(debug);
            DebugReportCallback = CreateDebugReportCallback(debug);
            Surface = CreateSurface();
            CreateContext(Instance, Surface, Platform.Platform);

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
                ToDispose(SubmitFences[i] = Device.CreateFence(new FenceCreateInfo(FenceCreateFlags.Signaled)));
            
        }

        private void CreateSwapchainImages()
        {
            Swapchain = ToDisposeFrame(CreateSwapchain());
            SwapchainImages = Swapchain.GetImages();
            SwapchainImageViews = ToDisposeFrame(CreateImageViews());
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

        public void Resize()
        {
            Device.WaitIdle();

            // Dispose all frame dependent resources.
            while (_toDisposeFrame.Count > 0)
                _toDisposeFrame.Pop().Dispose();

            // Reset all the command buffers allocated from the pools.
            GraphicsCommandPool.Reset();
            ComputeCommandPool.Reset();

            CreateSwapchainImages();

            GPUObject.RecreateAll();


        }


        public virtual void Draw()
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
                PrimaryCmdBuffers[imageIndex],
                RenderingFinishedSemaphore,
                SubmitFences[imageIndex]
            );
  
            // Present the color output to screen.
            PresentQueue.PresentKhr(RenderingFinishedSemaphore, Swapchain, imageIndex);
          
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

            GPUObject.DisposeAll();

            ComputeCommandPool.Dispose();
            GraphicsCommandPool.Dispose();
            Device.Dispose();

            DebugReportCallback.Dispose();
            Surface.Dispose();

            Instance.Dispose();
        }

    }
}
