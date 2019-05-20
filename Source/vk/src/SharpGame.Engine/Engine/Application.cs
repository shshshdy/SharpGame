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
    public unsafe partial class Application : System<Application>
    {
        public static string DataPath => Path.Combine(AppContext.BaseDirectory, "../../../../../data/");

        public CString Title { get; set; } = "Vulkan Example";
        public CString Name { get; set; } = "VulkanExample";
        public int width { get; protected set; } = 1280;
        public int height { get; protected set; } = 720;
        public IntPtr Window { get; protected set; }
        public Sdl2Window NativeWindow { get; private set; }
        public IntPtr WindowInstance { get; protected set; }

        protected Context context;
        protected Timer timer;
        protected FileSystem fileSystem;
        protected ResourceCache cache;
        protected Graphics graphics;
        protected Renderer renderer;
        protected Input input;
        protected bool paused = false;
        protected bool prepared;

        protected InputSnapshot snapshot;

        bool singleThreaded = true;

        private int frameNumber;
        private float timeElapsed = 0.0f;

        float fps;
        public float Fps => fps;

        public Application()
        {
            context = new Context();
        }

        protected virtual void Setup()
        {
            timer = context.CreateSubsystem<Timer>();
            fileSystem = context.CreateSubsystem<FileSystem>();
            graphics = context.CreateSubsystem<Graphics>();
            cache = context.CreateSubsystem<ResourceCache>(DataPath);
            renderer = context.CreateSubsystem<Renderer>();
            input = context.CreateSubsystem<Input>();
          
        }

        protected virtual void Init()
        {
            context.CreateSubsystem<GUI>();
        }

        protected override void Destroy()
        {
            context.Dispose();
        }

        protected virtual void CreateWindow()
        {
            WindowInstance = Process.GetCurrentProcess().SafeHandle.DangerousGetHandle();
            NativeWindow = new Sdl2Window(Name, 50, 50, (int)width, (int)height, SDL_WindowFlags.Resizable, threadedProcessing : false)
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

            CreateWindow();

            graphics.Init(NativeWindow.SdlWindowHandle);

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

            var beginFrame = new BeginFrame
            {
                frameNum_ = frameNumber,
                timeTotal_ = timer.TotalTime,
                timeDelta_ = timer.DeltaTime
            };

            this.SendGlobalEvent(beginFrame);

            var update = new Update
            {
                timeTotal_ = timer.TotalTime,
                timeDelta_ = timer.DeltaTime
            };

            this.SendGlobalEvent(update);

            var postUpdate = new PostUpdate
            {
                timeTotal_ = timer.TotalTime,
                timeDelta_ = timer.DeltaTime
            };

            this.SendGlobalEvent(postUpdate);

            renderer.RenderUpdate();

            var endFrame = new EndFrame { };

            this.SendGlobalEvent(endFrame);

            CalculateFrameRateStats();

        }

        private void CalculateFrameRateStats()
        {
            frameNumber++;

            if (timer.TotalTime - timeElapsed >= 1.0f)
            {
                fps = frameNumber;
                float mspf = 1000.0f / fps;
                NativeWindow.Title = $"{Name}    Fps: {fps}    Mspf: {mspf}";
               
                // Reset for next average.
                frameNumber = 0;
                timeElapsed += 1.0f;
            }
        }


    }

}
