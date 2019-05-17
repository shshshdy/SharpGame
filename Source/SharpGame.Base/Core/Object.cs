using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class Object : Observable
    {
        internal static Context _context;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>() => _context.Get<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(Object obj)
        {
            return obj != null;
        }

        protected override void Destroy()
        {
        }
    }
}
