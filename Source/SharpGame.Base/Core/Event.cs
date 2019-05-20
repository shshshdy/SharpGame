using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public interface IEventHandler
    {
        Type Type { get; }

        void Invoke(object e);
    }

    public struct TEventHandler<T> : IEventHandler
    {
        Action<T> action;
        public TEventHandler(Action<T> action)
        {
            this.action = action;
        }

        public Type Type { get { return typeof(T); } }
        
        public void Invoke(T e)
        {
            action(e);
        }

        public void Invoke(object e)
        {
            action((T)e);
        }
    }

    public delegate void RefAction<T>(ref T e);
    public struct RefEventHandler<T> : IEventHandler
    {
        RefAction<T> action;
        public RefEventHandler(RefAction<T> action)
        {
            this.action = action;
        }

        public Type Type { get { return typeof(T); } }

        public void Invoke(ref T e)
        {
            action(ref e);
        }

        public void Invoke(object e)
        {
            T data = (T)e;
            action(ref data);
        }
    }
 
}
