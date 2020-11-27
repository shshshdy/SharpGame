using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if !VALIDATE
using System.Diagnostics;
#endif
namespace SharpGame
{
    public unsafe class Vector<T> : IEnumerable<T>, IDisposable where T : unmanaged
    {
        private T* dataPtr;
        private uint capacity;
        private uint count;

        public const int DefaultCapacity = 4;
        private const float GrowthFactor = 2f;
        private static readonly uint s_elementByteSize = (uint)Unsafe.SizeOf<T>();

        public Vector() : this(DefaultCapacity) { }
        public Vector(uint capacity)
        {
            Allocate(capacity);
        }

        public Vector(uint capacity, uint count)
        {
            Allocate(capacity);
            Count = count;
        }

        public Vector(Vector<T> existingList)
        {
            Allocate(existingList.capacity);
            Unsafe.CopyBlock(dataPtr, existingList.dataPtr, existingList.count * s_elementByteSize);
            Count = existingList.count;
        }

        public IntPtr Data
        {
            get
            {
                ThrowIfDisposed();
                return (IntPtr)dataPtr;
            }
        }

        public T* DataPtr => dataPtr;

        public uint Count
        {
            get
            {
                ThrowIfDisposed();
                return count;
            }
            set
            {
                ThrowIfDisposed();
                if (value > capacity)
                {
                    uint newLements = value - Count;
                    CoreResize(value);
                    Unsafe.InitBlock(dataPtr + count /** s_elementByteSize*/, 0, newLements * s_elementByteSize);
                }

                count = value;
            }
        }

        public ref T this[uint index]
        {
            get
            {
                ThrowIfDisposed();
#if VALIDATE
                if (index >= _count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
#else
                Debug.Assert(index < count);
#endif
                return ref Unsafe.AsRef<T>(dataPtr + index/* * s_elementByteSize*/);
            }
        }

        public ref T this[int index]
        {
            get
            {
                ThrowIfDisposed();
#if VALIDATE
                if (index < 0 || index >= _count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
#else
                Debug.Assert(index >= 0 && index < count);
#endif
                return ref Unsafe.AsRef<T>(dataPtr + index/* * s_elementByteSize*/);
            }
        }

        public ref T Front()
        {
            return ref dataPtr[0];
        }

        public ref T Back()
        {
            return ref dataPtr[Count - 1];
        }

        public ReadOnlySpan<T> AsSpan()
        {
            ThrowIfDisposed();
            return new ReadOnlySpan<T>(DataPtr, (int)count);
        }

        public ReadOnlyNativeListView<T> GetReadOnlyView(uint start, uint count)
        {
            ThrowIfDisposed();
#if VALIDATE
            if (start + count > _count)
            {
                throw new ArgumentOutOfRangeException();
            }
#else
            Debug.Assert(start + count <= this.count);
#endif
            return new ReadOnlyNativeListView<T>(this, start, count);
        }

        public View<ViewType> GetView<ViewType>() where ViewType : struct
        {
            ThrowIfDisposed();
            return new View<ViewType>(this);
        }

        public bool IsDisposed => dataPtr == null;

        public void Add(in T item, uint numElements)
        {
            ThrowIfDisposed();

            while (count + numElements > capacity)
            {
                CoreResize((uint)(capacity * GrowthFactor));
            }

            //Unsafe.Copy(dataPtr + count, ref item);
            Unsafe.CopyBlock(dataPtr + count, Unsafe.AsPointer(ref Unsafe.AsRef(item)), numElements * s_elementByteSize);
            count += numElements;
        }

        public void Add(ReadOnlySpan<T> item)
        {
            Add(item.GetPinnableReference(), (uint)item.Length);
        }
        
        public void Add(T item)
        {
            ThrowIfDisposed();

            if (count == capacity)
            {
                CoreResize((uint)(capacity * GrowthFactor));
            }

            Unsafe.Write(dataPtr + count /** s_elementByteSize*/, item);
            count += 1;
        }

        public void Add(void* data, uint numElements)
        {
            ThrowIfDisposed();

            uint needed = count + numElements;
            while (needed > capacity)
            {
                CoreResize((uint)(capacity * GrowthFactor));
            }

            Unsafe.CopyBlock(dataPtr + count, data, numElements * s_elementByteSize);
            count += numElements;
        }

        public bool Remove(ref T item)
        {
            ThrowIfDisposed();
            bool result = IndexOf(ref item, out uint index);
            if (result)
            {
                CoreRemoveAt(index);
            }

            return result;
        }

        public bool Remove(T item) => Remove(ref item);

        public void RemoveAt(uint index)
        {
            ThrowIfDisposed();
#if VALIDATE
            if (index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
#else
            Debug.Assert(index < count);
#endif
            CoreRemoveAt(index);
        }

        public void Clear()
        {
            ThrowIfDisposed();
            count = 0;
        }

        public bool IndexOf(ref T item, out uint index)
        {
            ThrowIfDisposed();
            byte* itemPtr = (byte*)Unsafe.AsPointer(ref item);
            for (index = 0; index < count; index++)
            {
                byte* ptr = (byte*)(dataPtr + index) /** s_elementByteSize*/;
                if (Equals(ptr, itemPtr, s_elementByteSize))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IndexOf(T item, out uint index)
        {
            ThrowIfDisposed();
            byte* itemPtr = (byte*)Unsafe.AsPointer(ref item);
            for (index = 0; index < count; index++)
            {
                byte* ptr = (byte*)(dataPtr + index) /** s_elementByteSize*/;
                if (Equals(ptr, itemPtr, s_elementByteSize))
                {
                    return true;
                }
            }

            return false;
        }

        public IntPtr GetAddress(uint index)
        {
            ThrowIfDisposed();
#if VALIDATE
            if (index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

#else
            Debug.Assert(index < count);
#endif
            return new IntPtr(dataPtr + index);
        }

        public void Resize(uint elementCount)
        {
            ThrowIfDisposed();

            if (elementCount > capacity)
            {
                CoreResize(elementCount);
            }

            count = elementCount;
        }

        private void CoreResize(uint elementCount)
        {
            dataPtr = (T*)Marshal.ReAllocHGlobal(new IntPtr(dataPtr), (IntPtr)(elementCount * s_elementByteSize));
            capacity = elementCount;
        }

        private void Allocate(uint elementCount)
        {
            dataPtr = (T*)Marshal.AllocHGlobal((int)(elementCount * s_elementByteSize));
            capacity = elementCount;
        }

        private bool Equals(byte* ptr, byte* itemPtr, uint s_elementByteSize)
        {
            for (int i = 0; i < s_elementByteSize; i++)
            {
                if (ptr[i] != itemPtr[i])
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CoreRemoveAt(uint index)
        {
            Unsafe.CopyBlock(dataPtr + index, dataPtr + count - 1, s_elementByteSize);
            count -= 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if !VALIDATE
        [Conditional("DEBUG")]
#endif
        private void ThrowIfDisposed()
        {
#if VALIDATE
            if (_dataPtr == null)
            {
                throw new ObjectDisposedException(nameof(Data));
            }
#else
            Debug.Assert(dataPtr != null, "NativeList is disposed.");
#endif
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            Marshal.FreeHGlobal(new IntPtr(dataPtr));
            dataPtr = null;
        }
        
#if DEBUG
        ~Vector()
        {
            if (dataPtr != null)
            {
                Log.Warn($"A NativeList<{typeof(T).Name}> was not properly disposed.");
                Dispose();
            }
        }
#endif

        public Enumerator GetEnumerator()
        {
            ThrowIfDisposed();
            return new Enumerator(dataPtr, count);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private T* _basePtr;
            private uint _count;
            private uint _currentIndex;
            private T _current;

            public Enumerator(T* basePtr, uint count)
            {
                _basePtr = basePtr;
                _count = count;
                _currentIndex = 0;
                _current = default(T);
            }

            public T Current => _current;
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_currentIndex != _count)
                {
                    _current = Unsafe.Read<T>(_basePtr + _currentIndex);
                    _currentIndex += 1;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _current = default(T);
                _currentIndex = 0;
            }

            public void Dispose() { }
        }

        public struct View<ViewType> : IEnumerable<ViewType> where ViewType : struct
        {
            private static readonly uint s_elementByteSize = (uint)Unsafe.SizeOf<ViewType>();
            private readonly Vector<T> _parent;

            public View(Vector<T> parent)
            {
                _parent = parent;
            }

            public uint Count => (_parent.Count /** NativeList<T>.s_elementByteSize*/) /*/ s_elementByteSize*/;

            public ViewType this[uint index]
            {
                get
                {
#if VALIDATE
                    if (index >= Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
#else
                    Debug.Assert(index < Count);
#endif
                    return Unsafe.Read<ViewType>(_parent.dataPtr + index * s_elementByteSize);
                }
            }

            public Enumerator GetEnumerator() => new Enumerator(this);

            IEnumerator<ViewType> IEnumerable<ViewType>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<ViewType>
            {
                private View<ViewType> _view;
                private uint _currentIndex;
                private ViewType _current;

                public Enumerator(View<ViewType> view)
                {
                    _view = view;
                    _currentIndex = 0;
                    _current = default(ViewType);
                }

                public ViewType Current => _view[_currentIndex];
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_currentIndex != _view.Count)
                    {
                        _current = _view[_currentIndex];
                        _currentIndex += 1;
                        return true;
                    }

                    return false;
                }

                public void Reset()
                {
                    _currentIndex = 0;
                    _current = default(ViewType);
                }

                public void Dispose() { }
            }
        }
    }

    public struct ReadOnlyNativeListView<T> : IEnumerable<T> where T : unmanaged
    {
        private readonly Vector<T> _list;
        private readonly uint _start;
        public readonly uint Count;

        public ReadOnlyNativeListView(Vector<T> list, uint start, uint count)
        {
            _list = list;
            _start = start;
            Count = count;
        }

        public T this[uint index]
        {
            get
            {
#if VALIDATE
                if (index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
#else
                Debug.Assert(index < Count);
#endif
                return _list[index + _start];
            }
        }

        public T this[int index]
        {
            get
            {
#if VALIDATE
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
#else
                Debug.Assert(index >= 0 && index < Count);
#endif
                return _list[(uint)index + _start];
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private ReadOnlyNativeListView<T> _view;
            private uint _currentIndex;
            private T _current;

            public Enumerator(ReadOnlyNativeListView<T> view)
            {
                _view = view;
                _currentIndex = view._start;
                _current = default(T);
            }

            public T Current => _view[_currentIndex];
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_currentIndex != _view._start + _view.Count)
                {
                    _current = _view[_currentIndex];
                    _currentIndex += 1;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _currentIndex = _view._start;
                _current = default(T);
            }

            public void Dispose() { }
        }
    }
}
