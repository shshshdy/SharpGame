using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public struct MativeSpan<T>
    {
        public static MativeSpan<T> Empty => default(MativeSpan<T>);

        /// <summary>
        /// The number of items in the span.
        /// </summary>
        public int Length => length;

        /// <summary>
        /// Returns true if Length is 0.
        /// </summary>
        public bool IsEmpty => length == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe MativeSpan(void* pointer, int length)
        {
            this.length = length;
            _byteOffset = new IntPtr(pointer);
        }

        internal IntPtr ByteOffset => _byteOffset;
        private readonly IntPtr _byteOffset;
        private readonly int length;

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

    public unsafe class NativePool<T> : IDisposable where T : struct
    {
        List<IntPtr> buckets = new List<IntPtr>();
        List<int> bucketsSize = new List<int>();
        int capacity;
        Dictionary<int, List<IntPtr>> freeList = new Dictionary<int, List<IntPtr>>();

        public static NativePool<T> Shared { get; }
        static NativePool()
        {
            Shared = new NativePool<T>(1024);
        }

        public NativePool(int capacity = 1024)
        {
            if(capacity < Unsafe.SizeOf<T>())
            {
                throw new ArgumentException("Capacity is too small.");
            }

            this.capacity = capacity;
        }

        public void Dispose()
        {
            foreach(var ptr in buckets)
                Utilities.Free(ptr);
        }

        public MativeSpan<T> Acquire(int count)
        {
            return new MativeSpan<T>((void*)AcquireImpl(count), count);
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

            for(int i = 0; i < bucketsSize.Count; i++)
            {
                if(bucketsSize[i] + count <= capacity)
                {
                    IntPtr ret = buckets[i] + bucketsSize[i] * Unsafe.SizeOf<T>();
                    bucketsSize[i] = bucketsSize[i] + count;
                    return ret;
                }
            }

            IntPtr ret1 = CreateBucket();
            bucketsSize[bucketsSize.Count - 1] = bucketsSize[bucketsSize.Count - 1] + count;
            return ret1;
        }

        public void Release(MativeSpan<T> span)
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
            var ret = Utilities.Alloc(Unsafe.SizeOf<T>() * capacity);
            buckets.Add(ret);
            bucketsSize.Add(0);
            return ret;
        }
    }
}
