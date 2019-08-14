using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    public abstract class Resource : Object
    {
        [IgnoreDataMember]
        public Guid Guid { get; set; }

        [IgnoreDataMember]
        public string FileName { get; set; }

        [IgnoreDataMember]
        public int MemoryUse { get; set; }

        [IgnoreDataMember]
        public bool Modified { get; set; }

        [IgnoreDataMember]
        public Type ResourceType => GetType();
        [IgnoreDataMember]
        public ResourceRef ResourceRef => new ResourceRef(ResourceType, Guid, this);

        protected FileSystem FileSystem => FileSystem.Instance;

        protected bool builded_ = false;

        static Dictionary<string, Type> nameToType = new Dictionary<string, Type>();
        public static Type GetType(string name)
        {
            if(nameToType.TryGetValue(name, out var type))
            {
                return type;
            }

            return null;
        }

        public static void RegisterResType(Type type)
        {
            nameToType[type.Name] = type;
        }

        public static void RegisterAllResType(Type type)
        {
            var types = System.Reflection.Assembly.GetAssembly(type).GetTypes();

            foreach (var t in types)
            {
                if (t.IsSubclassOf(typeof(Resource)))
                {
                    RegisterResType(t);
                }
            }
        }

        public virtual Task<bool> LoadAsync(File stream)
        {
            return Task.FromResult(Load(stream));
        }

        public virtual bool Load(File stream)
        {
            if (!OnLoad(stream))
            {
                return false;
            }

            return Build();
        }

        public virtual bool Build()
        {
            if(builded_)
            {
                return true;
            }

            builded_ = true;
            return OnBuild();
         }

        protected virtual bool OnLoad(File stream)
        {
            return false;
        }

        protected virtual bool OnBuild()
        {
            return false;
        }
    }
}
