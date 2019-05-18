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
        public uint width { get; protected set; } = 1280;
        public uint height { get; protected set; } = 720;
        public IntPtr Window { get; protected set; }
        public Sdl2Window NativeWindow { get; private set; }
        public IntPtr WindowInstance { get; protected set; }

        protected Timer timer;
        protected FileSystem fileSystem;
        protected ResourceCache cache;
        protected Graphics graphics;

        protected bool paused = false;
        protected bool prepared;

        protected InputSnapshot snapshot;

        protected bool viewUpdated;

        protected Context context;

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
            NativeWindow = new Sdl2Window(Name, 50, 50, (int)width, (int)height, SDL_WindowFlags.Resizable, threadedProcessing: false)
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
            Setup();

            CreateWindow();

            graphics.CreateSwapchain(NativeWindow.SdlWindowHandle);

            Init();

            RenderLoop();
        }

        public void RenderLoop()
        {
            timer.Start();

            while (NativeWindow.Exists)
            {
                var tStart = DateTime.Now;
                if (viewUpdated)
                {
                    viewUpdated = false;
                    ViewChanged();
                }

                snapshot = NativeWindow.PumpEvents();

                if (!NativeWindow.Exists)
                {
                    // Exit early if the window was closed this frame.
                    break;
                }

                timer.Tick();

                render();
            }

            timer.Stop();
            // Flush device to make sure all resources can be freed 
            graphics.WaitIdle();
        }

        protected virtual void ViewChanged()
        {
        }

        protected virtual void render() { }

        void WindowResize()
        {
            if (!prepared)
            {
                return;
            }

            prepared = false;

            // Recreate swap chain
            width = (uint)NativeWindow.Width;
            height = (uint)NativeWindow.Width;

            graphics.Resize(width, height);

            graphics.WaitIdle();

            // Notify derived class
            WindowResized();
            ViewChanged();

            prepared = true;
        }

        protected virtual void WindowResized()
        {
        }

        protected virtual void BuildCommandBuffers()
        {
        }

    }

}
