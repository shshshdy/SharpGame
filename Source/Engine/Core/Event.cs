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
        Action<T> action_;
        public TEventHandler(Action<T> action)
        {
            action_ = action;
        }

        public Type Type { get { return typeof(T); } }
        
        public void Invoke(T e)
        {
            action_(e);
        }

        public void Invoke(object e)
        {
            action_((T)e);
        }
    }

    public delegate void RefAction<T>(ref T e);
    public struct RefEventHandler<T> : IEventHandler
    {
        RefAction<T> action_;
        public RefEventHandler(RefAction<T> action)
        {
            action_ = action;
        }

        public Type Type { get { return typeof(T); } }

        public void Invoke(ref T e)
        {
            action_(ref e);
        }

        public void Invoke(object e)
        {
            T data = (T)e;
            action_(ref data);
        }
    }
 
}
