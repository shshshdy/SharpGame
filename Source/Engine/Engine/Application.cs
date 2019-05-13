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

    public interface IGameWindow : IDisposable
    {
        IntPtr WindowHandle { get; }
        IntPtr InstanceHandle { get; }
        int Width { get; }
        int Height { get; }
        string Title { get; set; }
        PlatformType Platform { get; }
        void Create();
        void Destroy();
        void Show();
        void PumpEvents(InputSnapshot inputSnapshot);       
        Stream Open(string path);
    }

    public abstract class Application : Object
    {
        public string Name { get; set; }
        protected IGameWindow gameWindow_;
        protected FileSystem fileSystem_;
        protected Graphics graphics_;
        protected Renderer renderer_;
        protected ResourceCache resourceCache_;
        protected Input input_;

        protected Timer timer_;

        private int frameNumber_;
        private float _timeElapsed;
        private bool _appPaused = false;

        SynchronizationContext workThreadSyncContext_;
        int workThreadId_;

        static private bool _running;   // Is the application running?

        public Application()
        {
            new Context();
        }

        protected override void Destroy()
        {
            _context.Dispose();
        }
        
        public void Run(IGameWindow window)
        {
            gameWindow_ = window;

            Run();
        }

        protected virtual void Setup()
        {
            timer_ = CreateSubsystem<Timer>();
            fileSystem_ = CreateSubsystem<FileSystem>(gameWindow_);
            graphics_ = CreateSubsystem<Graphics>(gameWindow_);
            resourceCache_ = CreateSubsystem<ResourceCache>("../../Content");
            renderer_ = CreateSubsystem<Renderer>();
            input_ = CreateSubsystem<Input>();

        }

        protected virtual void Init()
        {
        }
        
        protected virtual void Update()
        {
        }

        protected virtual void Shutdown()
        {
        }

        public void Resize()
        {
            //graphics_.Resize();

            //SendGlobalEvent(new Resizing());
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

        public static void Quit()
        {
            _running = false;
        }

        public void Run(bool singleThreaded = false)
        {
            if(singleThreaded)
            {
                RunSingleThread();
            }
            else
            {
                new Thread(SimulateThread).Start();
                _running = true;
                while (_running)
                {
                    if(renderer_ == null)
                    {
                        continue;
                    }
                    
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

        }

        void RunSingleThread()
        {
            workThreadSyncContext_ = SynchronizationContext.Current;
            workThreadId_ = Thread.CurrentThread.ManagedThreadId;

            gameWindow_.Create();

            Setup();

            Init();

            gameWindow_.Show();

            timer_.Reset();

            _running = true;

            while (_running)
            {
                gameWindow_.PumpEvents(input_.InputSnapshot);

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

            Shutdown();

            graphics_.Close();

            gameWindow_.Destroy();
        }

        void SimulateThread()
        {
            workThreadSyncContext_ = SynchronizationContext.Current;
            workThreadId_ = Thread.CurrentThread.ManagedThreadId;

            gameWindow_.Create();

            Setup();

            timer_.Reset();

            Init();

            gameWindow_.Show();

            graphics_.FrameNoRenderWait();
            graphics_.Frame();

            _running = true;
            while (_running)
            {
                gameWindow_.PumpEvents(input_.InputSnapshot);

                UpdateFrame();

                graphics_.Frame();
            }

            graphics_.Frame();

            Shutdown();

            graphics_.Close();

            gameWindow_.Destroy();
        }

        void UpdateFrame()
        {
            timer_.Tick();

            var beginFrame = new BeginFrame
            {
                frameNum_ = frameNumber_,
                timeTotal_ = timer_.TotalTime,
                timeDelta_ = timer_.DeltaTime
            };

            this.SendGlobalEvent(ref beginFrame);

            var update = new Update
            {
                timeTotal_ = timer_.TotalTime,
                timeDelta_ = timer_.DeltaTime
            };

            this.SendGlobalEvent(ref update);

            Update();

            var postUpdate = new PostUpdate
            {
                timeTotal_ = timer_.TotalTime,
                timeDelta_ = timer_.DeltaTime
            };

            this.SendGlobalEvent(ref postUpdate);

            renderer_.RenderUpdate();

            var endFrame = new EndFrame { };

            this.SendGlobalEvent(ref endFrame); 

            CalculateFrameRateStats();

        }

        private void CalculateFrameRateStats()
        {
            frameNumber_++;

            if (timer_.TotalTime - _timeElapsed >= 1.0f)
            {
                float fps = frameNumber_;
                float mspf = 1000.0f / fps;
                gameWindow_.Title = $"{Name}    Fps: {fps}    Mspf: {mspf}";
                // Reset for next average.
                frameNumber_ = 0;
                _timeElapsed += 1.0f;
            }
        }



    }

}
