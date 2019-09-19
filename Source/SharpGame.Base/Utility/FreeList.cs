using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class FreeList<T> : List<T> where T : new()
    {
        public virtual T Request()
        {
            if(Count > 0)
            {
                return this.Pop();
            }

            return new T();

        }

        public virtual void Free(T obj)
        {
            Add(obj);
        }
    }

    public class ListPool<T> : FreeList<List<T>>
    {
        public override void Free(List<T> obj)
        {
            obj.Clear();
            Add(obj);
        }
    }

    public class FastListPool<T> : FreeList<FastList<T>>
    {
        public override void Free(FastList<T> obj)
        {
            obj.Clear();
            Add(obj);
        }
    }
}
