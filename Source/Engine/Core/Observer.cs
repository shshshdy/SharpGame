using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame
{

    public interface IObserver
    {
    }

    public static class ObserverExtensions
    {
        public static void SubscribeToEvent<T>(this IObserver self, Observable observable, Action<T> action)
        {
            EventSystem.Instance.SubscribeEvent(self, observable, action);
        }

        public static void SubscribeToEvent<T>(this IObserver self, Observable observable, RefAction<T> action)
        {
            EventSystem.Instance.SubscribeEvent(self, observable, action);
        }

        public static void UnsubscribeFromEvent<T>(this IObserver self, Observable observable, Action<T> action)
        {
            EventSystem.Instance.UnsubscribeEvent(self, observable, action);
        }

        public static void UnsubscribeFromEvent<T>(this IObserver self, Observable observable, RefAction<T> action)
        {
            EventSystem.Instance.UnsubscribeEvent(self, observable, action);
        }

        public static void SubscribeToEvent<T>(this IObserver self, Action<T> action)
        {
            EventSystem.Instance.SubscribeEvent(self, EventSystem.Instance, action);
        }

        public static void SubscribeToEvent<T>(this IObserver self, RefAction<T> action)
        {
            EventSystem.Instance.SubscribeEvent(self, EventSystem.Instance, action);
        }

        public static void UnsubscribeFromEvent<T>(this IObserver self, Action<T> action)
        {
            EventSystem.Instance.UnsubscribeEvent(self, EventSystem.Instance, action);
        }

        public static void UnsubscribeFromEvent<T>(this IObserver self, RefAction<T> action)
        {
            EventSystem.Instance.UnsubscribeEvent(self, EventSystem.Instance, action);
        }

        public static void UnsubscribeAllEvents(this IObserver self)
        {
            EventSystem.Instance.UnsubscribeAllEvents(self);
        }

        public static void SendGlobalEvent<T>(this IObserver self, ref T e)
        {
            EventSystem.Instance.SendEvent(ref e);
        }

        public static void SendGlobalEvent<T>(this IObserver self, T e)
        {
            EventSystem.Instance.SendEvent(e);
        }
    }

    public partial class Observer : IObserver
    {
        protected void SubscribeToEvent<T>(Observable observable, Action<T> action)
        {
            EventSystem.Instance.SubscribeEvent(this, observable, action);
        }

        protected void SubscribeToEvent<T>(Observable observable, RefAction<T> action)
        {
            EventSystem.Instance.SubscribeEvent(this, observable, action);
        }

        protected void UnsubscribeFromEvent<T>(Observable observable, Action<T> action)
        {
            EventSystem.Instance.UnsubscribeEvent(this, observable, action);
        }

        protected void UnsubscribeFromEvent<T>(Observable observable, RefAction<T> action)
        {
            EventSystem.Instance.UnsubscribeEvent(this, observable, action);
        }

        protected void SubscribeToEvent<T>(Action<T> action)
        {
            EventSystem.Instance.SubscribeEvent(this, EventSystem.Instance, action);
        }

        protected void SubscribeToEvent<T>(RefAction<T> action)
        {
            EventSystem.Instance.SubscribeEvent(this, EventSystem.Instance, action);
        }

        protected void UnsubscribeFromEvent<T>(Action<T> action)
        {
            EventSystem.Instance.UnsubscribeEvent(this, EventSystem.Instance, action);
        }

        protected void UnsubscribeFromEvent<T>(RefAction<T> action)
        {
            EventSystem.Instance.UnsubscribeEvent(this, EventSystem.Instance, action);
        }
 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(Observer obj)
        {
            return obj != null;
        }
        
        public void SendGlobalEvent<T>(ref T e)
        {
            EventSystem.Instance.SendEvent(ref e);
        }

        public void SendGlobalEvent<T>(T e)
        {
            EventSystem.Instance.SendEvent(e);
        }
    }
    
}
