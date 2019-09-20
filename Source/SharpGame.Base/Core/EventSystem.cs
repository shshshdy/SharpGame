
using System;
using System.Collections.Generic;


namespace SharpGame
{
    using EventSet = HashSet<(Object, IEventHandler)>;
    using EventMap = Dictionary<IObserver, HashSet<(Object, IEventHandler)>>;

    public interface IEventHandler
    {
        Type Type { get; }
    }

    public delegate void EventAction<T>(in T e);

    public struct EventHandler<T> : IEventHandler
    {
        EventAction<T> action;
        public EventHandler(EventAction<T> action)
        {
            this.action = action;
        }

        public Type Type => typeof(T);

        public void Invoke(in T e)
        {
            action(in e);
        }

    }
 
    public class EventSystem : System<EventSystem>
    {
        private EventMap subscribedEvents = new EventMap();

        internal void SubscribeEvent<T>(IObserver oberver, Object observable, EventAction<T> action)
        {
            EventHandler<T> handler = new EventHandler<T>(action);

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
        
        internal void UnsubscribeEvent<T>(IObserver oberver, Object observable, EventAction<T> action)
        {
            if (this.subscribedEvents.TryGetValue(oberver, out EventSet subscribedEvents))
            {
                EventHandler<T> handler = new EventHandler<T>(action);
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
