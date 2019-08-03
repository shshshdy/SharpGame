using Delegates;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Text;

namespace SharpGame
{
    public class TypeInfo
    {
        static Dictionary<Type, TypeInfo> metaDB = new Dictionary<Type, TypeInfo>();

        public static TypeInfo GetTypeInfo(Type type)
        {
            if(metaDB.TryGetValue(type, out var metaInfo))
            {
                return metaInfo;
            }

            metaInfo = new TypeInfo(type);
            metaDB.Add(type, metaInfo);            
            return metaInfo;
        }

        public Type Type { get; }

        Dictionary<string, IPropertyAccessor> delegateInfo = new Dictionary<string, IPropertyAccessor>();     

        protected TypeInfo(Type type)
        {
            Type = type;

            CollectProperties(type);
            CollectFields(type);
        }

        void CollectProperties(Type type)
        {
            var properties = Type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var p in properties)
            {
                if (p.IsDefined(typeof(System.NonSerializedAttribute), false))
                {
                    continue;
                }

                var getter = DelegateFactory.PropertyGet(type, p.Name);
                var setter = DelegateFactory.PropertySet(type, p.Name);
                delegateInfo.Add(p.Name, new DynamicAccessor(p.Name, p.PropertyType, getter, setter));
            }
        }

        void CollectFields(Type type)
        {
            var fields = Type.GetFields(BindingFlags.Instance | BindingFlags.Public);

            foreach (var p in fields)
            {
                if(p.IsDefined(typeof(System.NonSerializedAttribute), false))
                {
                    continue;
                }

                var getter = DelegateFactory.FieldGet(type, p.Name);
                var setter = DelegateFactory.FieldSet(type, p.Name);
                delegateInfo.Add(p.Name, new DynamicAccessor(p.Name, p.FieldType, getter, setter));
            }
        }

        public IPropertyAccessor Get(string name)
        {
            if(delegateInfo.TryGetValue(name, out var desc))
            {
                return desc;
            }

            return null;
        }

    }
}
