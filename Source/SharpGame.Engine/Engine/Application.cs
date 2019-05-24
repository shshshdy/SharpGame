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
        protected InputSnapshot snapshot;
        private bool singleThreaded = true;
        private string dataPath;
        private int frameNumber;
        private float timeElapsed = 0.0f;

        private float fps;
        public float Fps => fps;
        private float mspf;
        public float Msec => mspf;

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
            NativeWindow = new Sdl2Window(Name, 50, 50, width, height, SDL_WindowFlags.Resizable, threadedProcessing : false)
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
            if(singleThreaded)
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

            while (NativeWindow.Exists)
            {
                var tStart = DateTime.Now;

                snapshot = NativeWindow.PumpEvents();
                input.snapshot = snapshot;

                if (!NativeWindow.Exists)
                {
                    // Exit early if the window was closed this frame.
                    break;
                }

                timer.Tick();

                UpdateFrame();

                renderer.Render();

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
            timer.Tick();
           
            this.SendGlobalEvent(new BeginFrame
            {
                frameNum = frameNumber,
                timeTotal = timer.TotalTime,
                timeDelta = timer.DeltaTime
            });

            this.SendGlobalEvent(new Update
            {
                timeTotal = timer.TotalTime,
                timeDelta = timer.DeltaTime
            });

            this.SendGlobalEvent(new PostUpdate
            {
                timeTotal = timer.TotalTime,
                timeDelta = timer.DeltaTime
            });

            renderer.RenderUpdate();

            this.SendGlobalEvent(new EndFrame());

            CalculateFrameRateStats();

        }

        private void CalculateFrameRateStats()
        {
            frameNumber++;

            if (timer.TotalTime - timeElapsed >= 1.0f)
            {
                fps = frameNumber;
                mspf = 1000.0f / fps;

                // Reset for next average.
                frameNumber = 0;
                timeElapsed += 1.0f;
            }
        }


    }

}
