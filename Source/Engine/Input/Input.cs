using System;
using System.Collections.Generic;

namespace SharpGame
{
    public class Input : Object
    {
        public IReadOnlyList<KeyEvent> KeyEvents => keyEventsList_;
        public IReadOnlyList<MouseEvent> MouseEvents => mouseEventsList_;
        public IReadOnlyList<char> KeyCharPresses => keyCharPressesList_;
        public Vector2 MousePosition { get; set; }
        public float WheelDelta { get; set; }

        List<KeyEvent> keyEventsList_ = new List<KeyEvent>();
        List<MouseEvent> mouseEventsList_ = new List<MouseEvent>();
        List<char> keyCharPressesList_ = new List<char>();
        bool[] mouseDown_ = new bool[13];
        List<Key> keyPressed_ = new List<Key>();
        
        public bool IsMouseDown(MouseButton button)
        {
            return mouseDown_[(int)button];
        }

        public bool IsKeyPressed(Key key)
        {
            foreach(Key k in keyPressed_)
            {
                if(k == key)
                {
                    return true;
                }
            }

            return false;
        }

        internal void Clear()
        {
            keyEventsList_.Clear();
            mouseEventsList_.Clear();
            keyCharPressesList_.Clear();
            WheelDelta = 0f;
        }

        public event Action<MouseWheelEvent> OnMouseWheel;
        public event Action<MouseMoveEvent> OnMouseMove;
        public event Action<MouseEvent> OnMouseDown;
        public event Action<MouseEvent> OnMouseUp;
        public event Action<KeyEvent> OnKeyDown;
        public event Action<KeyEvent> OnKeyUp;

        public void InjectMouseWheel(MouseWheelEvent args)
        {
            OnMouseWheel?.Invoke(args);
        }

        public void InjectMouseMove(MouseMoveEvent args)
        {
            MousePosition = args.MousePosition;
            OnMouseMove?.Invoke(args);
        }

        public void InjectMouseDown(MouseEvent args)
        {
            mouseDown_[(int)args.MouseButton] = args.Down;
            mouseEventsList_.Add(args);

            OnMouseDown?.Invoke(args);
        }

        public void InjectMouseUp(MouseEvent args)
        {
            mouseDown_[(int)args.MouseButton] = args.Down;
            mouseEventsList_.Add(args);

            OnMouseUp?.Invoke(args);
        }

        public void InjectKeyDown(KeyEvent args)
        {
            keyEventsList_.Add(args);

            if(!keyPressed_.Contains(args.Key))
                keyPressed_.Add(args.Key);

            OnKeyDown?.Invoke(args);
        }

        public void InjectKeyUp(KeyEvent args)
        {
            keyEventsList_.Add(args);
            keyPressed_.Remove(args.Key);
            OnKeyUp?.Invoke(args);
        }

        public void InjectTextInput(char c)
        {
            keyCharPressesList_.Add(c);
        }
    }

    public enum MouseButton
    {
        Left = 0,
        Middle = 1,
        Right = 2,
        Button1 = 3,
        Button2 = 4
    }

    public struct MouseState
    {
        public readonly int X;
        public readonly int Y;
        private bool mouseDown0_;
        private bool mouseDown1_;
        private bool mouseDown2_;
        private bool mouseDown3_;
        private bool mouseDown4_;

        public MouseState(
            int x, int y,
            bool mouse0, bool mouse1, bool mouse2, bool mouse3, bool mouse4)
        {
            X = x;
            Y = y;
            mouseDown0_ = mouse0;
            mouseDown1_ = mouse1;
            mouseDown2_ = mouse2;
            mouseDown3_ = mouse3;
            mouseDown4_ = mouse4;
        }

        public bool IsButtonDown(MouseButton button)
        {
            uint index = (uint)button;
            switch (index)
            {
                case 0:
                    return mouseDown0_;
                case 1:
                    return mouseDown1_;
                case 2:
                    return mouseDown2_;
                case 3:
                    return mouseDown3_;
                case 4:
                    return mouseDown4_;
            }

            throw new System.ArgumentOutOfRangeException(nameof(button));
        }
    }

    [System.Flags]
    public enum ModifierKeys
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
    }

    public enum Key
    {
        Unknown = 0,
        ShiftLeft = 1,
        LShift = 1,
        ShiftRight = 2,
        RShift = 2,
        ControlLeft = 3,
        LControl = 3,
        ControlRight = 4,
        RControl = 4,
        AltLeft = 5,
        LAlt = 5,
        AltRight = 6,
        RAlt = 6,
        WinLeft = 7,
        LWin = 7,
        WinRight = 8,
        RWin = 8,
        Menu = 9,
        F1 = 10,
        F2 = 11,
        F3 = 12,
        F4 = 13,
        F5 = 14,
        F6 = 15,
        F7 = 16,
        F8 = 17,
        F9 = 18,
        F10 = 19,
        F11 = 20,
        F12 = 21,
        F13 = 22,
        F14 = 23,
        F15 = 24,
        F16 = 25,
        F17 = 26,
        F18 = 27,
        F19 = 28,
        F20 = 29,
        F21 = 30,
        F22 = 31,
        F23 = 32,
        F24 = 33,
        F25 = 34,
        F26 = 35,
        F27 = 36,
        F28 = 37,
        F29 = 38,
        F30 = 39,
        F31 = 40,
        F32 = 41,
        F33 = 42,
        F34 = 43,
        F35 = 44,
        Up = 45,
        Down = 46,
        Left = 47,
        Right = 48,
        Enter = 49,
        Escape = 50,
        Space = 51,
        Tab = 52,
        BackSpace = 53,
        Back = 53,
        Insert = 54,
        Delete = 55,
        PageUp = 56,
        PageDown = 57,
        Home = 58,
        End = 59,
        CapsLock = 60,
        ScrollLock = 61,
        PrintScreen = 62,
        Pause = 63,
        NumLock = 64,
        Clear = 65,
        Sleep = 66,
        Keypad0 = 67,
        Keypad1 = 68,
        Keypad2 = 69,
        Keypad3 = 70,
        Keypad4 = 71,
        Keypad5 = 72,
        Keypad6 = 73,
        Keypad7 = 74,
        Keypad8 = 75,
        Keypad9 = 76,
        KeypadDivide = 77,
        KeypadMultiply = 78,
        KeypadSubtract = 79,
        KeypadMinus = 79,
        KeypadAdd = 80,
        KeypadPlus = 80,
        KeypadDecimal = 81,
        KeypadPeriod = 81,
        KeypadEnter = 82,
        A = 83,
        B = 84,
        C = 85,
        D = 86,
        E = 87,
        F = 88,
        G = 89,
        H = 90,
        I = 91,
        J = 92,
        K = 93,
        L = 94,
        M = 95,
        N = 96,
        O = 97,
        P = 98,
        Q = 99,
        R = 100,
        S = 101,
        T = 102,
        U = 103,
        V = 104,
        W = 105,
        X = 106,
        Y = 107,
        Z = 108,
        Number0 = 109,
        Number1 = 110,
        Number2 = 111,
        Number3 = 112,
        Number4 = 113,
        Number5 = 114,
        Number6 = 115,
        Number7 = 116,
        Number8 = 117,
        Number9 = 118,
        Tilde = 119,
        Grave = 119,
        Minus = 120,
        Plus = 121,
        BracketLeft = 122,
        LBracket = 122,
        BracketRight = 123,
        RBracket = 123,
        Semicolon = 124,
        Quote = 125,
        Comma = 126,
        Period = 127,
        Slash = 128,
        BackSlash = 129,
        NonUSBackSlash = 130,
        LastKey = 131
    }
}