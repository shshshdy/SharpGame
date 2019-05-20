using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGame
{
    public class Input : System<Input>
    {
        public InputSnapshot snapshot;

        public IReadOnlyList<KeyEvent> KeyEvents => snapshot.KeyEvents;

        public IReadOnlyList<MouseEvent> MouseEvents => snapshot.MouseEvents;

        public IReadOnlyList<char> KeyCharPresses => snapshot.KeyCharPresses;

        public Vector2 MousePosition => (Vector2)snapshot.MousePosition;

        public float WheelDelta => snapshot.WheelDelta;

        public bool IsMouseDown(MouseButton button)
        {
            return snapshot.IsMouseDown(button);
        }

        public bool IsKeyPressed(Key key)
        {
            foreach (var k in KeyEvents)
            {
                if (k.Down && k.Key == key)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
