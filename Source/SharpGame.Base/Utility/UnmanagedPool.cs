using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public struct Span<T>
    {
        public static Span<T> Empty => default(Span<T>);

        /// <summary>
        /// The number of items in the span.
        /// </summary>
        public int Length => _length;

        /// <summary>
        /// Returns true if Length is 0.
        /// </summary>
        public bool IsEmpty => _length == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span(void* pointer, int length)
        {
            _length = length;
            _byteOffset = new IntPtr(pointer);
        }

        internal IntPtr ByteOffset => _byteOffset;
        private readonly IntPtr _byteOffset;
        private readonly int _length;

        /// <summary>
        /// Returns a reference to specified element of the Span.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="System.IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to Length
        /// </exception>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {                
                unsafe { return ref Unsafe.Add<T>(ref Unsafe.AsRef<T>(_byteOffset.ToPointer()), index); }
            }
        }
    }

    public unsafe class UnmanagedPool<T> : IDisposable where T : struct
    {
        List<IntPtr> buckets_ = new List<IntPtr>();
        List<int> bucketsSize_ = new List<int>();
        int capacity_;
        Dictionary<int, List<IntPtr>> freeList = new Dictionary<int, List<IntPtr>>();

        public static UnmanagedPool<T> Shared { get; }
        static UnmanagedPool()
        {
            Shared = new UnmanagedPool<T>(1024);
        }

        public UnmanagedPool(int capacity = 1024)
        {
            if(capacity < Unsafe.SizeOf<T>())
            {
                throw new ArgumentException("Capacity is too small.");
            }

            capacity_ = capacity;
        }

        public void Dispose()
        {
            foreach(var ptr in buckets_)
                Utilities.Free(ptr);
        }

        public Span<T> Acquire(int count)
        {
            return new Span<T>((void*)AcquireImpl(count), count);
        }

        public IntPtr Acquire()
        {
            return AcquireImpl(1);
        }

        IntPtr AcquireImpl(int count)
        {
            if(freeList.TryGetValue(count, out var list))
            {
                if(list.Count > 0)
                {
                    IntPtr ret = list.Pop();
                    return ret;
                }
            }

            for(int i = 0; i < bucketsSize_.Count; i++)
            {
                if(bucketsSize_[i] + count <= capacity_)
                {
                    IntPtr ret = buckets_[i] + bucketsSize_[i] * Unsafe.SizeOf<T>();
                    bucketsSize_[i] = bucketsSize_[i] + count;
                    return ret;
                }
            }

            IntPtr ret1 = CreateBucket();
            bucketsSize_[bucketsSize_.Count - 1] = bucketsSize_[bucketsSize_.Count - 1] + count;
            return ret1;
        }

        public void Release(Span<T> span)
        {
            if(!freeList.TryGetValue(span.Length, out var list))
            {
                list = new List<IntPtr>();
                freeList.Add(span.Length, list);
            }

            list.Add((IntPtr)Unsafe.AsPointer(ref span[0]));
        }

        public void Release(IntPtr ptr)
        {            
            if(!freeList.TryGetValue(1, out var list))
            {
                list = new List<IntPtr>();
                freeList.Add(1, list);
            }

            list.Add(ptr);           
        }

        IntPtr CreateBucket()
        {
            var ret = Utilities.Alloc(Unsafe.SizeOf<T>() * capacity_);
            buckets_.Add(ret);
            bucketsSize_.Add(0);
            return ret;
        }
    }
}
