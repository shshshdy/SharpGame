using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class FreeList<T> : List<T> where T : new()
    {
        public T Request()
        {
            if(Count > 0)
            {
                return this.Pop();
            }

            return new T();

        }

        public void Free(T obj)
        {
            Add(obj);
        }
    }
}
