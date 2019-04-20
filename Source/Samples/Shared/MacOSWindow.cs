using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SharpGame.Samples
{
    public class MacOSWindow : IPlatform
    {
        private readonly string _title;
        private readonly Application _app;

        private NativeMacOS.NativeApp _nativeApp;
        private NativeMacOS.NativeWindow _nativeWindow;
        private NativeMacOS.NativeMetalView _nativeMetalView;

        private bool _appPaused;

        public MacOSWindow(string title, Application app)
        {
            _title = title;
            _app = app;
        }

        public IntPtr WindowHandle => _nativeMetalView.NativeMetalViewPointer;
        public IntPtr InstanceHandle => Process.GetCurrentProcess().Handle;
        public int Width { get; private set; } = 1280;
        public int Height { get; private set; } = 720;
        public PlatformType Platform => PlatformType.MacOS;

        public string Tittle { get => _nativeWindow.Title; set => _nativeWindow.Title = value; }

        public void ProcessEvents() => _nativeApp.ProcessEvents();
        public Stream Open(string path) => new FileStream(Path.Combine("bin", path), FileMode.Open, FileAccess.Read);

        public void Initialize()
        {
            _nativeApp = new NativeMacOS.NativeApp();
            _nativeWindow = new NativeMacOS.NativeWindow(_nativeApp, new NativeMacOS.Size(Width, Height));
            _nativeWindow.MinSize = new NativeMacOS.Size(200f, 200f);
            _nativeWindow.Title = _title;
            _nativeWindow.BeginResizing += () =>
            {
                _appPaused = true;
                //_timer.Stop();
            };
            _nativeWindow.EndResizing += () =>
            {
                _appPaused = false;
                //_timer.Start();
                _app.Resize();
            };
            _nativeWindow.Resized += size =>
            {
                Width = (int)size.Width;
                Height = (int)size.Height;
            };
            _nativeWindow.CloseRequested += () =>
            {
                _app.Quit();
            };
            _nativeMetalView = new NativeMacOS.NativeMetalView(_nativeWindow);

            _app.Initialize(this);
            //_running = true;
            //_timer.Start();
        }

        public void Run()
        {
            Initialize();

            _app.Run();
        }

        public void Dispose()
        {
            _app.Dispose();
            _nativeWindow.Dispose();
            _nativeApp.Dispose();
        }
        
    }
}
