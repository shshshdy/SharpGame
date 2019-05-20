using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class System<T> : Object
    {
        public static T Instance => InstanceHoler<T>.inst;
    }

    public class CoreApplication : System<CoreApplication>
    {
        Stack<Object> subsystems = new Stack<Object>();
        public CoreApplication()
        {
        }

        public T CreateSubsystem<T>() where T : Object, new()
        {
            InstanceHoler<T>.inst = new T();
            return RegisterSubsystem(InstanceHoler<T>.inst);
        }

        public T CreateSubsystem<T>(params object[] param) where T : Object
        {
            InstanceHoler<T>.inst = Activator.CreateInstance(typeof(T), param) as T;
            return RegisterSubsystem(InstanceHoler<T>.inst);
        }

        public T RegisterSubsystem<T>(T sub) where T : Object
        {
            InstanceHoler<T>.inst = sub;
            subsystems.Push(sub);
            return InstanceHoler<T>.inst;
        }

        public T Get<T>()
        {
            return InstanceHoler<T>.inst;
        }

        protected override void Destroy()
        {
            while (subsystems.Count > 0)
                subsystems.Pop().Dispose();
        }
    }

    internal class InstanceHoler<T>
    {
        internal static T inst;
    }

}
