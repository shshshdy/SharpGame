using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;

using static SharpGame.Sdl2.Sdl2Native;
using System.ComponentModel;
using System.Collections.Concurrent;
using SharpGame;

namespace SharpGame.Sdl2
{
    public unsafe class SdlGameWindow : GameWindow
    {
        private IntPtr _window;       
        // Current input states
        private int _currentMouseX;
        private int _currentMouseY;
        private bool[] _currentMouseButtonStates = new bool[5];

        ConcurrentQueue<SDL_Event> coreEventQueue_ = new ConcurrentQueue<SDL_Event>();

        public Vector2 ScaleFactor => Vector2.One;

        public bool CursorVisible { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool Focused => (SDL_GetWindowFlags(_window) & SDL_WindowFlags.InputFocus) != 0;

        public IntPtr SdlWindowHandle => _window;

        public SdlGameWindow(string title, int x, int y, int width, int height, SDL_WindowFlags flags)
        {
            _window = SDL_CreateWindow(title, x, y, width, height, flags);

        }

        public int X { get => GetWindowPosition().X; set => SetWindowPosition(value, Y); }
        public int Y { get => GetWindowPosition().X; set => SetWindowPosition(X, value); }

        public override int Width
        {
            get => GetWindowSize().X;
            protected set => SetWindowSize(value,
                Height);
        }

        public override int Height
        {
            get => GetWindowSize().Y;
            protected set => SetWindowSize(Width, value);
        }

        public override IntPtr Handle => GetUnderlyingWindowHandle();

        public string Title
        {
            get => SDL_GetWindowTitle(_window);
            set => SDL_SetWindowTitle(_window, value);
        }

        public WindowState WindowState
        {
            get
            {
                SDL_WindowFlags flags = SDL_GetWindowFlags(_window);
                if (((flags & SDL_WindowFlags.FullScreenDesktop) == SDL_WindowFlags.FullScreenDesktop)
                    || ((flags & (SDL_WindowFlags.Borderless | SDL_WindowFlags.Fullscreen)) == (SDL_WindowFlags.Borderless | SDL_WindowFlags.Fullscreen)))
                {
                    return WindowState.BorderlessFullScreen;
                }
                else if ((flags & SDL_WindowFlags.Minimized) == SDL_WindowFlags.Minimized)
                {
                    return WindowState.Minimized;
                }
                else if ((flags & SDL_WindowFlags.Fullscreen) == SDL_WindowFlags.Fullscreen)
                {
                    return WindowState.FullScreen;
                }
                else if ((flags & SDL_WindowFlags.Maximized) == SDL_WindowFlags.Maximized)
                {
                    return WindowState.Maximized;
                }

                return WindowState.Normal;
            }

            set
            {
                switch (value)
                {
                    case WindowState.Normal:
                        SDL_SetWindowFullscreen(_window, SDL_FullscreenMode.Windowed);
                        break;
                    case WindowState.FullScreen:
                        SDL_SetWindowFullscreen(_window, SDL_FullscreenMode.Fullscreen);
                        break;
                    case WindowState.Maximized:
                        SDL_MaximizeWindow(_window);
                        break;
                    case WindowState.Minimized:
                        SDL_MinimizeWindow(_window);
                        break;
                    case WindowState.BorderlessFullScreen:
                        SDL_SetWindowFullscreen(_window, SDL_FullscreenMode.FullScreenDesktop);
                        break;
                    default:
                        throw new InvalidOperationException("Illegal WindowState value: " + value);
                }
            }
        }
        
        public bool Visible
        {
            get => (SDL_GetWindowFlags(_window) & SDL_WindowFlags.Shown) != 0;
            set
            {
                if (value)
                {
                    SDL_ShowWindow(_window);
                }
                else
                {
                    SDL_HideWindow(_window);
                }
            }
        }

        public Int2 ClientToScreen(Int2 p)
        {
            Int2 position = GetWindowPosition();
            return new Int2(p.X + position.X, p.Y + position.Y);
        }

        public override void Close()
        {       
            CloseCore();           
        }

        private void CloseCore()
        {
            RaiseClosing();
            SDL_DestroyWindow(_window);
            RaiseClosed();
        }
        
        void HandleEvent(SDL_Event ev)
        {
            switch(ev.type)
            {
                case SDL_EventType.Quit:
                //Application.Quit();
                //Close();
                break;
                case SDL_EventType.Terminating:
                //Close();
                //Application.Quit();
                break;
                case SDL_EventType.WindowEvent:
                SDL_WindowEvent windowEvent = Unsafe.Read<SDL_WindowEvent>(&ev);
                HandleWindowEvent(windowEvent);
                break;
                case SDL_EventType.KeyDown:
                case SDL_EventType.KeyUp:
                SDL_KeyboardEvent keyboardEvent = Unsafe.Read<SDL_KeyboardEvent>(&ev);
                HandleKeyboardEvent(keyboardEvent);
                break;
                case SDL_EventType.TextEditing:
                break;
                case SDL_EventType.TextInput:
                SDL_TextInputEvent textInputEvent = Unsafe.Read<SDL_TextInputEvent>(&ev);
                HandleTextInputEvent(textInputEvent);
                break;
                case SDL_EventType.KeyMapChanged:
                break;
                case SDL_EventType.MouseMotion:
                SDL_MouseMotionEvent mouseMotionEvent = Unsafe.Read<SDL_MouseMotionEvent>(&ev);
                HandleMouseMotionEvent(mouseMotionEvent);
                break;
                case SDL_EventType.MouseButtonDown:
                case SDL_EventType.MouseButtonUp:
                SDL_MouseButtonEvent mouseButtonEvent = Unsafe.Read<SDL_MouseButtonEvent>(&ev);
                HandleMouseButtonEvent(mouseButtonEvent);
                break;
                case SDL_EventType.MouseWheel:
                SDL_MouseWheelEvent mouseWheelEvent = Unsafe.Read<SDL_MouseWheelEvent>(&ev);
                HandleMouseWheelEvent(mouseWheelEvent);
                break;
                case SDL_EventType.JoyAxisMotion:
                break;
                case SDL_EventType.JoyBallMotion:
                break;
                case SDL_EventType.JoyHatMotion:
                break;
                case SDL_EventType.JoyButtonDown:
                break;
                case SDL_EventType.JoyButtonUp:
                break;
                case SDL_EventType.JoyDeviceAdded:
                break;
                case SDL_EventType.JoyDeviceRemoved:
                break;
                case SDL_EventType.ControllerAxisMotion:
                break;
                case SDL_EventType.ControllerButtonDown:
                break;
                case SDL_EventType.ControllerButtonUp:
                break;
                case SDL_EventType.ControllerDeviceAdded:
                break;
                case SDL_EventType.ControllerDeviceRemoved:
                break;
                case SDL_EventType.ControllerDeviceRemapped:
                break;
                case SDL_EventType.FingerDown:
                break;
                case SDL_EventType.FingerUp:
                break;
                case SDL_EventType.FingerMotion:
                break;
                case SDL_EventType.DollarGesture:
                break;
                case SDL_EventType.DollarRecord:
                break;
                case SDL_EventType.MultiGesture:
                break;
                case SDL_EventType.ClipboardUpdate:
                break;
                case SDL_EventType.DropFile:
                break;
                case SDL_EventType.DropTest:
                break;
                case SDL_EventType.DropBegin:
                break;
                case SDL_EventType.DropComplete:
                break;
                case SDL_EventType.AudioDeviceAdded:
                break;
                case SDL_EventType.AudioDeviceRemoved:
                break;
                case SDL_EventType.RenderTargetsReset:
                break;
                case SDL_EventType.RenderDeviceReset:
                break;
                case SDL_EventType.UserEvent:
                break;
                case SDL_EventType.LastEvent:
                break;
                default:
                // Ignore
                break;
            }
        }

        public override void RunMessageLoop()
        {
            SDL_PumpEvents();

            SDL_Event ev;

            while(SDL_PollEvent(&ev) != 0)
            {
                coreEventQueue_.Enqueue(ev);
            }
                
        }

        public SDL_Event? Poll()
        {
            SDL_Event ev;
            if(coreEventQueue_.TryDequeue(out ev))
                return ev;

            return null;
        }

        public override void ProcessEvents()
        {
            SDL_Event? ev;

            while((ev = Poll()) != null)
            {
                HandleEvent(ev.Value);
            }
            
        }

        private void HandleTextInputEvent(SDL_TextInputEvent textInputEvent)
        {
            uint byteCount = 0;
            // Loop until the null terminator is found or the max size is reached.
            while (byteCount < SDL_TextInputEvent.MaxTextSize && textInputEvent.text[byteCount++] != 0)
            {
            }

            Input input = Get<Input>();

            if (byteCount > 1)
            {
                // We don't want the null terminator.
                byteCount -= 1;
                int charCount = Encoding.UTF8.GetCharCount(textInputEvent.text, (int)byteCount);
                char* charsPtr = stackalloc char[charCount];
                Encoding.UTF8.GetChars(textInputEvent.text, (int)byteCount, charsPtr, charCount);
                for (int i = 0; i < charCount; i++)
                {
                    input.InjectTextInput(charsPtr[i]);
                }
            }
        }

        private void HandleMouseWheelEvent(SDL_MouseWheelEvent mouseWheelEvent)
        {
            Input input = Get<Input>();
            input.WheelDelta += mouseWheelEvent.y;
            input.InjectMouseWheel(new MouseWheelEvent(GetCurrentMouseState(), (float)mouseWheelEvent.y));
        }

        private void HandleMouseButtonEvent(SDL_MouseButtonEvent mouseButtonEvent)
        {
            Input input = Get<Input>();
            MouseButton button = MapMouseButton(mouseButtonEvent.button);
            bool down = mouseButtonEvent.state == 1;

            _currentMouseButtonStates[(int)button] = down;

            MouseEvent mouseEvent = new MouseEvent(button, down);

            if (down)
            {
                input.InjectMouseDown(mouseEvent);
            }
            else
            {
                input.InjectMouseUp(mouseEvent);
            }
        }

        private MouseButton MapMouseButton(SDL_MouseButton button)
        {
            switch (button)
            {
                case SDL_MouseButton.Left:
                    return MouseButton.Left;
                case SDL_MouseButton.Middle:
                    return MouseButton.Middle;
                case SDL_MouseButton.Right:
                    return MouseButton.Right;
                case SDL_MouseButton.X1:
                    return MouseButton.Button1;
                case SDL_MouseButton.X2:
                    return MouseButton.Button2;
                default:
                    return MouseButton.Left;
            }
        }

        private void HandleMouseMotionEvent(SDL_MouseMotionEvent mouseMotionEvent)
        {
            Vector2 mousePos = new Vector2(mouseMotionEvent.x, mouseMotionEvent.y);
            _currentMouseX = (int)mousePos.X;
            _currentMouseY = (int)mousePos.Y;

            Input input = Get<Input>();
            input.InjectMouseMove(new MouseMoveEvent(GetCurrentMouseState(), mousePos));
        }

        private void HandleKeyboardEvent(SDL_KeyboardEvent keyboardEvent)
        {
            KeyEvent keyEvent = new KeyEvent(MapKey(keyboardEvent.keysym), keyboardEvent.state == 1, MapModifierKeys(keyboardEvent.keysym.mod));

            Input input = Get<Input>();
            if (keyboardEvent.state == 1)
            {
                input.InjectKeyDown(keyEvent);
            }
            else
            {
                input.InjectKeyUp(keyEvent);
            }
        }

        private Key MapKey(SDL_Keysym keysym)
        {
            switch (keysym.scancode)
            {
                case SDL_Scancode.SDL_SCANCODE_A:
                    return Key.A;
                case SDL_Scancode.SDL_SCANCODE_B:
                    return Key.B;
                case SDL_Scancode.SDL_SCANCODE_C:
                    return Key.C;
                case SDL_Scancode.SDL_SCANCODE_D:
                    return Key.D;
                case SDL_Scancode.SDL_SCANCODE_E:
                    return Key.E;
                case SDL_Scancode.SDL_SCANCODE_F:
                    return Key.F;
                case SDL_Scancode.SDL_SCANCODE_G:
                    return Key.G;
                case SDL_Scancode.SDL_SCANCODE_H:
                    return Key.H;
                case SDL_Scancode.SDL_SCANCODE_I:
                    return Key.I;
                case SDL_Scancode.SDL_SCANCODE_J:
                    return Key.J;
                case SDL_Scancode.SDL_SCANCODE_K:
                    return Key.K;
                case SDL_Scancode.SDL_SCANCODE_L:
                    return Key.L;
                case SDL_Scancode.SDL_SCANCODE_M:
                    return Key.M;
                case SDL_Scancode.SDL_SCANCODE_N:
                    return Key.N;
                case SDL_Scancode.SDL_SCANCODE_O:
                    return Key.O;
                case SDL_Scancode.SDL_SCANCODE_P:
                    return Key.P;
                case SDL_Scancode.SDL_SCANCODE_Q:
                    return Key.Q;
                case SDL_Scancode.SDL_SCANCODE_R:
                    return Key.R;
                case SDL_Scancode.SDL_SCANCODE_S:
                    return Key.S;
                case SDL_Scancode.SDL_SCANCODE_T:
                    return Key.T;
                case SDL_Scancode.SDL_SCANCODE_U:
                    return Key.U;
                case SDL_Scancode.SDL_SCANCODE_V:
                    return Key.V;
                case SDL_Scancode.SDL_SCANCODE_W:
                    return Key.W;
                case SDL_Scancode.SDL_SCANCODE_X:
                    return Key.X;
                case SDL_Scancode.SDL_SCANCODE_Y:
                    return Key.Y;
                case SDL_Scancode.SDL_SCANCODE_Z:
                    return Key.Z;
                case SDL_Scancode.SDL_SCANCODE_1:
                    return Key.Number1;
                case SDL_Scancode.SDL_SCANCODE_2:
                    return Key.Number2;
                case SDL_Scancode.SDL_SCANCODE_3:
                    return Key.Number3;
                case SDL_Scancode.SDL_SCANCODE_4:
                    return Key.Number4;
                case SDL_Scancode.SDL_SCANCODE_5:
                    return Key.Number5;
                case SDL_Scancode.SDL_SCANCODE_6:
                    return Key.Number6;
                case SDL_Scancode.SDL_SCANCODE_7:
                    return Key.Number7;
                case SDL_Scancode.SDL_SCANCODE_8:
                    return Key.Number8;
                case SDL_Scancode.SDL_SCANCODE_9:
                    return Key.Number9;
                case SDL_Scancode.SDL_SCANCODE_0:
                    return Key.Number0;
                case SDL_Scancode.SDL_SCANCODE_RETURN:
                    return Key.Enter;
                case SDL_Scancode.SDL_SCANCODE_ESCAPE:
                    return Key.Escape;
                case SDL_Scancode.SDL_SCANCODE_BACKSPACE:
                    return Key.BackSpace;
                case SDL_Scancode.SDL_SCANCODE_TAB:
                    return Key.Tab;
                case SDL_Scancode.SDL_SCANCODE_SPACE:
                    return Key.Space;
                case SDL_Scancode.SDL_SCANCODE_MINUS:
                    return Key.Minus;
                case SDL_Scancode.SDL_SCANCODE_EQUALS:
                    return Key.Plus;
                case SDL_Scancode.SDL_SCANCODE_LEFTBRACKET:
                    return Key.BracketLeft;
                case SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET:
                    return Key.BracketRight;
                case SDL_Scancode.SDL_SCANCODE_BACKSLASH:
                    return Key.BackSlash;
                case SDL_Scancode.SDL_SCANCODE_SEMICOLON:
                    return Key.Semicolon;
                case SDL_Scancode.SDL_SCANCODE_APOSTROPHE:
                    return Key.Quote;
                case SDL_Scancode.SDL_SCANCODE_GRAVE:
                    return Key.Grave;
                case SDL_Scancode.SDL_SCANCODE_COMMA:
                    return Key.Comma;
                case SDL_Scancode.SDL_SCANCODE_PERIOD:
                    return Key.Period;
                case SDL_Scancode.SDL_SCANCODE_SLASH:
                    return Key.Slash;
                case SDL_Scancode.SDL_SCANCODE_CAPSLOCK:
                    return Key.CapsLock;
                case SDL_Scancode.SDL_SCANCODE_F1:
                    return Key.F1;
                case SDL_Scancode.SDL_SCANCODE_F2:
                    return Key.F2;
                case SDL_Scancode.SDL_SCANCODE_F3:
                    return Key.F3;
                case SDL_Scancode.SDL_SCANCODE_F4:
                    return Key.F4;
                case SDL_Scancode.SDL_SCANCODE_F5:
                    return Key.F5;
                case SDL_Scancode.SDL_SCANCODE_F6:
                    return Key.F6;
                case SDL_Scancode.SDL_SCANCODE_F7:
                    return Key.F7;
                case SDL_Scancode.SDL_SCANCODE_F8:
                    return Key.F8;
                case SDL_Scancode.SDL_SCANCODE_F9:
                    return Key.F9;
                case SDL_Scancode.SDL_SCANCODE_F10:
                    return Key.F10;
                case SDL_Scancode.SDL_SCANCODE_F11:
                    return Key.F11;
                case SDL_Scancode.SDL_SCANCODE_F12:
                    return Key.F12;
                case SDL_Scancode.SDL_SCANCODE_PRINTSCREEN:
                    return Key.PrintScreen;
                case SDL_Scancode.SDL_SCANCODE_SCROLLLOCK:
                    return Key.ScrollLock;
                case SDL_Scancode.SDL_SCANCODE_PAUSE:
                    return Key.Pause;
                case SDL_Scancode.SDL_SCANCODE_INSERT:
                    return Key.Insert;
                case SDL_Scancode.SDL_SCANCODE_HOME:
                    return Key.Home;
                case SDL_Scancode.SDL_SCANCODE_PAGEUP:
                    return Key.PageUp;
                case SDL_Scancode.SDL_SCANCODE_DELETE:
                    return Key.Delete;
                case SDL_Scancode.SDL_SCANCODE_END:
                    return Key.End;
                case SDL_Scancode.SDL_SCANCODE_PAGEDOWN:
                    return Key.PageDown;
                case SDL_Scancode.SDL_SCANCODE_RIGHT:
                    return Key.Right;
                case SDL_Scancode.SDL_SCANCODE_LEFT:
                    return Key.Left;
                case SDL_Scancode.SDL_SCANCODE_DOWN:
                    return Key.Down;
                case SDL_Scancode.SDL_SCANCODE_UP:
                    return Key.Up;
                case SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR:
                    return Key.NumLock;
                case SDL_Scancode.SDL_SCANCODE_KP_DIVIDE:
                    return Key.KeypadDivide;
                case SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY:
                    return Key.KeypadMultiply;
                case SDL_Scancode.SDL_SCANCODE_KP_MINUS:
                    return Key.KeypadMinus;
                case SDL_Scancode.SDL_SCANCODE_KP_PLUS:
                    return Key.KeypadPlus;
                case SDL_Scancode.SDL_SCANCODE_KP_ENTER:
                    return Key.KeypadEnter;
                case SDL_Scancode.SDL_SCANCODE_KP_1:
                    return Key.Keypad1;
                case SDL_Scancode.SDL_SCANCODE_KP_2:
                    return Key.Keypad2;
                case SDL_Scancode.SDL_SCANCODE_KP_3:
                    return Key.Keypad3;
                case SDL_Scancode.SDL_SCANCODE_KP_4:
                    return Key.Keypad4;
                case SDL_Scancode.SDL_SCANCODE_KP_5:
                    return Key.Keypad5;
                case SDL_Scancode.SDL_SCANCODE_KP_6:
                    return Key.Keypad6;
                case SDL_Scancode.SDL_SCANCODE_KP_7:
                    return Key.Keypad7;
                case SDL_Scancode.SDL_SCANCODE_KP_8:
                    return Key.Keypad8;
                case SDL_Scancode.SDL_SCANCODE_KP_9:
                    return Key.Keypad9;
                case SDL_Scancode.SDL_SCANCODE_KP_0:
                    return Key.Keypad0;
                case SDL_Scancode.SDL_SCANCODE_KP_PERIOD:
                    return Key.KeypadPeriod;
                case SDL_Scancode.SDL_SCANCODE_NONUSBACKSLASH:
                    return Key.NonUSBackSlash;
                case SDL_Scancode.SDL_SCANCODE_KP_EQUALS:
                    return Key.KeypadPlus;
                case SDL_Scancode.SDL_SCANCODE_F13:
                    return Key.F13;
                case SDL_Scancode.SDL_SCANCODE_F14:
                    return Key.F14;
                case SDL_Scancode.SDL_SCANCODE_F15:
                    return Key.F15;
                case SDL_Scancode.SDL_SCANCODE_F16:
                    return Key.F16;
                case SDL_Scancode.SDL_SCANCODE_F17:
                    return Key.F17;
                case SDL_Scancode.SDL_SCANCODE_F18:
                    return Key.F18;
                case SDL_Scancode.SDL_SCANCODE_F19:
                    return Key.F19;
                case SDL_Scancode.SDL_SCANCODE_F20:
                    return Key.F20;
                case SDL_Scancode.SDL_SCANCODE_F21:
                    return Key.F21;
                case SDL_Scancode.SDL_SCANCODE_F22:
                    return Key.F22;
                case SDL_Scancode.SDL_SCANCODE_F23:
                    return Key.F23;
                case SDL_Scancode.SDL_SCANCODE_F24:
                    return Key.F24;
                case SDL_Scancode.SDL_SCANCODE_MENU:
                    return Key.Menu;
                case SDL_Scancode.SDL_SCANCODE_LCTRL:
                    return Key.ControlLeft;
                case SDL_Scancode.SDL_SCANCODE_LSHIFT:
                    return Key.ShiftLeft;
                case SDL_Scancode.SDL_SCANCODE_LALT:
                    return Key.AltLeft;
                case SDL_Scancode.SDL_SCANCODE_RCTRL:
                    return Key.ControlRight;
                case SDL_Scancode.SDL_SCANCODE_RSHIFT:
                    return Key.ShiftRight;
                case SDL_Scancode.SDL_SCANCODE_RALT:
                    return Key.AltRight;
                default:
                    return Key.Unknown;
            }
        }

        private ModifierKeys MapModifierKeys(SDL_Keymod mod)
        {
            ModifierKeys mods = ModifierKeys.None;
            if ((mod & (SDL_Keymod.LeftShift | SDL_Keymod.RightShift)) != 0)
            {
                mods |= ModifierKeys.Shift;
            }
            if ((mod & (SDL_Keymod.LeftAlt | SDL_Keymod.RightAlt)) != 0)
            {
                mods |= ModifierKeys.Alt;
            }
            if ((mod & (SDL_Keymod.LeftControl | SDL_Keymod.RightControl)) != 0)
            {
                mods |= ModifierKeys.Control;
            }

            return mods;
        }

        private void HandleWindowEvent(SDL_WindowEvent windowEvent)
        {
            switch (windowEvent.@event)
            {
                case SDL_WindowEventID.Resized:
                case SDL_WindowEventID.SizeChanged:
                case SDL_WindowEventID.Minimized:
                case SDL_WindowEventID.Maximized:
                case SDL_WindowEventID.Restored:
                    RaiseResized();
                    break;
                case SDL_WindowEventID.FocusGained:
                    RaiseFocusGained();
                    break;
                case SDL_WindowEventID.FocusLost:
                    RaiseFocusLost();
                    break;
                case SDL_WindowEventID.Close:
                    Close();
                    break;
                case SDL_WindowEventID.Shown:
                    RaiseShown();
                    break;
                case SDL_WindowEventID.Hidden:
                    RaiseHidden();
                    break;
                case SDL_WindowEventID.Enter:
                    RaiseMouseEnter();
                    break;
                case SDL_WindowEventID.Leave:
                    RaiseMouseLeave();
                    break;
                case SDL_WindowEventID.Exposed:
                    RaiseExposed();
                    break;
                case SDL_WindowEventID.Moved:
                    RaiseMoved(new Int2(windowEvent.data1, windowEvent.data2));
                    break;
                default:
                    Debug.WriteLine("Unhandled SDL WindowEvent: " + windowEvent.@event);
                    break;
            }
        }

        private MouseState GetCurrentMouseState()
        {
            return new MouseState(
                _currentMouseX, _currentMouseY,
                _currentMouseButtonStates[0], _currentMouseButtonStates[1],
                _currentMouseButtonStates[2], _currentMouseButtonStates[3],
                _currentMouseButtonStates[4]);
        }

        public Int2 ScreenToClient(Int2 p)
        {
            Int2 position = GetWindowPosition();
            return new Int2(p.X - position.X, p.Y - position.Y);
        }

        private Int2 GetWindowPosition()
        {
            int x, y;
            SDL_GetWindowPosition(_window, &x, &y);
            return new Int2(x, y);
        }

        private void SetWindowPosition(int x, int y)
        {
            SDL_SetWindowPosition(_window, x, y);
        }

        private Int2 GetWindowSize()
        {
            int w, h;
            SDL_GetWindowSize(_window, &w, &h);
            return new Int2(w, h);
        }

        private void SetWindowSize(int width, int height)
        {
            SDL_SetWindowSize(_window, width, height);
        }

        private IntPtr GetUnderlyingWindowHandle()
        {
            SDL_SysWMinfo wmInfo;
            SDL_GetVersion(&wmInfo.version);
            SDL_GetWMWindowInfo(_window, &wmInfo);
            if (wmInfo.subsystem == SysWMType.Windows)
            {
                Win32WindowInfo win32Info = Unsafe.Read<Win32WindowInfo>(&wmInfo.info);
                return win32Info.window;
            }

            return _window;
        }

        private bool GetWindowBordered() => (SDL_GetWindowFlags(_window) & SDL_WindowFlags.Borderless) == 0;
        private void SetWindowBordered(bool value) => SDL_SetWindowBordered(_window, value ? 1u : 0u);

        
    }

}
