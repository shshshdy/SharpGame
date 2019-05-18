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

        // Destination dimensions for resizing the window
        private uint destWidth;
        private uint destHeight;
        private bool viewUpdated;
        protected bool paused = false;
        protected bool prepared;

        protected float zoom;
        protected float zoomSpeed = 50f;
        protected Vector3 rotation;
        protected float rotationSpeed = 1f;
        protected Vector3 cameraPos = new Vector3();
        protected Vector2 mousePos;

        protected vkCamera camera = new vkCamera();

        protected VkClearColorValue defaultClearColor => new VkClearColorValue(0.025f, 0.025f, 0.025f, 1.0f);

        protected InputSnapshot snapshot;


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

        private IntPtr CreateWindow()
        {
            WindowInstance = Process.GetCurrentProcess().SafeHandle.DangerousGetHandle();
            NativeWindow = new Sdl2Window(Name, 50, 50, (int)width, (int)height, SDL_WindowFlags.Resizable, threadedProcessing: false)
            {
                X = 50,
                Y = 50,
                Visible = true
            };
            NativeWindow.Resized += OnNativeWindowResized;
            NativeWindow.MouseWheel += OnMouseWheel;
            NativeWindow.MouseMove += OnMouseMove;
            NativeWindow.MouseDown += OnMouseDown;
            NativeWindow.KeyDown += OnKeyDown;
            Window = NativeWindow.Handle;
            return NativeWindow.Handle;
        }

        public void Run()
        {
            Setup();

            CreateWindow();

            graphics.CreateSwapchain(NativeWindow.SdlWindowHandle);

            Init();

            RenderLoop();
        }

        private void OnKeyDown(KeyEvent e)
        {
            keyPressed(e.Key);
        }

        private void OnMouseDown(MouseEvent e)
        {
            if (e.Down)
            {
                mousePos = new Vector2(snapshot.MousePosition.X, snapshot.MousePosition.Y);
            }
        }

        private void OnMouseMove(MouseMoveEventArgs e)
        {
            if (e.State.IsButtonDown(MouseButton.Right))
            {
                int posx = (int)e.MousePosition.X;
                int posy = (int)e.MousePosition.Y;
                zoom += (mousePos.Y - posy) * .005f * zoomSpeed;
                camera.translate(new Vector3(-0.0f, 0.0f, (mousePos.Y - posy) * .005f * zoomSpeed));
                mousePos = new Vector2(posx, posy);
                viewUpdated = true;
            }

            if (e.State.IsButtonDown(MouseButton.Left))
            {
                int posx = (int)e.MousePosition.X;
                int posy = (int)e.MousePosition.Y;
                rotation.X += (mousePos.Y - posy) * 1.25f * rotationSpeed;
                rotation.Y -= (mousePos.X - posx) * 1.25f * rotationSpeed;
                camera.rotate(new Vector3((mousePos.Y - posy) * camera.rotationSpeed, -(mousePos.X - posx) * camera.rotationSpeed, 0.0f));
                mousePos = new Vector2(posx, posy);
                viewUpdated = true;
            }

            if (e.State.IsButtonDown(MouseButton.Middle))
            {
                int posx = (int)e.MousePosition.X;
                int posy = (int)e.MousePosition.Y;
                cameraPos.X -= (mousePos.X - posx) * 0.01f;
                cameraPos.Y -= (mousePos.Y - posy) * 0.01f;
                camera.translate(new Vector3(-(mousePos.X - posx) * 0.01f, -(mousePos.Y - posy) * 0.01f, 0.0f));
                viewUpdated = true;
                mousePos.X = posx;
                mousePos.Y = posy;
            }
        }

        private void OnMouseWheel(MouseWheelEventArgs e)
        {
            var wheelDelta = e.WheelDelta;
            zoom += wheelDelta * 0.005f * zoomSpeed;
            camera.translate(new Vector3(0.0f, 0.0f, wheelDelta * 0.005f * zoomSpeed));
            viewUpdated = true;
        }

        private void OnNativeWindowResized()
        {
            windowResize();
        }

        public void RenderLoop()
        {
            destWidth = width;
            destHeight = height;

            timer.Start();

            while (NativeWindow.Exists)
            {
                var tStart = DateTime.Now;
                if (viewUpdated)
                {
                    viewUpdated = false;
                    viewChanged();
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

        protected virtual void viewChanged()
        {
        }

        protected virtual void render() { }

        void windowResize()
        {
            if (!prepared)
            {
                return;
            }
            prepared = false;

            // Recreate swap chain
            width = destWidth;
            height = destHeight;

            graphics.Resize(destWidth, destHeight);

            graphics.WaitIdle();

            // Notify derived class
            windowResized();
            viewChanged();

            prepared = true;
        }

        protected virtual void windowResized()
        {
        }

        protected virtual void buildCommandBuffers()
        {
        }

        protected virtual void keyPressed(Key key)
        {
        }
    }

}
