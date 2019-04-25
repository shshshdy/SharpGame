using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpGame
{
    public class Resource : Object
    {
        public string FileName { get; set; }

        protected FileSystem FileSystem => Get<FileSystem>();

        public async virtual void Load(Stream stream)
        {
        }

        public virtual void Build()
        {
        }
    }
}
