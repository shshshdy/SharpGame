using Delegates;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{

    public interface IPropertyAccessor
    {
        string Name { get; }
        Type PropertyType { get; }

        object Get(object obj);
        void Set(object obj, object value);

    }

    public interface IPropertyAccessor<T> : IPropertyAccessor
    {
        T Get(object obj);
        void Set(object obj, T value);

    }

    public class DynamicAccessor : IPropertyAccessor
    {
        public string Name { get; }
        public Type PropertyType { get; }

        Func<object, object> getter;
        Action<object, object> setter;

        public DynamicAccessor(string name, Type propertyType, Func<object, object> getter, Action<object, object> setter)
        {
            Name = name;
            PropertyType = propertyType;
            this.getter = getter;
            this.setter = setter;
        }

        public object Get(object obj)
        {
            return getter.Invoke(obj);
        }

        public void Set(object obj, object value)
        {
            setter.Invoke(obj, value);
        }
    }

    public class PropertyAccessor<T> : IPropertyAccessor<T>
    {
        public string Name { get; }
        public Type PropertyType { get; } = typeof(T);

        Func<object, T> getter;
        Action<object, T> setter;

        public PropertyAccessor(Type type, string name)
        {
            Name = name;
            getter = DelegateFactory.PropertyGet<T>(type, name);
            setter = DelegateFactory.PropertySet<T>(type, name);
        }

        public T Get(object obj)
        {
            return getter.Invoke(obj);
        }

        public void Set(object obj, T value)
        {
            setter.Invoke(obj, value);
        }

//         public object Get(object obj)
//         {
//             return getter.Invoke(obj);
//         }

        public void Set(object obj, object value)
        {
            setter.Invoke(obj, (T)value);
        }

        object IPropertyAccessor.Get(object obj)
        {
            throw new NotImplementedException();
        }
    }

    public class FieldAccessor<T> : IPropertyAccessor
    {
        public string Name { get; }
        public Type PropertyType { get; } = typeof(T);

        Func<object, T> getter;
        Action<object, T> setter;

        public FieldAccessor(Type type, string name)
        {
            Name = name;
            getter = DelegateFactory.FieldGet<T>(type, name);
            setter = DelegateFactory.FieldSet<T>(type, name);
        }

        public object Get(object obj)
        {
            throw new NotImplementedException();
        }

        public void Set(object obj, object value)
        {
            throw new NotImplementedException();
        }
    }


}
