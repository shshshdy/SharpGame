using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    public class Resource : Object
    {
        [IgnoreDataMember]
        public string FileName { get; set; }

        protected FileSystem FileSystem => Get<FileSystem>();

        protected bool builded_ = false;

        public async virtual Task<bool> Load(Stream stream)
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
