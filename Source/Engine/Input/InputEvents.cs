namespace SharpGame
{
    public struct MouseEvent
    {
        public MouseButton MouseButton { get; }
        public bool Down { get; }
        public int X { get; }
        public int Y { get; }
        public MouseEvent(MouseButton button, bool down, int x, int y)
        {
            MouseButton = button;
            Down = down;
            X = x;
            Y = y;
        }
    }

    public struct MouseWheelEvent
    {
        public MouseState State { get; }
        public float WheelDelta { get; }
        public MouseWheelEvent(MouseState mouseState, float wheelDelta)
        {
            State = mouseState;
            WheelDelta = wheelDelta;
        }
    }

    public struct MouseMoveEvent
    {
        public MouseState State { get; }
        public Vector2 MousePosition { get; }
        public MouseMoveEvent(MouseState mouseState, Vector2 mousePosition)
        {
            State = mouseState;
            MousePosition = mousePosition;
        }
    }

    public struct KeyEvent
    {
        public Key Key { get; }
        public bool Down { get; }
        public ModifierKeys Modifiers { get; }
        public KeyEvent(Key key, bool down, ModifierKeys modifiers)
        {
            Key = key;
            Down = down;
            Modifiers = modifiers;
        }

    }

    public struct KeyDownEvent
    {
        public Key Key { get; }
        public ModifierKeys Modifiers { get; }
        public KeyDownEvent(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

    }

    public struct KeyUpEvent
    {
        public Key Key { get; }
        public ModifierKeys Modifiers { get; }
        public KeyUpEvent(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

    }

    public struct DragDropEvent
    {
        public string File { get; }
        public DragDropEvent(string file)
        {
            File = file;
        }
    }
}