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
        public CommandBuffer[] SecondaryCmdBuffers { get; private set; }
        public CommandBuffer WorkingCmdBuffer => SecondaryCmdBuffers[currentContext_];

        public Fence[] SubmitFences { get; private set; }

        public Semaphore ImageAvailableSemaphore { get; private set; }
        public Semaphore RenderingFinishedSemaphore { get; private set; }
                
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

            SecondaryCmdBuffers = SecondaryCommandPool[0].AllocateBuffers(
                new CommandBufferAllocateInfo(CommandBufferLevel.Secondary, 2));

            //SecondaryCmdBuffers = GraphicsCommandPool.AllocateBuffers(
            //    new CommandBufferAllocateInfo(CommandBufferLevel.Primary, 2));

            renderThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;

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
            SecondaryCommandPool[0].Reset();
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
           
            CommandBuffer cmdBuffer = PrimaryCmdBuffers[RenderContext];     

            // Submit recorded commands to graphics queue for execution.
            GraphicsQueue.Submit(
                ImageAvailableSemaphore,
                PipelineStages.ColorAttachmentOutput,
                cmdBuffer,
                RenderingFinishedSemaphore,
                SubmitFences[imageIndex]
            );
       

            // Present the color output to screen.
            PresentQueue.PresentKhr(RenderingFinishedSemaphore, Swapchain, imageIndex);
          
        }

        #region MULTITHREAD
        int currentContext_;
        public int WorkContext => currentContext_;
        public int RenderContext => 1 - currentContext_;

        public int currentFrame_;

        int renderThreadID;
        bool singleThreaded_ = false;
        System.Threading.Semaphore renderSem_ = new System.Threading.Semaphore(0, 1);
        System.Threading.Semaphore mainSem_ = new System.Threading.Semaphore(0, 1);
        long waitSubmit_;
        long waitRender_;

        public bool IsRenderThread => renderThreadID == System.Threading.Thread.CurrentThread.ManagedThreadId;

        List<Action> commands_ = new List<Action>();
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

        public bool BeginRender()
        {
            if (MainSemWait())
            {
                if(commands_.Count > 0)
                {
                    foreach (var cmd in commands_)
                    {
                        cmd.Invoke();
                    }

                    commands_.Clear();
                }

                return true;
            }

            return false;
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
            if (!singleThreaded_)
            {
                mainSem_.Release();
            }
        }

        bool MainSemWait()
        {
            if (singleThreaded_)
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
            if (!singleThreaded_)
            {
                renderSem_.Release();
            }
        }

        void RenderSemWait()
        {
            if (!singleThreaded_)
            {
                long curTime = Stopwatch.GetTimestamp();
                bool ok = renderSem_.WaitOne();                
                waitRender_ = (long)((Stopwatch.GetTimestamp() - curTime) * Timer.MilliSecondsPerCount);
            }
        }
        #endregion
    }
}
