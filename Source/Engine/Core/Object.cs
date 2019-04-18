using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class Object : IDisposable
    {
        internal static Context _context;

        public static T CreateSubsystem<T>() where T : Object, new()
            => _context.CreateSubsystem<T>();

        public static T RegisterSubsystem<T>(T sub) where T : Object
            => _context.RegisterSubsystem(sub);

        public static T Get<T>() => _context.Get<T>();

        public virtual void Dispose()
        {
        }
    }
}
