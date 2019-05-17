using System;
using System.Runtime.CompilerServices;

namespace SharpGame
{
    public unsafe class UnmanagedList<T> : IDisposable where T : struct
    {
        public IntPtr Pointer => ptr_;
        public int Size => size_;

        IntPtr ptr_;
        int size_;
        int capacity_;

        public UnmanagedList(int cap = 256)
        {
            capacity_ = cap;
            var finalSize = cap * Unsafe.SizeOf<T>();
            ptr_ = Utilities.Alloc(finalSize);
            size_ = 0;
        }
        
        public void Dispose()
        {
            if(ptr_ != IntPtr.Zero)
                Utilities.Free(ptr_);            
        }

        public ref T this[int index]
        {
            get
            {
                if (index >= size_)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return ref Unsafe.Add(ref Unsafe.AsRef<T>((void*)ptr_), index);

            }
            
        }

        public ref T Front()
        {
            return ref Unsafe.AsRef<T>((void*)ptr_);
        }
        
        public ref T At(int index)
        {
            return ref Unsafe.Add(ref Unsafe.AsRef<T>((void*)ptr_), index);
        }

        public unsafe void Add(T val)
        {
            if (size_ == capacity_)
            {
                Grow();
            }

            Unsafe.Write((void*)(ptr_+ size_* Unsafe.SizeOf<T>()), val);
            size_++;
        }

        public void Clear()
        {
            size_ = 0;
        }

        void Grow()
        {
            int new_capacity = capacity_ == 0 ? 4 : capacity_ * 2;
            ptr_ = Utilities.Resize<T>(ptr_, new_capacity);
            capacity_ = new_capacity;
        }
        
    }
}
