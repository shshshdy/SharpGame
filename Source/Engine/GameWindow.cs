using SharpGame;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public enum WindowState
    {
        Normal,
        FullScreen,
        Maximized,
        Minimized,
        BorderlessFullScreen
    }

    public abstract class GameWindow : Object
    {
        public virtual IntPtr Handle
        {
            get;
        }

        public virtual int Width
        {
            get; protected set;
        }

        public virtual int Height
        {
            get; protected set;
        }

        public virtual void Close() { }

        public abstract void RunMessageLoop();

        public abstract void ProcessEvents();

        public event Action OnResized;
        public event Action OnClosing;
        public event Action OnClosed;
        public event Action OnFocusLost;
        public event Action OnFocusGained;
        public event Action OnShown;
        public event Action OnHidden;
        public event Action OnMouseEnter;
        public event Action OnMouseLeave;
        public event Action OnExposed;
        public event Action<Int2> OnMoved;

        protected void RaiseResized()
        {
            OnResized?.Invoke();
        }

        protected void RaiseClosing()
        {
            OnClosing?.Invoke();
        }

        protected void RaiseClosed()
        {
            OnClosed?.Invoke();
        }

        protected void RaiseFocusLost()
        {
            OnFocusLost?.Invoke();
        }

        protected void RaiseFocusGained()
        {
            OnFocusGained?.Invoke();
        }

        protected void RaiseShown()
        {
            OnShown?.Invoke();
        }

        protected void RaiseHidden()
        {
            OnHidden?.Invoke();
        }

        protected void RaiseMouseEnter()
        {
            OnMouseEnter?.Invoke();
        }

        protected void RaiseMouseLeave()
        {
            OnMouseLeave?.Invoke();
        }

        protected void RaiseExposed()
        {
            OnExposed?.Invoke();
        }

        protected void RaiseMoved(Int2 point)
        {
            OnMoved?.Invoke(point);
        }
    }
}
