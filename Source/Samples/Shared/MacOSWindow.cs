using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SharpGame.Samples
{
    public class MacOSWindow : IGameWindow
    {
        private readonly string _title;
        private readonly Application _app;

        private NativeMacOS.NativeApp _nativeApp;
        private NativeMacOS.NativeWindow _nativeWindow;
        private NativeMacOS.NativeMetalView _nativeMetalView;
        

        public IntPtr WindowHandle => _nativeMetalView.NativeMetalViewPointer;
        public IntPtr InstanceHandle => Process.GetCurrentProcess().Handle;
        public int Width { get; private set; } = 1280;
        public int Height { get; private set; } = 720;
        public PlatformType Platform => PlatformType.MacOS;

        public string Title { get => _nativeWindow.Title; set => _nativeWindow.Title = value; }

        public void ProcessEvents() => _nativeApp.ProcessEvents();
        public void PumpEvents(InputSnapshot inputSnapshot) { }

        public Stream Open(string path) => new FileStream(Path.Combine("bin", path), FileMode.Open, FileAccess.Read);

        public MacOSWindow(string title, Application app)
        {
            _title = title;
            _app = app;
            _nativeApp = new NativeMacOS.NativeApp();
            _nativeWindow = new NativeMacOS.NativeWindow(_nativeApp, new NativeMacOS.Size(Width, Height));
            _nativeWindow.MinSize = new NativeMacOS.Size(200f, 200f);
            _nativeWindow.Title = _title;

            _nativeWindow.BeginResizing += () =>
            {
                _app.Pause();
            };

            _nativeWindow.EndResizing += () =>
            {
                _app.Resume();
                _app.Resize();
            };

            _nativeWindow.Resized += size =>
            {
                Width = (int)size.Width;
                Height = (int)size.Height;
            };

            _nativeWindow.CloseRequested += () =>
            {
                Application.Quit();
            };

            _nativeMetalView = new NativeMacOS.NativeMetalView(_nativeWindow);

        }

        public void Show()
        {           
        }

        public void Dispose()
        {
            _nativeWindow.Dispose();
            _nativeApp.Dispose();
        }


    }
}
