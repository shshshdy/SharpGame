using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    public class FileSystem : System<FileSystem>
    {
        public static string ContentRoot { get; set; }


        public FileSystem(string contentRoot)
        {
            ContentRoot = contentRoot;
        }

        public Stream OpenStream(string path) => new FileStream(path, FileMode.Open);

        public File OpenFile(string file)
        {
            string filePath = Path.Combine(ContentRoot, file);
            var stream = OpenStream(filePath);
            return new File(stream);
        }

        public BinaryReader Open(string file)
        {
            string filePath = Path.Combine(ContentRoot, file);
            var stream = OpenStream(filePath);
            return new BinaryReader(stream);
        }

        public StreamReader OpenText(string file)
        {
            string filePath = Path.Combine(ContentRoot, file);
            var stream = OpenStream(filePath);
            return new StreamReader(stream);
        }

        public byte[] ReadBytes(string path)
        {
            //return platform.Open()
            return null;
        }

        public byte[] ReadBytesAsync(string path)
        {
            return null;
                
        }
    }
}
