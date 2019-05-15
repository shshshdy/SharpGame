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

        protected IGameWindow gameWindow;
        protected FileSystem fileSystem;
        protected Graphics graphics;
        protected Renderer renderer;
        protected ResourceCache resourceCache;
        protected Input input;
        protected Timer timer;

        private int frameNumber;
        private float timeElapsed;
        private bool appPaused = false;

        bool singleThreaded = false;

        SynchronizationContext workThreadSyncContext;
        int workThreadId;

        static private bool _running;   // Is the application running?

        public string DataPath { get; }

        private Context context;
        public Application(string dataPath)
        {
            context = new Context();

            DataPath = dataPath;
        }

        protected override void Destroy()
        {
            context.Dispose();
        }
        
        public void Run(IGameWindow window)
        {
            gameWindow = window;

            Run();
        }

        protected virtual void Setup()
        {
            timer = CreateSubsystem<Timer>();
            fileSystem = CreateSubsystem<FileSystem>();
            graphics = CreateSubsystem<Graphics>(gameWindow);
            graphics.SingleThreaded = this.singleThreaded;
            resourceCache = CreateSubsystem<ResourceCache>(DataPath);
            renderer = CreateSubsystem<Renderer>();
            input = CreateSubsystem<Input>();

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
            appPaused = false;
            timer.Start();
        }

        public void Deactivate()
        {
            appPaused = true;
            timer.Stop();
        }

        public void Pause()
        {
            appPaused = true;
            timer.Stop();
        }

        public void Resume()
        {
            appPaused = false;
            timer.Start();
        }

        public static void Quit()
        {
            _running = false;
        }

        public void Run()
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
                    if(renderer == null)
                    {
                        continue;
                    }
                    
                    if (!appPaused)
                    {
                        renderer.Render();
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
            workThreadSyncContext = SynchronizationContext.Current;
            workThreadId = Thread.CurrentThread.ManagedThreadId;

            gameWindow.Create();

            Setup();

            Init();

            gameWindow.Show();

            timer.Reset();

            _running = true;

            while (_running)
            {
                gameWindow.PumpEvents(input.InputSnapshot);

                if (!appPaused)
                {
                    UpdateFrame();

                    renderer.Render();
                }
                else
                {
                    Thread.Sleep(100);
                }
            }

            Shutdown();

            graphics.Close();

            gameWindow.Destroy();
        }

        void SimulateThread()
        {
            workThreadSyncContext = SynchronizationContext.Current;
            workThreadId = Thread.CurrentThread.ManagedThreadId;

            gameWindow.Create();

            Setup();

            timer.Reset();

            Init();

            gameWindow.Show();

            graphics.FrameNoRenderWait();
            graphics.Frame();

            _running = true;
            while (_running)
            {
                gameWindow.PumpEvents(input.InputSnapshot);

                UpdateFrame();

                graphics.Frame();
            }

            graphics.Frame();

            Shutdown();

            graphics.Close();

            gameWindow.Destroy();
        }

        void UpdateFrame()
        {
            timer.Tick();

            var beginFrame = new BeginFrame
            {
                frameNum_ = frameNumber,
                timeTotal_ = timer.TotalTime,
                timeDelta_ = timer.DeltaTime
            };

            this.SendGlobalEvent(ref beginFrame);

            var update = new Update
            {
                timeTotal_ = timer.TotalTime,
                timeDelta_ = timer.DeltaTime
            };

            this.SendGlobalEvent(ref update);

            Update();

            var postUpdate = new PostUpdate
            {
                timeTotal_ = timer.TotalTime,
                timeDelta_ = timer.DeltaTime
            };

            this.SendGlobalEvent(ref postUpdate);

            renderer.RenderUpdate();

            var endFrame = new EndFrame { };

            this.SendGlobalEvent(ref endFrame); 

            CalculateFrameRateStats();

        }

        private void CalculateFrameRateStats()
        {
            frameNumber++;

            if (timer.TotalTime - timeElapsed >= 1.0f)
            {
                float fps = frameNumber;
                float mspf = 1000.0f / fps;
                gameWindow.Title = $"{Name}    Fps: {fps}    Mspf: {mspf}";
                // Reset for next average.
                frameNumber = 0;
                timeElapsed += 1.0f;
            }
        }



    }

}
