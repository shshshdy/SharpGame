using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    public class FileSystem : Object
    {
        public FileSystem()
        {
        }

        public Stream Open(string path) => new FileStream(path, FileMode.Open);

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
