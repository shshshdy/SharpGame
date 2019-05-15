using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class Observable : DisposeBase, IObserver
    {
        protected Dictionary<Type, List<IEventHandler>> eventHandlers_;

        internal void SubscribeEvent(IEventHandler handler)
        {
            if(eventHandlers_ == null)
            {
                eventHandlers_ = new Dictionary<Type, List<IEventHandler>>();
            }

            if (!eventHandlers_.TryGetValue(handler.Type, out List<IEventHandler> handlers))
            {
                handlers = new List<IEventHandler>();
                eventHandlers_[handler.Type] = handlers;
            }

            handlers.Add(handler);
        }

        internal void UnsubscribeEvent(IEventHandler handler)
        {
            if(eventHandlers_ == null)
            {
                return;
            }

            if (eventHandlers_.TryGetValue(handler.Type, out List<IEventHandler> handlers))
            {
                handlers.Remove(handler);
            }

        }
        
        public void SendEvent<T>(ref T e)
        {
            if(eventHandlers_ == null)
            {
                return;
            }

            if (eventHandlers_.TryGetValue(typeof(T), out List<IEventHandler> handlers))
            {
                foreach (var handler in handlers)
                {
                    ((RefEventHandler<T>)handler).Invoke(ref e);
                }
            }
        }

        public void SendEvent<T>(T e)
        {
            if(eventHandlers_ == null)
            {
                return;
            }

            if (eventHandlers_.TryGetValue(typeof(T), out List<IEventHandler> handlers))
            {
                foreach (var handler in handlers)
                {
                    ((TEventHandler<T>)handler).Invoke(e);
                }
            }
        }
    }

}
