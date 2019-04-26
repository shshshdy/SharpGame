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
        protected IPlatform platform_;
        protected FileSystem fileSystem_;
        protected Graphics graphics_;
        protected Renderer renderer_;
        protected ResourceCache resourceCache_;

        private Timer timer_;
        private bool _running;   // Is the application running?
        private int frameNumber_;
        private float _timeElapsed;
        private bool _appPaused = false;

        SynchronizationContext workThreadSyncContext_;
        int workThreadId_;

        public Application()
        {
            new Context();
        }

        public override void Dispose()
        {
            _context.Dispose();
        }

        public void Initialize(IPlatform host)
        {
            Name = host.Tittle;
            platform_ = host;

            timer_ = CreateSubsystem<Timer>();
            fileSystem_ = CreateSubsystem<FileSystem>(platform_);            
            graphics_ = CreateSubsystem<Graphics>(platform_);
            resourceCache_ = CreateSubsystem<ResourceCache>("Content");
            renderer_ = CreateSubsystem<Renderer>();
            renderer_.Inialize();
        }

        protected virtual void OnInit()
        {
        }
        
        protected virtual void Update(Timer timer)
        {
        }

        public void Resize()
        {
            graphics_.Resize();

            SendGlobalEvent(new Resizing());
        }

        public void Activate()
        {
            _appPaused = false;
            timer_.Start();
        }

        public void Deactivate()
        {
            _appPaused = true;
            timer_.Stop();
        }

        public void Pause()
        {
            _appPaused = true;
            timer_.Stop();
        }

        public void Resume()
        {
            _appPaused = false;
            timer_.Start();
        }

        public void Quit()
        {
            _running = false;
        }

        public void Run()
        {
            _running = true;

            if(graphics_.SingleThreaded)
            {
                RunSingleThread();
            }
            else
            {
                new Thread(SimulateThread).Start();

                while (_running)
                {
                    platform_.ProcessEvents();

                    if (!_appPaused)
                    {
                        renderer_.Render();
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }

            graphics_.Close();
        }

        void RunSingleThread()
        {
            workThreadSyncContext_ = SynchronizationContext.Current;
            workThreadId_ = Thread.CurrentThread.ManagedThreadId;

            timer_.Reset();

            OnInit();

            while (_running)
            {
                platform_.ProcessEvents();

                if (!_appPaused)
                {
                    UpdateFrame();

                    renderer_.Render();
                }
                else
                {
                    Thread.Sleep(100);
                }
            }


        }

        void SimulateThread()
        {
            workThreadSyncContext_ = SynchronizationContext.Current;
            workThreadId_ = Thread.CurrentThread.ManagedThreadId;

            timer_.Reset();

            OnInit();
            
            graphics_.FrameNoRenderWait();
            graphics_.Frame();

            while (_running)
            { 
                UpdateFrame();

                graphics_.Frame();
            }

        }

        void UpdateFrame()
        {
            timer_.Tick();

            SendGlobalEvent(new BeginFrame { frameNum_ = frameNumber_, timeTotal_ = timer_.TotalTime, timeDelta_ = timer_.DeltaTime });

            SendGlobalEvent(new Update { timeTotal_ = timer_.TotalTime, timeDelta_ = timer_.DeltaTime });

            Update(timer_);

            SendGlobalEvent(new PostUpdate { timeTotal_ = timer_.TotalTime, timeDelta_ = timer_.DeltaTime });

            renderer_.RenderUpdate();


            SendGlobalEvent(new EndFrame { }); 

            CalculateFrameRateStats();

        }

        private void CalculateFrameRateStats()
        {
            frameNumber_++;

            if (timer_.TotalTime - _timeElapsed >= 1.0f)
            {
                float fps = frameNumber_;
                float mspf = 1000.0f / fps;

                graphics_.Post(() =>
                {
                    platform_.Tittle = $"{Name}    Fps: {fps}    Mspf: {mspf}";
                });

                // Reset for next average.
                frameNumber_ = 0;
                _timeElapsed += 1.0f;
            }
        }

        protected T ToDispose<T>(T disposable) => graphics_.ToDispose(disposable);

    }

}
