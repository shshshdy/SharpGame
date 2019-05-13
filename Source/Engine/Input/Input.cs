using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SharpGame
{
    public interface IInputSnapshot
    {
        IReadOnlyList<KeyEvent> KeyEvents { get; }
        IReadOnlyList<MouseEvent> MouseEvents { get; }
        IReadOnlyList<MouseMoveEvent> MouseMoveEvents { get; }
        IReadOnlyList<char> KeyCharPresses { get; }
        bool IsMouseDown(MouseButton button);
        Vector2 MousePosition { get; }
        float WheelDelta { get; }
    }

    public class Input : Object, IInputSnapshot
    {
        public InputSnapshot InputSnapshot { get; } = new InputSnapshot();
        public IReadOnlyList<KeyEvent> KeyEvents => InputSnapshot.KeyEvents;
        public IReadOnlyList<MouseEvent> MouseEvents => InputSnapshot.MouseEvents;
        public IReadOnlyList<MouseMoveEvent> MouseMoveEvents => InputSnapshot.MouseMoveEvents;
        public IReadOnlyList<char> KeyCharPresses => InputSnapshot.KeyCharPresses;
        public Vector2 MousePosition => InputSnapshot.MousePosition;
        public float WheelDelta => InputSnapshot.WheelDelta;
        public bool IsMouseDown(MouseButton button) => InputSnapshot.IsMouseDown(button);

        public bool IsKeyPressed(Key key)
        {
            foreach (var k in KeyEvents)
            {
                if (k.Key == key)
                {
                    return true;
                }
            }

            return false;
        }


    }

    public class InputSnapshot : IInputSnapshot
    {
        public List<KeyEvent> KeyEventsList { get; private set; } = new List<KeyEvent>();
        public List<MouseEvent> MouseEventsList { get; private set; } = new List<MouseEvent>();
        public List<MouseMoveEvent> MouseMoveEventList { get; private set; } = new List<MouseMoveEvent>();
        public List<char> KeyCharPressesList { get; private set; } = new List<char>();
        public IReadOnlyList<KeyEvent> KeyEvents => KeyEventsList;
        public IReadOnlyList<MouseEvent> MouseEvents => MouseEventsList;
        public IReadOnlyList<MouseMoveEvent> MouseMoveEvents => MouseMoveEventList;
        public IReadOnlyList<char> KeyCharPresses => KeyCharPressesList;

        public Vector2 MousePosition { get; set; }

        private bool[] _mouseDown = new bool[13];
        public bool[] MouseDown => _mouseDown;
        public float WheelDelta { get; set; }

        public bool IsMouseDown(MouseButton button)
        {
            return _mouseDown[(int)button];
        }

        public void Clear()
        {
            KeyEventsList.Clear();
            MouseEventsList.Clear();
            MouseMoveEventList.Clear();
            KeyCharPressesList.Clear();
            WheelDelta = 0f;
        }

        public void CopyTo(InputSnapshot other)
        {
            Debug.Assert(this != other);

            other.MouseEventsList.Clear();
            foreach (var me in MouseEventsList) { other.MouseEventsList.Add(me); }

            other.MouseMoveEventList.Clear();
            foreach (var me in MouseMoveEventList) { other.MouseMoveEventList.Add(me); }

            other.KeyEventsList.Clear();
            foreach (var ke in KeyEventsList) { other.KeyEventsList.Add(ke); }

            other.KeyCharPressesList.Clear();
            foreach (var kcp in KeyCharPressesList) { other.KeyCharPressesList.Add(kcp); }

            other.MousePosition = MousePosition;
            other.WheelDelta = WheelDelta;
            _mouseDown.CopyTo(other._mouseDown, 0);
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

        private bool _mouseDown0;
        private bool _mouseDown1;
        private bool _mouseDown2;
        private bool _mouseDown3;
        private bool _mouseDown4;
        private bool _mouseDown5;
        private bool _mouseDown6;
        private bool _mouseDown7;
        private bool _mouseDown8;
        private bool _mouseDown9;
        private bool _mouseDown10;
        private bool _mouseDown11;
        private bool _mouseDown12;

        public MouseState(
            int x, int y,
            bool mouse0, bool mouse1, bool mouse2, bool mouse3, bool mouse4, bool mouse5, bool mouse6,
            bool mouse7, bool mouse8, bool mouse9, bool mouse10, bool mouse11, bool mouse12)
        {
            X = x;
            Y = y;
            _mouseDown0 = mouse0;
            _mouseDown1 = mouse1;
            _mouseDown2 = mouse2;
            _mouseDown3 = mouse3;
            _mouseDown4 = mouse4;
            _mouseDown5 = mouse5;
            _mouseDown6 = mouse6;
            _mouseDown7 = mouse7;
            _mouseDown8 = mouse8;
            _mouseDown9 = mouse9;
            _mouseDown10 = mouse10;
            _mouseDown11 = mouse11;
            _mouseDown12 = mouse12;
        }

        public bool IsButtonDown(MouseButton button)
        {
            uint index = (uint)button;
            switch (index)
            {
                case 0:
                    return _mouseDown0;
                case 1:
                    return _mouseDown1;
                case 2:
                    return _mouseDown2;
                case 3:
                    return _mouseDown3;
                case 4:
                    return _mouseDown4;
                case 5:
                    return _mouseDown5;
                case 6:
                    return _mouseDown6;
                case 7:
                    return _mouseDown7;
                case 8:
                    return _mouseDown8;
                case 9:
                    return _mouseDown9;
                case 10:
                    return _mouseDown10;
                case 11:
                    return _mouseDown11;
                case 12:
                    return _mouseDown12;
            }

            throw new ArgumentOutOfRangeException(nameof(button));
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