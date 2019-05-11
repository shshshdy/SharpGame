using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    public struct ResourceRef
    {
        public Guid guid;
        public Resource resource;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Resource(ResourceRef obj)
        {
            return obj.resource;
        }
    }

    public class Resource : Object
    {
        [IgnoreDataMember]
        public string FileName { get; set; }

        [IgnoreDataMember]
        public int MemoryUse { get; protected set; }

        [IgnoreDataMember]
        public bool Modified { get; set; }

        protected FileSystem FileSystem => Get<FileSystem>();

        protected bool builded_ = false;

        public async virtual Task<bool> Load(File stream)
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
