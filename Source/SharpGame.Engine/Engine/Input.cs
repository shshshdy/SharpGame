using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGame
{
    public class Input : System<Input>
    {
        public InputSnapshot snapshot;

        bool[] keyState = new bool[256];

        public IReadOnlyList<KeyEvent> KeyEvents => snapshot.KeyEvents;

        public IReadOnlyList<MouseEvent> MouseEvents => snapshot.MouseEvents;

        public IReadOnlyList<char> KeyCharPresses => snapshot.KeyCharPresses;

        public vec2 MousePosition => snapshot.MousePosition;

        public float WheelDelta => snapshot.WheelDelta;

        public bool IsMouseDown(MouseButton button)
        {
            return snapshot.IsMouseDown(button);
        }

        public bool IsKeyPressed(Key key)
        {
            return keyState[(int)key];
        }

        internal void SetKeyState(Key key, bool down)
        {
            keyState[(int)key] = down;
        }

    }
}
