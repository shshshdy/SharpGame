using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    [DataContract]
    public class ResourceRef
    {
        [DataMember]
        public Guid Guid { get; set; }

        [IgnoreDataMember]
        public Resource value;
    }

    [DataContract]
    public class ResourceRef<T> : ResourceRef where T : Resource
    {
        [IgnoreDataMember]
        public T Value => (T)value;
    }

    public class Resource : Object
    {
        [IgnoreDataMember]
        public string FileName { get; set; }

        [IgnoreDataMember]
        public int MemoryUse { get; protected set; }

        [IgnoreDataMember]
        public bool Modified { get; set; }

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
            return false;
        }

        public virtual void Build()
        {
            if(!builded_)
            {
                builded_ = true;
            }

            OnBuild();
        }
        
        protected virtual void OnBuild()
        {

        }
    }
}
