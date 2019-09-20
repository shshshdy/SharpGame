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
        public static void Subscribe<T>(this IObserver self, Object observable, EventAction<T> action)
        {
            EventSystem.Instance.SubscribeEvent(self, observable, action);
        }

        public static void Unsubscribe<T>(this IObserver self, Object observable, EventAction<T> action)
        {
            EventSystem.Instance.UnsubscribeEvent(self, observable, action);
        }

        public static void Subscribe<T>(this IObserver self, EventAction<T> action)
        {
            EventSystem.Instance.SubscribeEvent(self, EventSystem.Instance, action);
        }

        public static void Unsubscribe<T>(this IObserver self, EventAction<T> action)
        {
            EventSystem.Instance.UnsubscribeEvent(self, EventSystem.Instance, action);
        }

        public static void UnsubscribeAllEvents(this IObserver self)
        {
            EventSystem.Instance.UnsubscribeAllEvents(self);
        }

        public static void SendGlobalEvent<T>(this IObserver self, in T e)
        {
            EventSystem.Instance.SendEvent(in e);
        }

    }
    
}
