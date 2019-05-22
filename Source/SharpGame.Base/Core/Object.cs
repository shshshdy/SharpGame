using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class Object : Observable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(Object obj)
        {
            return obj != null;
        }

    }
}
