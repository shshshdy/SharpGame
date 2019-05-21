using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class DeviceObject<T> : DisposeBase where T : struct
    {
        public T handle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(DeviceObject<T> obj)
        {
            return obj.handle;
        }

    }
}
