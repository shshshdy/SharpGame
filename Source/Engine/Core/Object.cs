using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class Object : Observer
    {
        internal static Context _context;

        public static T CreateSubsystem<T>() where T : Object, new()
            => _context.CreateSubsystem<T>();

        public static T CreateSubsystem<T>(params object[] param) where T : Object
            => _context.CreateSubsystem<T>(param);

        public static T RegisterSubsystem<T>(T sub) where T : Object
            => _context.RegisterSubsystem(sub);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>() => _context.Get<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(Object obj)
        {
            return obj != null;
        }

        public virtual void Dispose()
        {
        }
    }
}
