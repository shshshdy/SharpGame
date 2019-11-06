using System;
using System.Diagnostics;
using System.IO;
using SharpGame.Sdl2;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace SharpGame
{
    public class Application : CoreApplication
    {
        public static Application Instance { get; private set; }

        private string m_title= "SharpGame";
        public string Title
        {
            get => m_title;
            set
            {
                m_title = value;
                if(window != null)
                {
                    window.Title = value;
                }
            }
        }

        public UTF8String Name { get; set; } = "SharpGame";
        public int Width { get; protected set; } = 1280;
        public int Height { get; protected set; } = 720;

        public Settings Settings { get; } = new Settings();
        public ref Stats Stats => ref graphics.stats;

        protected IntPtr windowHandle;
        protected IntPtr windowInstance;
        protected Sdl2Window window;

        protected string workSpace;
        protected Timer timer;
        protected FileSystem fileSystem;
        protected Resources cache;
        protected Graphics graphics;
        protected Renderer renderer;
        protected Input input;
        protected bool paused = false;
        private bool shouldQuit = false;

        protected bool singleLoop = false;
        private bool mainThreadRender = false;

        private float fps;
        public float Fps => fps;
        private float msec;
        public float Msec => msec;

        private long elapsedTime;
        private int frameNum;
        private List<float> lastTimeSteps = new List<float>();
        private float timeStep;
        private int timeStepSmoothing = 2;
        private uint minFps = 10;
        private uint maxFps = 3000;
        private bool resized = false;
        protected RenderView mainView;

        public RenderView MainView => mainView;

        public Application(string dataPath)
        {
            Instance = this;
            workSpace = Path.Combine(AppContext.BaseDirectory, dataPath);
        }

        protected virtual void Setup()
        {
            Resource.RegisterAllResType(typeof(Graphics));
            Resource.RegisterAllResType(typeof(Application));

            timer = CreateSubsystem<Timer>();
            fileSystem = CreateSubsystem<FileSystem>(workSpace);
            cache = CreateSubsystem<Resources>();

            cache.RegisterAssetReader(new ShaderReader());

            cache.RegisterAssetReader(new MdlModelReader());
            cache.RegisterAssetReader(new ObjModelReader());
            cache.RegisterAssetReader(new AssimpModelReader());

            cache.RegisterAssetReader(new DDSTextureReader());
            cache.RegisterAssetReader(new SharpTextureReader());
            cache.RegisterAssetReader(new KtxTextureReader());

            cache.RegisterAssetReader(new AnimationReader());

            CreateWindow();

            Settings.SingleLoop = singleLoop;
            Settings.ApplicationName = Name;

            graphics = CreateSubsystem<Graphics>(Settings);
            graphics.Init(window.SdlWindowHandle);
            renderer = CreateSubsystem<Renderer>();
            input = CreateSubsystem<Input>();
            renderer.Initialize();

            CreateSubsystem<ImGUI>();
        }

        protected virtual void Init()
        {
            mainView = renderer.CreateRenderView();
        }

        protected virtual void CreateWindow()
        {
            windowInstance = Process.GetCurrentProcess().SafeHandle.DangerousGetHandle();
            window = new Sdl2Window(Title, 50, 50, Width, Height, /*SDL_WindowFlags.Resizable|*/0, threadedProcessing: false)
            {
                X = 50,
                Y = 50,
                Visible = true
            };

            window.Create();

            windowHandle = window.Handle;
            window.Resized += OnWindowResize;
            window.Closing += OnWindowClosing;

            window.KeyDown += Window_KeyDown;
            window.KeyUp += Window_KeyUp;
        }

        public static void Quit()
        {
            Instance.shouldQuit = true;
        }

        public void Run()
        {
            if (singleLoop)
            {
                SingleLoop();
            }
            else
            {
                DoubleLoop();
            }
        }

        public void SingleLoop()
        {
            Graphics.SetMainThread();
            Graphics.SetRenderThread();

            Setup();

            Init();

            Start();

            while (window.Exists)
            {
                Profiler.Begin();
                Time.Tick(timeStep);
                Stats.Tick(timeStep);

                input.snapshot = window.PumpEvents();

                if (!window.Exists)
                {
                    // Exit early if the window was closed this frame.
                    break;
                }
               
                UpdateFrame();

                renderer.Submit();

                ApplyFrameLimit();
                Profiler.End();
            }

            timer.Stop();
            // Flush device to make sure all resources can be freed 
            graphics.WaitIdle();

            Destroy();

        }

        private void DoubleLoop()
        {
            if(mainThreadRender)
            {
                new Thread(SimulateLoop).Start();
               
                RenderLoop();
            }
            else
            {
                new Thread(RenderLoop).Start();
                SimulateLoop();     
            }

            
        }

        private void SimulateLoop()
        {
            Graphics.SetMainThread();

            Setup();

            Init();

            Start();
            started = true;

            //graphics.WakeRender();           

            while (!shouldQuit)
            {
                timer.Restart();

                Profiler.Begin();

                Time.Tick(timeStep);

                input.snapshot = window.PumpEvents();

                if(resized)
                {
                    resized = false;
                    graphics.Frame();
                    Profiler.End();
                    continue;
                }

                UpdateFrame();

                ApplyFrameLimit();

                Profiler.End();
            }

            graphics.Frame();

            timer.Stop();
            // Flush device to make sure all resources can be freed 
            graphics.WaitIdle();

            window.Destroy();

            Destroy();
        }

        bool started = false;
        private void RenderLoop()
        {
            Graphics.SetRenderThread();

            while (!shouldQuit)
            {
                if (!started)
                {
                    continue;
                }

                Profiler.Begin();
                renderer.Submit();
                Profiler.End();

            }

            graphics.Close();
        }

        void Start()
        {
            timer.Reset();
            timer.Start();

        }

        private void UpdateFrame()
        {
            Profiler.BeginSample("UpdateFrame");

            this.SendGlobalEvent(new BeginFrame
            {
                frameNum = Time.FrameNum,
                timeTotal = Time.Elapsed,
                timeDelta = Time.Delta
            });

            Stats.Tick(timeStep);

            this.SendGlobalEvent(new Update
            {
                timeTotal = Time.Elapsed,
                timeDelta = Time.Delta
            });

            this.SendGlobalEvent(new PostUpdate
            {
                timeTotal = Time.Elapsed,
                timeDelta = Time.Delta
            });

            renderer.Update();

            renderer.Render();

            this.SendGlobalEvent(new EndFrame());

            graphics.Frame();
            Profiler.EndSample();
        }

        private void ApplyFrameLimit()
        {
            uint maxFps = this.maxFps;

            long elapsed = 0;

            if (maxFps > 0)
            {
                long targetMax = 1000000L / maxFps;

                for (; ; )
                {
                    elapsed = timer.ElapsedMicroseconds;
                    if (elapsed >= targetMax)
                        break;

                    // Sleep if 1 ms or more off the frame limiting goal
                    if (targetMax - elapsed >= 1000L)
                    {
                        int sleepTime = (int)((targetMax - elapsed) / 1000L);
                        System.Threading.Thread.Sleep(sleepTime);
                    }
                }
            }


            elapsed = timer.ElapsedMicroseconds;
            elapsedTime += elapsed;
            frameNum++;
            if (elapsedTime >= 1000000L)
            {
                fps = frameNum;
                msec = elapsedTime*0.001f / frameNum;
                frameNum = 0;
                elapsedTime = 0;
            }

            timer.Restart();

            // If FPS lower than minimum, clamp elapsed time
            if (minFps > 0)
            {
                long targetMin = 1000000L / minFps;
                if (elapsed > targetMin)
                    elapsed = targetMin;
            }

            // Perform timestep smoothing
            timeStep = 0.0f;
            
            lastTimeSteps.Add(elapsed / 1000000.0f);
            if (lastTimeSteps.Count > timeStepSmoothing)
            {
                // If the smoothing configuration was changed, ensure correct amount of samples
                lastTimeSteps.RemoveRange(0, lastTimeSteps.Count - timeStepSmoothing);
                for (int i = 0; i < lastTimeSteps.Count; ++i)
                    timeStep += lastTimeSteps[i];
                timeStep /= lastTimeSteps.Count;
            }
            else
                timeStep = lastTimeSteps[lastTimeSteps.Count - 1];
        }

        private void OnWindowResize()
        {
            // Recreate swap chain
            Width = window.Width;
            Height = window.Width;
            //resized = true;

            if(singleLoop)
            {
                graphics.Resize(Width, Height);
            }
            else
            {
                graphics.Execute(() =>
                           {
                               graphics.Resize(Width, Height);

                           });
            }
           

        }

        private void Window_KeyDown(KeyEvent obj)
        {
            input.SetKeyState(obj.Key, obj.Down);
        }

        private void Window_KeyUp(KeyEvent obj)
        {
            input.SetKeyState(obj.Key, obj.Down);
        }

        private void OnWindowClosing()
        {
            if (singleLoop)
            {
                window.Destroy();
            }
            else
            {
                Quit();
            }
        }

    }

}
