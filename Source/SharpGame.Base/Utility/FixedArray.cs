using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public struct FixedArray2<T> : IEnumerable<T>
    {
        T item1, item2;

        public T this[int index]
        {
            get
            {
                return Unsafe.Add(ref item1, index);
            }
            set
            {
                Unsafe.Add(ref item1, index) = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            yield return item1;
            yield return item2;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

    }

    public struct FixedArray3<T> : IEnumerable<T>
    {
        T item1, item2, item3;

        public T this[int index]
        {
            get
            {
                return Unsafe.Add(ref item1, index);
            }
            set
            {
                Unsafe.Add(ref item1, index) = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            yield return item1;
            yield return item2;
            yield return item3;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

    }

    public struct FixedArray4<T> : IEnumerable<T>
    {
        T item1, item2, item3, item4;

        public T this[int index]
        {
            get
            {
                return Unsafe.Add(ref item1, index);
            }
            set
            {
                Unsafe.Add(ref item1, index) = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            yield return item1;
            yield return item2;
            yield return item3;
            yield return item4;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

    }

    public struct FixedArray6<T> : IEnumerable<T>
    {
        T item1, item2, item3, item4, item5, item6;

        public T this[int index]
        {
            get
            {
                return Unsafe.Add(ref item1, index);
            }
            set
            {
                Unsafe.Add(ref item1, index) = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            yield return item1;
            yield return item2;
            yield return item3;
            yield return item4;
            yield return item5;
            yield return item6;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

    }

    public struct FixedArray8<T> : IEnumerable<T>
    {
        T item1, item2, item3, item4, item5, item6, item7, item8;

        public T this[int index]
        {
            get
            {
                return Unsafe.Add(ref item1, index);
            }
            set
            {
                Unsafe.Add(ref item1, index) = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            yield return item1;
            yield return item2;
            yield return item3;
            yield return item4;
            yield return item5;
            yield return item6;
            yield return item7;
            yield return item8;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

    }

}
