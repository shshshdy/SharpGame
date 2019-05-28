using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    public class Resource<T> : Resource
    {
        [IgnoreDataMember]
        public override Type ResourceType => typeof(T);
        static Resource()
        {
            nameToType[typeof(T).Name] = typeof(T);
        }
    }

    public abstract class Resource : Object
    {
        public static Dictionary<string, Type> nameToType = new Dictionary<string, Type>();

        [DataMember]
        public Guid Guid { get; set; }

        [IgnoreDataMember]
        public string FileName { get; set; }

        [IgnoreDataMember]
        public int MemoryUse { get; set; }

        [IgnoreDataMember]
        public bool Modified { get; set; }

        [IgnoreDataMember]
        public abstract Type ResourceType { get; }
        public ResourceRef ResourceRef => new ResourceRef(GetType(), Guid, this);

        protected FileSystem FileSystem => FileSystem.Instance;

        protected bool builded_ = false;

#pragma warning disable CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
        public virtual async Task<bool> LoadAsync(File stream)
#pragma warning restore CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
        {
            return false;
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
            return true;
        }

        protected virtual bool OnBuild()
        {
            return true;
        }
    }
}
