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

        SynchronizationContext synchronizationContext_;
        int mainThreadId_;

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

            new Thread(LogicThread).Start();
            
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

            graphics_.Close();
        }

        void LogicThread()
        {
            synchronizationContext_ = SynchronizationContext.Current;
            mainThreadId_ = Thread.CurrentThread.ManagedThreadId;

            timer_.Reset();

            OnInit();

            graphics_.FrameNoRenderWait();

            graphics_.Frame();

            while (_running)
            {                
                timer_.Tick();

                SendGlobalEvent(new BeginFrame { frameNum_ = frameNumber_, timeDelta_ = timer_.DeltaTime });

                SendGlobalEvent(new Update { timeDelta_ = timer_.DeltaTime });

                SendGlobalEvent(new PostUpdate { timeDelta_ = timer_.DeltaTime });

                renderer_.Update();

                Update(timer_);       

                graphics_.Frame();

                SendGlobalEvent(new EndFrame {});


                CalculateFrameRateStats();
            }

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
