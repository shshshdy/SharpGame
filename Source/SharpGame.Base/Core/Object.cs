using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class Object : RefCounted, IObserver
    {
        protected Dictionary<Type, List<IEventHandler>> eventHandlers;

        internal void SubscribeEvent(IEventHandler handler)
        {
            if(eventHandlers == null)
            {
                eventHandlers = new Dictionary<Type, List<IEventHandler>>();
            }

            if (!eventHandlers.TryGetValue(handler.Type, out List<IEventHandler> handlers))
            {
                handlers = new List<IEventHandler>();
                eventHandlers[handler.Type] = handlers;
            }

            handlers.Add(handler);
        }

        internal void UnsubscribeEvent(IEventHandler handler)
        {
            if(eventHandlers == null)
            {
                return;
            }

            if (eventHandlers.TryGetValue(handler.Type, out List<IEventHandler> handlers))
            {
                handlers.Remove(handler);
            }

        }
        
        public void SendEvent<T>(in T e)
        {
            if(eventHandlers == null)
            {
                return;
            }

            if (eventHandlers.TryGetValue(typeof(T), out List<IEventHandler> handlers))
            {
                for (int i = 0; i < handlers.Count; i++)
                {
                    var handler = handlers[i];
                    ((EventHandler<T>)handler).Invoke(in e);
                }
            }
        }

        protected override void Destroy()
        {
            this.UnsubscribeAllEvents();
        }
    }

}
