using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class Subsystem<T> : Object
    {
        public static T Instance => InstanceHoler<T>.inst;

        public static T CreateSubsystem<T>() where T : Object, new()
            => _context.CreateSubsystem<T>();

        public static T CreateSubsystem<T>(params object[] param) where T : Object
            => _context.CreateSubsystem<T>(param);

        public static T RegisterSubsystem<T>(T sub) where T : Object
            => _context.RegisterSubsystem(sub);

    }
}
