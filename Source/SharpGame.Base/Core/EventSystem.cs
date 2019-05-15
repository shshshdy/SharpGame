
using System;
using System.Collections.Generic;


namespace SharpGame
{
    public class EventSystem : Observable
    {
        static EventSystem()
        {
            Instance = new EventSystem();
        }

        public static EventSystem Instance { get; }

        private Dictionary<IObserver, HashSet<(Observable, IEventHandler)>> subscribedEvents_ 
            = new Dictionary<IObserver, HashSet<(Observable, IEventHandler)>>();
        internal void SubscribeEvent<T>(IObserver oberver, Observable observable, System.Action<T> action)
        {
            HashSet<(Observable, IEventHandler)> subscribedEvents;
            TEventHandler<T> handler = new TEventHandler<T>(action);
            if (subscribedEvents_.TryGetValue(oberver, out subscribedEvents))
            {
                if (subscribedEvents.Contains(ValueTuple.Create(observable, (IEventHandler)handler)))
                {
                    return;
                }
            }
            else
            {
                subscribedEvents = new HashSet<(Observable, IEventHandler)>();
                subscribedEvents_[oberver] = subscribedEvents;
            }

            observable.SubscribeEvent(handler);
            subscribedEvents.Add(ValueTuple.Create(observable, (IEventHandler)handler));
        }

        internal void SubscribeEvent<T>(IObserver oberver, Observable observable, RefAction<T> action)
        {
            HashSet<(Observable, IEventHandler)> subscribedEvents;
            RefEventHandler<T> handler = new RefEventHandler<T>(action);

            if (subscribedEvents_.TryGetValue(oberver, out subscribedEvents))
            {
                if (subscribedEvents.Contains(ValueTuple.Create(observable, (IEventHandler)handler)))
                {
                    return;
                }
            }
            else
            {
                subscribedEvents = new HashSet<(Observable, IEventHandler)>();
                subscribedEvents_[oberver] = subscribedEvents;
            }

            observable.SubscribeEvent(handler);
            subscribedEvents.Add(ValueTuple.Create(observable, (IEventHandler)handler));
        }

        internal void UnsubscribeEvent<T>(IObserver oberver, Observable observable, System.Action<T> action)
        {
            HashSet<(Observable, IEventHandler)> subscribedEvents;
            if (subscribedEvents_.TryGetValue(oberver, out subscribedEvents))
            {
                TEventHandler<T> handler = new TEventHandler<T>(action);
                if (!subscribedEvents.Contains(ValueTuple.Create(observable, (IEventHandler)handler)))
                {
                    return;
                }

                subscribedEvents.Remove(ValueTuple.Create(observable, (IEventHandler)handler));
                observable.UnsubscribeEvent(handler);
            }

        }

        internal void UnsubscribeEvent<T>(IObserver oberver, Observable observable, RefAction<T> action)
        {
            HashSet<(Observable, IEventHandler)> subscribedEvents;
            if (subscribedEvents_.TryGetValue(oberver, out subscribedEvents))
            {
                RefEventHandler<T> handler = new RefEventHandler<T>(action);
                if (!subscribedEvents.Contains((observable, handler)))
                {
                    return;
                }

                subscribedEvents.Remove((observable, handler));
                observable.UnsubscribeEvent(handler);
            }

        }

        internal void UnsubscribeAllEvents(IObserver oberver)
        {
            HashSet<(Observable, IEventHandler)> subscribedEvents;
            if (subscribedEvents_.TryGetValue(oberver, out subscribedEvents))
            {
                foreach (var it in subscribedEvents)
                {
                    it.Item1.UnsubscribeEvent(it.Item2);
                }

                subscribedEvents.Clear();
                subscribedEvents_.Remove(oberver);
            }

        }

    }



}