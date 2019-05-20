using System;
using System.Runtime.CompilerServices;

namespace SharpGame
{
    public unsafe class UnmanagedList<T> : IDisposable where T : struct
    {
        public IntPtr Pointer => ptr;
        public int Size => size;

        IntPtr ptr;
        int size;
        int capacity;

        public UnmanagedList(int cap = 256)
        {
            capacity = cap;
            var finalSize = cap * Unsafe.SizeOf<T>();
            ptr = Utilities.Alloc(finalSize);
            size = 0;
        }
        
        public void Dispose()
        {
            if(ptr != IntPtr.Zero)
                Utilities.Free(ptr);            
        }

        public ref T this[int index]
        {
            get
            {
                if (index >= size)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return ref Unsafe.Add(ref Unsafe.AsRef<T>((void*)ptr), index);

            }
            
        }

        public ref T Front()
        {
            return ref Unsafe.AsRef<T>((void*)ptr);
        }
        
        public ref T At(int index)
        {
            return ref Unsafe.Add(ref Unsafe.AsRef<T>((void*)ptr), index);
        }

        public unsafe void Add(T val)
        {
            if (size == capacity)
            {
                Grow();
            }

            Unsafe.Write((void*)(ptr+ size* Unsafe.SizeOf<T>()), val);
            size++;
        }

        public void Clear()
        {
            size = 0;
        }

        void Grow()
        {
            int new_capacity = capacity == 0 ? 4 : capacity * 2;
            ptr = Utilities.Resize<T>(ptr, new_capacity);
            capacity = new_capacity;
        }
        
    }
}
