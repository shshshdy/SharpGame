using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace SharpGame
{
    [DebuggerDisplay("Count = {Count}")]
    public class FastList<T> : IList<T>, IReadOnlyList<T>, ICollection<T>, IEnumerable<T>, IEnumerable
    {
        // Fields
        private const int defaultCapacity_ = 4;

        /// <summary>
        /// Gets the items.
        /// </summary>
        public T[] Items { get=>items_; private set => items_ = value; }
        private T[] items_;
        private int size_;
        public static readonly T[] Empty = new T[0];
        public FastList()
        {
            Items = Empty;
        }

        public FastList(IEnumerable<T> collection)
        {
            var is2 = collection as ICollection<T>;
            if (is2 != null)
            {
                int count = is2.Count;
                Items = new T[count];
                is2.CopyTo(Items, 0);
                size_ = count;
            }
            else
            {
                size_ = 0;
                Items = new T[defaultCapacity_];
                using (IEnumerator<T> enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Add(enumerator.Current);
                    }
                }
            }
        }

        public FastList(int capacity)
        {
            Items = new T[capacity];
        }

        public int Capacity
        {
            get { return Items.Length; }
            set
            {
                if (value != Items.Length)
                {
                    if (value > 0)
                    {
                        var destinationArray = new T[value];
                        if (size_ > 0)
                        {
                            Array.Copy(Items, 0, destinationArray, 0, size_);
                        }
                        Items = destinationArray;
                    }
                    else
                    {
                        Items = Empty;
                    }
                }
            }
        }

        #region IList<T> Members

        public void Add(T item)
        {
            if (size_ == Items.Length)
            {
                EnsureCapacity(size_ + 1);
            }
            Items[size_++] = item;
        }

        public void IncreaseCapacity(int index)
        {
            EnsureCapacity(size_ + index);
            size_ += index;
        }

        public void Clear()
        {
            Clear(false);
        }

        public bool Contains(T item)
        {
            if (item == null)
            {
                for (int j = 0; j < size_; j++)
                {
                    if (Items[j] == null)
                    {
                        return true;
                    }
                }
                return false;
            }
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < size_; i++)
            {
                if (comparer.Equals(Items[i], item))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(Items, 0, array, arrayIndex, size_);
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(Items, item, 0, size_);
        }

        public void Insert(int index, T item)
        {
            if (size_ == Items.Length)
            {
                EnsureCapacity(size_ + 1);
            }
            if (index < size_)
            {
                Array.Copy(Items, index, Items, index + 1, size_ - index);
            }
            Items[index] = item;
            size_++;
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public bool FastRemove(T item)
        {
            int index = IndexOf(item);
            if(index >= 0)
            {
                FastRemove(index);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= size_) throw new ArgumentOutOfRangeException(nameof(index));
            size_--;
            if (index < size_)
            {
                Array.Copy(Items, index + 1, Items, index, size_ - index);
            }
            Items[size_] = default(T);
        }

        public void FastRemove(int index)
        {
            if(index < 0 || index >= size_) throw new ArgumentOutOfRangeException(nameof(index));
            size_--;
            
            if(index < size_)
            {
                Items[index] = Items[size_];
            }
            Items[size_] = default(T);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public int Count
        {
            get { return size_; }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= size_) throw new ArgumentOutOfRangeException(nameof(index));
                return Items[index];
            }
            set
            {
                if (index < 0 || index >= size_) throw new ArgumentOutOfRangeException(nameof(index));
                Items[index] = value;
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        #endregion

        public ref T At(int index)
        {
            return ref items_[index];
        }

        /// <summary>
        /// Clears this list with a fast-clear option.
        /// </summary>
        /// <param name="fastClear">if set to <c>true</c> this method only resets the count elements but doesn't clear items referenced already stored in the list.</param>
        public void Clear(bool fastClear)
        {
            if (!fastClear && size_ > 0)
            {
                Array.Clear(Items, 0, size_);
            }
            size_ = 0;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            InsertRange(size_, collection);
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            return new ReadOnlyCollection<T>(this);
        }

        public int BinarySearch(T item)
        {
            return BinarySearch(0, Count, item, null);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return BinarySearch(0, Count, item, comparer);
        }

        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            return Array.BinarySearch(Items, index, count, item, comparer);
        }

        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            Array.Copy(Items, index, array, arrayIndex, count);
        }

        public void EnsureCapacity(int min)
        {
            if (Items.Length < min)
            {
                int num = (Items.Length == 0) ? defaultCapacity_ : (Items.Length*2);
                if (num < min)
                {
                    num = min;
                }
                Capacity = num;
            }
        }

        public bool Exists(Predicate<T> match)
        {
            return (FindIndex(match) != -1);
        }

        public T Find(Predicate<T> match)
        {
            for (int i = 0; i < size_; i++)
            {
                if (match(Items[i]))
                {
                    return Items[i];
                }
            }
            return default(T);
        }

        public FastList<T> FindAll(Predicate<T> match)
        {
            var list = new FastList<T>();
            for (int i = 0; i < size_; i++)
            {
                if (match(Items[i]))
                {
                    list.Add(Items[i]);
                }
            }
            return list;
        }

        public int FindIndex(Predicate<T> match)
        {
            return FindIndex(0, size_, match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return FindIndex(startIndex, size_ - startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            int num = startIndex + count;
            for (int i = startIndex; i < num; i++)
            {
                if (match(Items[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public T FindLast(Predicate<T> match)
        {
            for (int i = size_ - 1; i >= 0; i--)
            {
                if (match(Items[i]))
                {
                    return Items[i];
                }
            }
            return default(T);
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return FindLastIndex(size_ - 1, size_, match);
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return FindLastIndex(startIndex, startIndex + 1, match);
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            int num = startIndex - count;
            for (int i = startIndex; i > num; i--)
            {
                if (match(Items[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public void ForEach(Action<T> action)
        {
            for (int i = 0; i < size_; i++)
            {
                action(Items[i]);
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public FastList<T> GetRange(int index, int count)
        {
            var list = new FastList<T>(count);
            Array.Copy(Items, index, list.Items, 0, count);
            list.size_ = count;
            return list;
        }

        public int IndexOf(T item, int index)
        {
            return Array.IndexOf(Items, item, index, size_ - index);
        }

        public int IndexOf(T item, int index, int count)
        {
            return Array.IndexOf(Items, item, index, count);
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            var is2 = collection as ICollection<T>;
            if (is2 != null)
            {
                int count = is2.Count;
                if (count > 0)
                {
                    EnsureCapacity(size_ + count);
                    if (index < size_)
                    {
                        Array.Copy(Items, index, Items, index + count, size_ - index);
                    }
                    if (this == is2)
                    {
                        Array.Copy(Items, 0, Items, index, index);
                        Array.Copy(Items, (index + count), Items, (index*2), (size_ - index));
                    }
                    else
                    {
                        is2.CopyTo(Items, index);
                    }
                    size_ += count;
                }
            }
            else
            {
                using (IEnumerator<T> enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Insert(index++, enumerator.Current);
                    }
                }
            }
        }

        private static bool IsCompatibleObject(object value)
        {
            return ((value is T) || ((value == null) && (default(T) == null)));
        }

        public int LastIndexOf(T item)
        {
            if (size_ == 0)
            {
                return -1;
            }
            return LastIndexOf(item, size_ - 1, size_);
        }

        public int LastIndexOf(T item, int index)
        {
            return LastIndexOf(item, index, index + 1);
        }

        public int LastIndexOf(T item, int index, int count)
        {
            if (size_ == 0)
            {
                return -1;
            }
            return Array.LastIndexOf(Items, item, index, count);
        }

        public int RemoveAll(Predicate<T> match)
        {
            int index = 0;
            while ((index < size_) && !match(Items[index]))
            {
                index++;
            }
            if (index >= size_)
            {
                return 0;
            }
            int num2 = index + 1;
            while (num2 < size_)
            {
                while ((num2 < size_) && match(Items[num2]))
                {
                    num2++;
                }
                if (num2 < size_)
                {
                    Items[index++] = Items[num2++];
                }
            }
            Array.Clear(Items, index, size_ - index);
            int num3 = size_ - index;
            size_ = index;
            return num3;
        }

        public void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                size_ -= count;
                if (index < size_)
                {
                    Array.Copy(Items, index + count, Items, index, size_ - index);
                }
                Array.Clear(Items, size_, count);
            }
        }

        public void Reverse()
        {
            Reverse(0, Count);
        }

        public void Reverse(int index, int count)
        {
            Array.Reverse(Items, index, count);
        }

        public void Sort()
        {
            Sort(0, Count, null);
        }

        public void Sort(IComparer<T> comparer)
        {
            Sort(0, Count, comparer);
        }

        struct FunctorComparer<K> : IComparer<K>
        {
            Comparison<K> comparison_;
            public FunctorComparer(Comparison<K> c)
            {
                comparison_ = c;
            }

            public int Compare(K x, K y)
            {
                return comparison_(x, y);
            }
        }

        public void Sort(Comparison<T> comparison)
        {
            if (this.size_ > 0)
            {
                IComparer<T> comparer = new FunctorComparer<T>(comparison);
                Array.Sort<T>(this.Items, 0, this.size_, comparer);
            }
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            Array.Sort(Items, index, count, comparer);
        }

        public T[] ToArray()
        {
            var destinationArray = new T[size_];
            Array.Copy(Items, 0, destinationArray, 0, size_);
            return destinationArray;
        }

        public void TrimExcess()
        {
            var num = (int) (Items.Length*0.9);
            if (size_ < num)
            {
                Capacity = size_;
            }
        }

        public bool TrueForAll(Predicate<T> match)
        {
            for (int i = 0; i < size_; i++)
            {
                if (!match(Items[i]))
                {
                    return false;
                }
            }
            return true;
        }

        // Properties

        // Nested Types

        #region Nested type: Enumerator

        [StructLayout(LayoutKind.Sequential)]
        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private readonly FastList<T> list;
            private int index;
            private T current;

            internal Enumerator(FastList<T> list)
            {
                this.list = list;
                index = 0;
                current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                FastList<T> list = this.list;
                if (index < list.size_)
                {
                    current = list.Items[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                index = list.size_ + 1;
                current = default(T);
                return false;
            }

            public T Current
            {
                get { return current; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            void IEnumerator.Reset()
            {
                index = 0;
                current = default(T);
            }
        }

        #endregion
    }
}