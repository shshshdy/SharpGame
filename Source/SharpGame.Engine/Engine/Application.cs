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

namespace SharpGame
{
    public unsafe partial class Application : CoreApplication
    {
        protected static Application instance;
        public static string DataPath => instance.dataPath;

        public string Title { get; set; } = "SharpGame";
        public CString Name { get; set; } = "SharpGame";
        public int width { get; protected set; } = 1280;
        public int height { get; protected set; } = 720;
        public IntPtr Window { get; protected set; }
        public Sdl2Window NativeWindow { get; private set; }
        public IntPtr WindowInstance { get; protected set; }

        protected Timer timer;
        protected FileSystem fileSystem;
        protected ResourceCache cache;
        protected Graphics graphics;
        protected Renderer renderer;
        protected Input input;
        protected bool paused = false;
        protected bool prepared;
        private bool singleThreaded = true;
        private string dataPath;

        private float fps;
        public float Fps => fps;
        private float mspf;
        public float Msec => mspf;
        private long elapsedTime;
        private int frameNum;
        /// Previous timesteps for smoothing.
        List<float> lastTimeSteps_ = new List<float>();
        /// Next frame timestep in seconds.
        float timeStep_;
        /// How many frames to average for the smoothed timestep.
        int timeStepSmoothing_ = 2;
        /// Minimum frames per second.
        uint minFps_ = 10;
        /// Maximum frames per second.
        uint maxFps_ = 2000;
        /// Maximum frames per second when the application does not have input focus.
        uint maxInactiveFps_ = 60;

        public Application(string dataPath)
        {
            instance = this;

            this.dataPath = Path.Combine(AppContext.BaseDirectory, dataPath);
        }

        protected virtual void Setup()
        {
            timer = CreateSubsystem<Timer>();
            fileSystem = CreateSubsystem<FileSystem>();
            cache = CreateSubsystem<ResourceCache>(DataPath);
            CreateWindow();
            graphics = CreateSubsystem<Graphics>();
            graphics.Init(NativeWindow.SdlWindowHandle);
            renderer = CreateSubsystem<Renderer>();
            input = CreateSubsystem<Input>();

        }

        protected virtual void Init()
        {
            CreateSubsystem<GUI>();
        }

        protected virtual void CreateWindow()
        {
            WindowInstance = Process.GetCurrentProcess().SafeHandle.DangerousGetHandle();
            NativeWindow = new Sdl2Window(Name, 50, 50, width, height, SDL_WindowFlags.Resizable, threadedProcessing: false)
            {
                X = 50,
                Y = 50,
                Visible = true
            };

            NativeWindow.Create();

            Window = NativeWindow.Handle;
            NativeWindow.Resized += WindowResize;
        }

        public void Run()
        {
            if (singleThreaded)
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

            while (NativeWindow.Exists)
            {
                Time.Tick(timeStep_);

                input.snapshot = NativeWindow.PumpEvents();

                if (!NativeWindow.Exists)
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

        }

        void WindowResize()
        {
            if (!prepared)
            {
                return;
            }

            prepared = false;

            // Recreate swap chain
            width = NativeWindow.Width;
            height = NativeWindow.Width;

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
            uint maxFps = maxFps_;

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
                mspf = elapsedTime*0.001f / frameNum;
                frameNum = 0;
                elapsedTime = 0;
            }

            timer.Restart();

            // If FPS lower than minimum, clamp elapsed time
            if (minFps_ > 0)
            {
                long targetMin = 1000000L / minFps_;
                if (elapsed > targetMin)
                    elapsed = targetMin;
            }

            // Perform timestep smoothing
            timeStep_ = 0.0f;
            
            lastTimeSteps_.Add(elapsed / 1000000.0f);
            if (lastTimeSteps_.Count > timeStepSmoothing_)
            {
                // If the smoothing configuration was changed, ensure correct amount of samples
                lastTimeSteps_.RemoveRange(0, lastTimeSteps_.Count - timeStepSmoothing_);
                for (int i = 0; i < lastTimeSteps_.Count; ++i)
                    timeStep_ += lastTimeSteps_[i];
                timeStep_ /= lastTimeSteps_.Count;
            }
            else
                timeStep_ = lastTimeSteps_[lastTimeSteps_.Count - 1];
        }

    }

}
