using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    public class FileSystem : Object
    {
        IPlatform platform;
        public FileSystem(IPlatform platform)
        {
            this.platform = platform;
        }

        public Stream Open(string path) => platform.Open(path);

        public byte[] ReadBytes(string path)
        {
            //return platform.Open()
            return null;
        }

        public async Task<byte[]> ReadBytesAsync(string path)
        {
            return null;
                
        }
    }
}
