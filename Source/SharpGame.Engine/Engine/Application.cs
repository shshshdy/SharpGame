using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;
using static Vulkan.VulkanNative;
using System.Numerics;
using System.IO;
using SharpGame.Sdl2;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace SharpGame
{
    public unsafe partial class Application : CoreApplication
    {
        protected static Application instance;
        private string m_title= "SharpGame";
        public string Title
        {
            get => m_title;
            set
            {
                m_title = value;
                if(nativeWindow != null)
                {
                    nativeWindow.Title = value;
                }
            }
        }

        public UTF8String Name { get; set; } = "SharpGame";
        public int Width { get; protected set; } = 1280;
        public int Height { get; protected set; } = 720;

        public Settings Settings { get; } = new Settings();
        public ref Stats Stats => ref graphics.stats;

        protected IntPtr window;
        protected Sdl2Window nativeWindow;
        protected IntPtr windowInstance;

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

        public Application(string dataPath)
        {
            instance = this;
            workSpace = Path.Combine(AppContext.BaseDirectory, dataPath);
        }

        protected virtual void Setup()
        {
            Resource.RegisterAllResType(typeof(Application));

            timer = CreateSubsystem<Timer>();
            fileSystem = CreateSubsystem<FileSystem>(workSpace);
            cache = CreateSubsystem<Resources>();

            cache.RegisterAssertReader(new ShaderReader());

            cache.RegisterAssertReader(new MdlModelReader());
            cache.RegisterAssertReader(new ObjModelReader());
            cache.RegisterAssertReader(new AssimpModelReader());
            
            cache.RegisterAssertReader(new SharpTextureReader());
            //cache.RegisterAssertReader(new KtxTexture2DReader());
            cache.RegisterAssertReader(new KtxTextureReader());

            cache.RegisterAssertReader(new AnimationReader());

            CreateWindow();

            Settings.SingleLoop = singleLoop;
            Settings.ApplicationName = Name;

            graphics = CreateSubsystem<Graphics>(Settings);
            graphics.Init(nativeWindow.SdlWindowHandle);
            renderer = CreateSubsystem<Renderer>();
            input = CreateSubsystem<Input>();

        }

        protected virtual void Init()
        {
            CreateSubsystem<ImGUI>();
        }

        protected virtual void CreateWindow()
        {
            windowInstance = Process.GetCurrentProcess().SafeHandle.DangerousGetHandle();
            nativeWindow = new Sdl2Window(Title, 50, 50, Width, Height, SDL_WindowFlags.Resizable, threadedProcessing: false)
            {
                X = 50,
                Y = 50,
                Visible = true
            };

            nativeWindow.Create();

            window = nativeWindow.Handle;
            nativeWindow.Resized += OnWindowResize;
            nativeWindow.Closing += OnWindowClosing;
        }

        public static void Quit()
        {
            instance.shouldQuit = true;
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

            while (nativeWindow.Exists)
            {
                Profiler.Begin();
                Time.Tick(timeStep);
                Stats.Tick(timeStep);

                input.snapshot = nativeWindow.PumpEvents();

                if (!nativeWindow.Exists)
                {
                    // Exit early if the window was closed this frame.
                    break;
                }
               
                UpdateFrame();

                renderer.Render();

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
            graphics.WakeRender();
            graphics.Frame();

            while (!shouldQuit)
            {
                timer.Restart();

                Profiler.Begin();

                Time.Tick(timeStep);

                input.snapshot = nativeWindow.PumpEvents();

                UpdateFrame();

                ApplyFrameLimit();

                Profiler.End();
            }

            graphics.Frame();

            timer.Stop();
            // Flush device to make sure all resources can be freed 
            graphics.WaitIdle();

            nativeWindow.Destroy();

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
                renderer.Render();
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

            renderer.RenderUpdate();

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
            Width = nativeWindow.Width;
            Height = nativeWindow.Width;

            graphics.Execute(() =>
            {
                graphics.Resize(Width, Height);

            });

        }

        private void OnWindowClosing()
        {
            if (singleLoop)
            {
                nativeWindow.Destroy();
            }
            else
            {
                Quit();
            }
        }

    }

}
