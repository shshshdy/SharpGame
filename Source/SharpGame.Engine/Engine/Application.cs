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

        public string Title { get; set; } = "SharpGame";
        public CString Name { get; set; } = "SharpGame";
        public int width { get; protected set; } = 1280;
        public int height { get; protected set; } = 720;

        public Settings Settings { get; } = new Settings();

        protected IntPtr window;
        protected Sdl2Window nativeWindow;
        protected IntPtr windowInstance;

        protected Timer timer;
        protected FileSystem fileSystem;
        protected Resources cache;
        protected Graphics graphics;
        protected Renderer renderer;
        protected Input input;
        protected bool paused = false;
        protected bool prepared;
        protected bool singleLoop = false;
        protected string workSpace;

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
        private uint maxFps = 2000;
        private bool shouldQuit = false;
        private bool mainThreadRender = false;
        public Application(string dataPath)
        {
            instance = this;
            workSpace = Path.Combine(AppContext.BaseDirectory, dataPath);
        }

        protected virtual void Setup()
        {
            timer = CreateSubsystem<Timer>();
            fileSystem = CreateSubsystem<FileSystem>(workSpace);
            cache = CreateSubsystem<Resources>();

            cache.RegisterAssertReader(new ShaderReader());

            cache.RegisterAssertReader(new MdlModelReader());
            cache.RegisterAssertReader(new AssimpModelReader());
            cache.RegisterAssertReader(new ObjModelReader());
            
            cache.RegisterAssertReader(new SharpTextureReader());
            cache.RegisterAssertReader(new KtxTextureReader());

            cache.RegisterAssertReader(new AnimationReader());

            CreateWindow();

            Settings.SingleLoop = singleLoop;

            graphics = CreateSubsystem<Graphics>(Settings);
            graphics.Init(nativeWindow.SdlWindowHandle);
            renderer = CreateSubsystem<Renderer>();
            input = CreateSubsystem<Input>();

        }

        protected virtual void Init()
        {
            CreateSubsystem<GUI>();
        }

        protected virtual void CreateWindow()
        {
            windowInstance = Process.GetCurrentProcess().SafeHandle.DangerousGetHandle();
            nativeWindow = new Sdl2Window(Name, 50, 50, width, height, SDL_WindowFlags.Resizable, threadedProcessing: false)
            {
                X = 50,
                Y = 50,
                Visible = true
            };

            nativeWindow.Create();

            window = nativeWindow.Handle;
            nativeWindow.Resized += WindowResize;
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
            Setup();

            Init();

            timer.Reset();
            timer.Start();

            while (nativeWindow.Exists)
            {
                Time.Tick(timeStep);
                
                input.snapshot = nativeWindow.PumpEvents();

                if (!nativeWindow.Exists)
                {
                    // Exit early if the window was closed this frame.
                    break;
                }
               
                UpdateFrame();

                renderer.Render();

                ApplyFrameLimit();
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

        void SimulateLoop()
        {
            Setup();

            Init();

            timer.Reset();
            timer.Start();

            graphics.FrameNoRenderWait();
            graphics.Frame();

            while (nativeWindow.Exists)
            {
                Time.Tick(timeStep);
                
                input.snapshot = nativeWindow.PumpEvents();

                if (!nativeWindow.Exists)
                {
                    // Exit early if the window was closed this frame.
                    break;
                }

                UpdateFrame();
                
                graphics.Frame();
                
                ApplyFrameLimit();
            }

            graphics.Frame();
            timer.Stop();
            // Flush device to make sure all resources can be freed 
            graphics.WaitIdle();

            Destroy();
        }

        void RenderLoop()
        {
            while (!shouldQuit)
            {
                if (nativeWindow == null || renderer == null)
                {
                    continue;
                }

                renderer.Render();

            }

            graphics.Close();
        }

        void WindowResize()
        {
            if (!prepared)
            {
                return;
            }

            prepared = false;

            // Recreate swap chain
            width = nativeWindow.Width;
            height = nativeWindow.Width;

            graphics.Resize(width, height);

            graphics.WaitIdle();

            prepared = true;
        }

        void UpdateFrame()
        {
            this.SendGlobalEvent(new BeginFrame
            {
                frameNum = Time.FrameNum,
                timeTotal = Time.Elapsed,
                timeDelta = Time.Delta
            });

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

        }

        void ApplyFrameLimit()
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

    }

}
