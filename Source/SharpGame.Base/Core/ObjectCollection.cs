using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ObjectCollection<T> : List<T> where T : Object
    {
        public new void Add(T obj)
        {
            obj.AddRef();

            base.Add(obj);
        }

        public new void AddRange(IEnumerable<T> collection)
        {
            foreach(var obj in collection)
            {
                obj.AddRef();
            }

            base.AddRange(collection);
        }

        public new void Remove(T obj)
        {
            if(base.Remove(obj))
            {
                obj.Release();
            }
        }

        public new void RemoveAt(int index)
        {
            if(index < 0 || index >= Count)
            {
                return;
            }

            T obj = this[index];

            base.RemoveAt(index);

            obj.Release();
        }

        public new void Clear()
        {
            foreach(var obj in this)
            {
                obj.Release();
            }

            base.Clear();
        }

    }
}
