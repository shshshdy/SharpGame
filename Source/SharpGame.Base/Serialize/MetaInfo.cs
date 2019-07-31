using Delegates;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Text;

namespace SharpGame
{

    public class DelegateInfo
    {        
        public Delegate getter;
        public Delegate setter;
    }

    public class MetaInfo
    {
        static Dictionary<Type, MetaInfo> metaDB = new Dictionary<Type, MetaInfo>();

        public static MetaInfo GetMetaInfo(Type type)
        {
            if(metaDB.TryGetValue(type, out var metaInfo))
            {
                return metaInfo;
            }

            metaInfo = new MetaInfo(type);
            metaDB.Add(type, metaInfo);            
            return metaInfo;
        }

        public Type Type { get; }

        Dictionary<string, DelegateInfo> delegateInfo = new Dictionary<string, DelegateInfo>();     

        protected MetaInfo(Type type)
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
                delegateInfo.Add(p.Name, new DelegateInfo
                {
                    getter = getter,
                    setter = setter
                });
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
                delegateInfo.Add(p.Name, new DelegateInfo
                {
                    getter = getter,
                    setter = setter
                });
            }
        }

        public DelegateInfo Get(string name)
        {
            if(delegateInfo.TryGetValue(name, out var desc))
            {
                return desc;
            }

            return null;
        }

    }
}
