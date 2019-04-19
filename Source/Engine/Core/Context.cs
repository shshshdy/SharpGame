using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{



    public class Context : IDisposable
    {
        Stack<Object> _subsystems = new Stack<Object>();
        public Context()
        {
            Object._context = this;
        }

        public T CreateSubsystem<T>() where T : Object, new()
        {
            InstanceHoler<T>._inst = new T();
            return RegisterSubsystem(InstanceHoler<T>._inst);
        }

        public T CreateSubsystem<T>(params object[] param) where T : Object
        {
            InstanceHoler<T>._inst = Activator.CreateInstance(typeof(T), param) as T;
            return RegisterSubsystem(InstanceHoler<T>._inst);
        }

        public T RegisterSubsystem<T>(T sub) where T : Object
        {
            InstanceHoler<T>._inst = sub;
            _subsystems.Push(sub);
            return InstanceHoler<T>._inst;
        }

        public T Get<T>()
        {
            return InstanceHoler<T>._inst;
        }

        public void Dispose()
        {
            while (_subsystems.Count > 0)
                _subsystems.Pop().Dispose();
        }
    }

    internal class InstanceHoler<T>
    {
        internal static T _inst;
    }
}
