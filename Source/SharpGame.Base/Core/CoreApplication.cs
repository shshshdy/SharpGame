using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class CoreApplication : Object
    {
        Stack<Object> subsystems = new Stack<Object>();
        public CoreApplication()
        {
            CreateSubsystem<EventSystem>();
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
        
        protected override void Destroy()
        {
            while (subsystems.Count > 0)
                subsystems.Pop().Dispose();
        }
    }

    internal struct InstanceHoler<T>
    {
        internal static T inst;
    }

    public class System<T> : Object
    {
        public static T Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InstanceHoler<T>.inst;
        }
    }

}
