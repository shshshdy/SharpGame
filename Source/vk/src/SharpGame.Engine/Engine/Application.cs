using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;
using static Vulkan.VulkanNative;
using System.Numerics;
using System.IO;
using Veldrid;
using Veldrid.Sdl2;

namespace SharpGame
{
    public unsafe partial class Application : System<Application>
    {
        public static string DataPath => Path.Combine(AppContext.BaseDirectory, "data/");

        public FixedUtf8String Title { get; set; } = "Vulkan Example";
        public FixedUtf8String Name { get; set; } = "VulkanExample";
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

        protected bool paused = false;
        protected bool prepared;

        protected InputSnapshot snapshot;

        private int frameNumber;
        private float timeElapsed = 0.0f;
        bool singleThreaded = true;

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
        }

        public virtual void Init()
        {
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

            graphics.CreateSwapchain(NativeWindow.SdlWindowHandle);

            Init();

            timer.Reset();

            while (NativeWindow.Exists)
            {
                var tStart = DateTime.Now;

                snapshot = NativeWindow.PumpEvents();

                if (!NativeWindow.Exists)
                {
                    // Exit early if the window was closed this frame.
                    break;
                }

                timer.Tick();

                UpdateFrame();

                Render();
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

            //renderer.RenderUpdate();

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
                NativeWindow.Title = $"{Name}    Fps: {fps}    Mspf: {mspf}";
               
                // Reset for next average.
                frameNumber = 0;
                timeElapsed += 1.0f;
            }
        }

        protected virtual void Update()
        {
        }

        protected virtual void Render()
        {
        }

        protected virtual void BuildCommandBuffers()
        {
        }
    }

}
