﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.CompilerServices;

namespace SharpGame
{
    public static unsafe class Utilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>()
        {
            return Unsafe.SizeOf<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T As<T>(IntPtr ptr)
        {
            return ref Unsafe.AsRef<T>((void*)ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr As<T>(ref T value)
        {
            return (IntPtr)Unsafe.AsPointer(ref value);
        }
        /// <summary>
        /// Return the sizeof an array of struct. Equivalent to sizeof operator but works on generics too.
        /// </summary>
        /// <typeparam name="T">a struct</typeparam>
        /// <param name="array">The array of struct to evaluate.</param>
        /// <returns>sizeof in bytes of this array of struct</returns>
        public static int SizeOf<T>(T[] array) where T : struct
        {
            return array == null ? 0 : array.Length * Unsafe.SizeOf<T>();
        }

        public static IntPtr AsPointer<T>(ref T source) where T : struct
        {
            unsafe
            {
                return (IntPtr)Unsafe.AsPointer(ref source);
            }
        }

        /// <summary>
        /// Pins the specified source and call an action with the pinned pointer.
        /// </summary>
        /// <typeparam name="T">The type of the structure to pin</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="pinAction">The pin action to perform on the pinned pointer.</param>
        public static void Pin<T>(ref T source, Action<IntPtr> pinAction) where T : struct
        {
            unsafe
            {
                pinAction((IntPtr)Unsafe.AsPointer(ref source));
            }
        }

        /// <summary>
        /// Pins the specified source and call an action with the pinned pointer.
        /// </summary>
        /// <typeparam name="T">The type of the structure to pin</typeparam>
        /// <param name="source">The source array.</param>
        /// <param name="pinAction">The pin action to perform on the pinned pointer.</param>
        public static void Pin<T>(T[] source, Action<IntPtr> pinAction) where T : struct
        {
            unsafe
            {
                pinAction(source == null ? IntPtr.Zero : (IntPtr)Unsafe.AsPointer(ref source));
            }
        }
        
        /// <summary>
        /// Reads the specified T data from a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to read</typeparam>
        /// <param name="source">Memory location to read from.</param>
        /// <returns>The data read from the memory location</returns>
        public static T Read<T>(IntPtr source) where T : struct
        {
            unsafe
            {
                return Unsafe.ReadUnaligned<T>((void*)source);
            }
        }

        /// <summary>
        /// Reads the specified T data from a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to read</typeparam>
        /// <param name="source">Memory location to read from.</param>
        /// <param name="data">The data write to.</param>
        /// <returns>source pointer + sizeof(T)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read<T>(IntPtr source, ref T data) where T : struct
        {
            unsafe
            {
                Unsafe.Copy(ref data, (void*)source);
            }
        }

        /// <summary>
        /// Reads the specified array T[] data from a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to read</typeparam>
        /// <param name="source">Memory location to read from.</param>
        /// <param name="data">The data write to.</param>
        /// <param name="offset">The offset in the array to write to.</param>
        /// <param name="count">The number of T element to read from the memory location</param>
        /// <returns>source pointer + sizeof(T) * count</returns>
        public static IntPtr Read<T>(IntPtr source, T[] data, int offset, int count) where T : struct
        {
            unsafe
            {
                unsafe
                {
                    uint byteCount = (uint)(count * Unsafe.SizeOf<T>());
                    void* dest = Unsafe.AsPointer(ref data[offset]);
                    Unsafe.CopyBlockUnaligned(dest, (void*)source, byteCount);
                    return (IntPtr)((byte*)source + byteCount);
                }
                
            }
        }

        /// <summary>
        /// Writes the specified T data to a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to write</typeparam>
        /// <param name="destination">Memory location to write to.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>destination pointer + sizeof(T)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(IntPtr destination, ref T data) where T : struct
        {
            unsafe
            {
                Unsafe.Copy((void*)destination, ref data);
            }
        }
        
        /// <summary>
        /// Writes the specified array T[] data to a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to write</typeparam>
        /// <param name="destination">Memory location to write to.</param>
        /// <param name="data">The array of T data to write.</param>
        /// <param name="offset">The offset in the array to read from.</param>
        /// <param name="count">The number of T element to write to the memory location</param>
        /// <returns>destination pointer + sizeof(T) * count</returns>
        public static void Write<T>(byte[] destination, T[] data, int offset, int count) where T : struct
        {
            unsafe
            {
                fixed (void* pDest = destination)
                {
                    Write((IntPtr)pDest, data, offset, count);
                }
            }
        }

        /// <summary>
        /// Writes the specified array T[] data to a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to write</typeparam>
        /// <param name="destination">Memory location to write to.</param>
        /// <param name="data">The array of T data to write.</param>
        /// <param name="offset">The offset in the array to read from.</param>
        /// <param name="count">The number of T element to write to the memory location</param>
        /// <returns>destination pointer + sizeof(T) * count</returns>
        public static IntPtr Write<T>(IntPtr destination, T[] data, int offset, int count) where T : struct
        {
            unsafe
            {
                uint byteCount = (uint)(count * Unsafe.SizeOf<T>());
                void* src = Unsafe. AsPointer(ref data[offset]);
                Unsafe.CopyBlockUnaligned((void*)destination, src, byteCount);
                return (IntPtr)((byte*)destination + byteCount);
            }
        }

        /// <summary>
        /// Native memcpy.
        /// </summary>
        /// <param name="dest">The destination memory location</param>
        /// <param name="src">The source memory location.</param>
        /// <param name="sizeInBytesToCopy">The count.</param>
        /// <returns></returns>
        public static void CopyMemory(IntPtr dest, IntPtr src, int sizeInBytesToCopy)
        {
            Unsafe.CopyBlockUnaligned((void*)dest, (void*)src, (uint)sizeInBytesToCopy);
        }

        public static void ClearMemory(IntPtr dest, byte value, int sizeInBytesToClear)
        {
            Unsafe.InitBlockUnaligned((void*)dest, value, (uint)sizeInBytesToClear);
        }

        public unsafe static IntPtr Allocate<T>()
        {
            return AllocateAndClear(Unsafe.SizeOf<T>());
        }

        public unsafe static IntPtr Alloc(int sizeInBytes)
        {
            return Marshal.AllocHGlobal(sizeInBytes);
        }

        public static IntPtr Alloc<T>(int count = 1) => Alloc(SizeOf<T>() * count);

        public static IntPtr AllocateAndClear(int sizeInBytes, byte clearValue = 0)
        {
            var ptr = Alloc(sizeInBytes);
            ClearMemory(ptr, clearValue, sizeInBytes);
            return ptr;
        }

        public static IntPtr Resize<T>(IntPtr oldPointer, int newElementCount) where T : struct
        {
            return (Marshal.ReAllocHGlobal(oldPointer,
                new IntPtr(Marshal.SizeOf(typeof(T)) * newElementCount)));
        }
        
        public unsafe static void Free(IntPtr buffer)
        {
            Marshal.FreeHGlobal(buffer);
        }

        /// <summary>
        /// Allocates unmanaged memory and copies the specified structure over.
        /// </summary>
        /// <typeparam name="T">Type of structure to copy.</typeparam>
        /// <param name="value">The value to copy.</param>
        /// <returns>
        /// A pointer to the newly allocated memory. This memory must be released using the <see
        /// cref="Free(IntPtr)"/> method.
        /// </returns>
        public static IntPtr AllocToPointer<T>(ref T value) where T : struct
        {
            IntPtr ptr = Alloc<T>();
            Unsafe.Copy(ptr.ToPointer(), ref value);
            return ptr;
        }

        /// <summary>
        /// Allocates unmanaged memory and copies the specified structure over.
        /// <para>If the value is <c>null</c>, returns <see cref="IntPtr.Zero"/>.</para>
        /// </summary>
        /// <typeparam name="T">Type of structure to copy.</typeparam>
        /// <param name="value">The value to copy.</param>
        /// <returns>
        /// A pointer to the newly allocated memory. This memory must be released using the <see
        /// cref="Free(IntPtr)"/> method.
        /// </returns>
        public static IntPtr AllocToPointer<T>(ref T? value) where T : struct
        {
            if (!value.HasValue) return IntPtr.Zero;

            IntPtr ptr = Alloc<T>();
            Unsafe.Write(ptr.ToPointer(), value.Value);
            return ptr;
        }

        /// <summary>
        /// Allocates unmanaged memory and copies the specified structures over.
        /// <para>If the array is <c>null</c> or empty, returns <see cref="IntPtr.Zero"/>.</para>
        /// </summary>
        /// <typeparam name="T">Type of elements to copy.</typeparam>
        /// <param name="values">The values to copy.</param>
        /// <returns>
        /// A pointer to the newly allocated memory. This memory must be released using the <see
        /// cref="Free(IntPtr)"/> method.
        /// </returns>
        public static IntPtr AllocToPointer<T>(T[] values) where T : struct
        {
            if (values == null || values.Length == 0) return IntPtr.Zero;

            int structSize = SizeOf<T>();
            int totalSize = values.Length * structSize;
            IntPtr ptr = Alloc(totalSize);

            var walk = (byte*)ptr;
            for (int i = 0; i < values.Length; i++)
            {
                Unsafe.Copy(walk, ref values[i]);
                walk += structSize;
            }

            return ptr;
        }

        public static string FromPointer(byte* pointer)
        {
            if (pointer == null) return null;

            // Read until null-terminator.
            byte* walkPtr = pointer;
            while (*walkPtr != 0) walkPtr++;

            // Decode UTF-8 bytes to string.
            return Encoding.UTF8.GetString(pointer, (int)(walkPtr - pointer));
        }


    }
}
