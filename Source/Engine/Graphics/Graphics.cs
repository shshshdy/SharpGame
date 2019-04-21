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
        
        public IPlatform Host { get; private set; }

        public int Width => Host.Width;
        public int Height => Host.Height;

        public Image[] SwapchainImages { get; private set; }
        public CommandBuffer[] CommandBuffers { get; private set; }
        public Fence[] SubmitFences { get; private set; }

        public Semaphore ImageAvailableSemaphore { get; private set; }
        public Semaphore RenderingFinishedSemaphore { get; private set; }
        
        public Graphics(IPlatform host)
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
            
        }
        
        public void Resize()
        {
            Device.WaitIdle();

            // Dispose all frame dependent resources.
            while (_toDisposeFrame.Count > 0)
                _toDisposeFrame.Pop().Dispose();

            GPUObject.RecreateAll();

            // Reset all the command buffers allocated from the pools.
            GraphicsCommandPool.Reset();
            ComputeCommandPool.Reset();

            // Reinitialize frame dependent resources.
            Swapchain = ToDisposeFrame(CreateSwapchain());
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
