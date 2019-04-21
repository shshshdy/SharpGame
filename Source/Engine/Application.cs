using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using VulkanCore;


namespace SharpGame
{
    public enum PlatformType
    {
        Android, Win32, MacOS
    }

    public interface IPlatform : IDisposable
    {
        IntPtr WindowHandle { get; }
        IntPtr InstanceHandle { get; }
        int Width { get; }
        int Height { get; }
        string Tittle { get; set; }

        PlatformType Platform { get; }
        void ProcessEvents();
        Stream Open(string path);
    }

    public abstract class Application : Object
    {
        public string Name { get; set; }
        public IPlatform Platform { get; set; }
        public FileSystem FileSystem { get; private set; }
        public Graphics Graphics { get; private set; }
        public  Renderer Renderer { get; private set; }
        public ResourceCache ResourceCache { get; private set; }

        bool inited = false;

        private Timer _timer;
        private bool _running;   // Is the application running?
        private int _frameCount;
        private float _timeElapsed;
        private bool _appPaused = false;

        public Application()
        {
            new Context();
        }

        public void Initialize(IPlatform host)
        {
            Name = host.Tittle;
            Platform = host;

            FileSystem = CreateSubsystem<FileSystem>(Platform);
            _timer = CreateSubsystem<Timer>();
            Graphics = CreateSubsystem<Graphics>(Platform);
            ResourceCache = CreateSubsystem<ResourceCache>("Content");
            Renderer = CreateSubsystem<Renderer>();

            //new Thread(() =>
            {

                // Allow concrete samples to initialize their resources.
                InitializePermanent();
                //_initializingPermanent = false;
                InitializeFrame();

                // Record commands for execution by Vulkan.
                RecordCommandBuffers();

                inited = true;
            }
            //).Start();

        }

        /// <summary>
        /// Allows derived classes to initializes resources the will stay alive for the duration of
        /// the application.
        /// </summary>
        protected virtual void InitializePermanent() { }

        /// <summary>
        /// Allows derived classes to initializes resources that need to be recreated on events such
        /// as window resize.
        /// </summary>
        protected virtual void InitializeFrame() { }

        public void Resize()
        {
            Graphics.Resize();

            Renderer.Recreate();

            InitializeFrame();

            // Re-record command buffers.
            RecordCommandBuffers();
        }

        public void Activate()
        {
            _appPaused = false;
            _timer.Start();
        }

        public void Deactivate()
        {
            _appPaused = true;
            _timer.Stop();
        }

        public void Pause()
        {
            _appPaused = true;
            _timer.Stop();
        }

        public void Resume()
        {
            _appPaused = false;
            _timer.Start();
        }

        public void Quit()
        {
            _running = false;
        }

        public void Run()
        {
            _running = true;
            _timer.Reset();

            //void r()
            {
                while (_running)
                {
                    Platform.ProcessEvents();

                    _timer.Tick();

                    if (!_appPaused)
                    {
                        CalculateFrameRateStats();
                        Tick(_timer);
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }

            }

            //new Thread(r).Start();
        }

        private void CalculateFrameRateStats()
        {
            _frameCount++;

            if (_timer.TotalTime - _timeElapsed >= 1.0f)
            {
                float fps = _frameCount;
                float mspf = 1000.0f / fps;

                Platform.Tittle = $"{Name}    Fps: {fps}    Mspf: {mspf}";

                // Reset for next average.
                _frameCount = 0;
                _timeElapsed += 1.0f;
            }
        }

        public void Tick(Timer timer)
        {
            Update(timer);

            Renderer.Update();
            
            Graphics.Draw(timer);
        }

        protected virtual void Update(Timer timer) { }

        void RecordCommandBuffers()
        {
            var subresourceRange = new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1);
            for (int i = 0; i < Graphics.CommandBuffers.Length; i++)
            {
                CommandBuffer cmdBuffer = Graphics.CommandBuffers[i];
                cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.SimultaneousUse));

                if (Graphics.PresentQueue != Graphics.GraphicsQueue)
                {
                    var barrierFromPresentToDraw = new ImageMemoryBarrier(
                        Graphics.SwapchainImages[i], subresourceRange,
                        Accesses.MemoryRead, Accesses.ColorAttachmentWrite,
                        ImageLayout.Undefined, ImageLayout.PresentSrcKhr,
                        Graphics.PresentQueue.FamilyIndex, Graphics.GraphicsQueue.FamilyIndex);

                    cmdBuffer.CmdPipelineBarrier(
                        PipelineStages.ColorAttachmentOutput,
                        PipelineStages.ColorAttachmentOutput,
                        imageMemoryBarriers: new[] { barrierFromPresentToDraw });
                }

                RecordCommandBuffer(cmdBuffer, i);

                if (Graphics.PresentQueue != Graphics.GraphicsQueue)
                {
                    var barrierFromDrawToPresent = new ImageMemoryBarrier(
                        Graphics.SwapchainImages[i], subresourceRange,
                        Accesses.ColorAttachmentWrite, Accesses.MemoryRead,
                        ImageLayout.PresentSrcKhr, ImageLayout.PresentSrcKhr,
                        Graphics.GraphicsQueue.FamilyIndex, Graphics.PresentQueue.FamilyIndex);

                    cmdBuffer.CmdPipelineBarrier(
                        PipelineStages.ColorAttachmentOutput,
                        PipelineStages.BottomOfPipe,
                        imageMemoryBarriers: new[] { barrierFromDrawToPresent });
                }

                cmdBuffer.End();
            }
        }


        protected abstract void RecordCommandBuffer(CommandBuffer cmdBuffer, int imageIndex);

        protected T ToDispose<T>(T disposable) => Graphics.ToDispose(disposable);
        protected T ToDisposeFrame<T>(T disposable) => Graphics.ToDisposeFrame(disposable);

        public override void Dispose()
        {
            Graphics.Dispose();
        }
    }

}
