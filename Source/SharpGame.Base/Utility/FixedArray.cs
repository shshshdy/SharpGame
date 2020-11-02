using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public struct FixedArray2<T> : IEnumerable<T> //where T : unmanaged
    {
#pragma warning disable CS0649
        public T item1, item2;
#pragma warning restore CS0649
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

        public IntPtr Data => Utilities.AsPointer(ref item1);

        public void Clear()
        {
            item1 = default;
            item2 = default;
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

    public struct FixedArray3<T> : IEnumerable<T> //where T : unmanaged
    {
#pragma warning disable CS0649
        public T item1, item2, item3;
#pragma warning restore CS0649
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

        public IntPtr Data => Utilities.AsPointer(ref item1);

        public void Clear()
        {
            item1 = default;
            item2 = default;
            item3 = default;
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

    public struct FixedArray4<T> : IEnumerable<T> //where T : unmanaged
    {
#pragma warning disable CS0649
        public T item1, item2, item3, item4;
#pragma warning restore CS0649
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

        public IntPtr Data => Utilities.AsPointer(ref item1);

        public void Clear()
        {
            item1 = default;
            item2 = default;
            item3 = default;
            item4 = default;
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

    public struct FixedArray6<T> : IEnumerable<T> //where T : unmanaged
    {
#pragma warning disable CS0649
        public T item1, item2, item3, item4, item5, item6;
#pragma warning restore CS0649
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

        public IntPtr Data => Utilities.AsPointer(ref item1);

        public void Clear()
        {
            item1 = default;
            item2 = default;
            item3 = default;
            item4 = default;
            item5 = default;
            item6 = default;
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

    public struct FixedArray8<T> : IEnumerable<T>// where T : unmanaged
    {
#pragma warning disable CS0649
        public T item1, item2, item3, item4, item5, item6, item7, item8;
#pragma warning restore CS0649

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

        public IntPtr Data => Utilities.AsPointer(ref item1);

        public void Clear()
        {
            item1 = default;
            item2 = default;
            item3 = default;
            item4 = default;
            item5 = default;
            item6 = default;
            item7 = default;
            item8 = default;
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
