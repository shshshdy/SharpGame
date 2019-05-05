using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace SharpGame.Samples
{
    public class Win32Window : IGameWindow
    {
        private readonly string _title;
        private readonly Application _app;
        private Form _form;

        private bool _minimized; // Is the application minimized?
        private bool _maximized; // Is the application maximized?
        private bool _resizing;  // Are the resize bars being dragged?

        private FormWindowState _lastWindowState = FormWindowState.Normal;
        
        public IntPtr WindowHandle => _form.Handle;
        public IntPtr InstanceHandle => Process.GetCurrentProcess().Handle;
        public int Width { get; private set; } = 1280;
        public int Height { get; private set; } = 720;
        public PlatformType Platform => PlatformType.Win32;

        public string Title { get => _form.Text; set => _form.Text = value; }

        public void ProcessEvents() => System.Windows.Forms.Application.DoEvents();
        public void PumpEvents(InputSnapshot inputSnapshot) { }
        public Stream Open(string path) => new FileStream(path, FileMode.Open, FileAccess.Read);

        public Win32Window(string title, Application app)
        {
            _title = title;
            _app = app;

            _form = new Form
            {
                Text = _title,
                FormBorderStyle = FormBorderStyle.Sizable,
                ClientSize = new System.Drawing.Size(Width, Height),
                StartPosition = FormStartPosition.CenterScreen,
                MinimumSize = new System.Drawing.Size(200, 200),
                Visible = false
            };
            _form.ResizeBegin += (sender, e) =>
            {
                _app.Pause();
                _resizing = true;

            };
            _form.ResizeEnd += (sender, e) =>
            {
                _app.Resume();
                _resizing = false;
                _app.Resize();
            };
            _form.Activated += (sender, e) =>
            {
                //_appPaused = _form.WindowState == FormWindowState.Minimized;
                _app.Activate();
            };
            _form.Deactivate += (sender, e) =>
            {
                _app.Deactivate();
            };

            _form.HandleDestroyed += (sender, e) => Application.Quit();
            _form.Resize += (sender, e) =>
            {
                Width = _form.ClientSize.Width;
                Height = _form.ClientSize.Height;
                // When window state changes.
                if (_form.WindowState != _lastWindowState)
                {
                    _lastWindowState = _form.WindowState;
                    if (_form.WindowState == FormWindowState.Maximized)
                    {
                        //_appPaused = false;
                        _minimized = false;
                        _maximized = true;
                        _app.Resize();
                    }
                    else if (_form.WindowState == FormWindowState.Minimized)
                    {
                        //_appPaused = true;
                        _minimized = true;
                        _maximized = false;
                    }
                    else if (_form.WindowState == FormWindowState.Normal)
                    {
                        if (_minimized) // Restoring from minimized state?
                        {
                            //_appPaused = false;
                            _minimized = false;
                            _app.Resize();
                        }
                        else if (_maximized) // Restoring from maximized state?
                        {
                            //_appPaused = false;
                            _maximized = false;
                            _app.Resize();
                        }
                        else if (_resizing)
                        {
                            // If user is dragging the resize bars, we do not resize 
                            // the buffers here because as the user continuously 
                            // drags the resize bars, a stream of WM_SIZE messages are
                            // sent to the window, and it would be pointless (and slow)
                            // to resize for each WM_SIZE message received from dragging
                            // the resize bars. So instead, we reset after the user is 
                            // done resizing the window and releases the resize bars, which 
                            // sends a WM_EXITSIZEMOVE message.
                        }
                        else // API call such as SetWindowPos or setting fullscreen state.
                        {
                            _app.Resize();
                        }
                    }
                }
                else if (!_resizing) // Resize due to snapping.
                {
                    _app.Resize();
                }
            };

        }


        public void Show()
        {            
            _form.Show();
            _form.Update();
        }

        public void Dispose()
        {

        }
        
    }
}
