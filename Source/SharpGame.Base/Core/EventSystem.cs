﻿
using System;
using System.Collections.Generic;


namespace SharpGame
{
    using EventSet = HashSet<(Object, IEventHandler)>;
    using EventMap = Dictionary<IObserver, HashSet<(Object, IEventHandler)>>;

    public class EventSystem : System<EventSystem>
    {
        private EventMap subscribedEvents = new EventMap();
        internal void SubscribeEvent<T>(IObserver oberver, Object observable, System.Action<T> action)
        {
            TEventHandler<T> handler = new TEventHandler<T>(action);
            if (this.subscribedEvents.TryGetValue(oberver, out EventSet subscribedEvents))
            {
                if (subscribedEvents.Contains((observable, handler)))
                {
                    return;
                }
            }
            else
            {
                subscribedEvents = new EventSet();
                this.subscribedEvents[oberver] = subscribedEvents;
            }

            observable.SubscribeEvent(handler);
            subscribedEvents.Add((observable, handler));
        }

        internal void SubscribeEvent<T>(IObserver oberver, Object observable, RefAction<T> action)
        {
            RefEventHandler<T> handler = new RefEventHandler<T>(action);

            if (this.subscribedEvents.TryGetValue(oberver, out EventSet subscribedEvents))
            {
                if (subscribedEvents.Contains((observable, handler)))
                {
                    return;
                }
            }
            else
            {
                subscribedEvents = new EventSet();
                this.subscribedEvents[oberver] = subscribedEvents;
            }

            observable.SubscribeEvent(handler);
            subscribedEvents.Add((observable, handler));
        }

        internal void UnsubscribeEvent<T>(IObserver oberver, Object observable, System.Action<T> action)
        {
            if (this.subscribedEvents.TryGetValue(oberver, out EventSet subscribedEvents))
            {
                TEventHandler<T> handler = new TEventHandler<T>(action);
                if (!subscribedEvents.Contains((observable, handler)))
                {
                    return;
                }

                subscribedEvents.Remove((observable, handler));
                observable.UnsubscribeEvent(handler);
            }

        }

        internal void UnsubscribeEvent<T>(IObserver oberver, Object observable, RefAction<T> action)
        {
            if (this.subscribedEvents.TryGetValue(oberver, out EventSet subscribedEvents))
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
            if (this.subscribedEvents.TryGetValue(oberver, out EventSet subscribedEvents))
            {
                foreach (var it in subscribedEvents)
                {
                    it.Item1.UnsubscribeEvent(it.Item2);
                }

                subscribedEvents.Clear();
                this.subscribedEvents.Remove(oberver);
            }

        }

    }



}