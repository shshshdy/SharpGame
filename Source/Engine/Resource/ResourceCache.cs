using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VulkanCore;


namespace SharpGame
{
    public class ResourceGroup
    {
        public string name;

    }

    public class ResourceCache : Object
    {
        public FileSystem FileSystem => Get<FileSystem>();
        public static ResourceCache Instance => Get<ResourceCache>();

        public static string ContentRoot { get; set; }
        private readonly Dictionary<string, Resource> cachedContent_ = new Dictionary<string, Resource>();

        public ResourceCache(string contentRoot)
        {
            ContentRoot = contentRoot;
        }

        public File Open(string file)
        {
            string filePath = Path.Combine(ContentRoot, file);
            return new File(FileSystem.Open(filePath));
        }

        public StreamReader OpenText(string file)
        {
            string filePath = Path.Combine(ContentRoot, file);
            var stream = FileSystem.Open(filePath);
            return new StreamReader(stream);
        }

        public async Task<T> Load<T>(string contentName) where T : Resource, new()
        {
            if (cachedContent_.TryGetValue(contentName, out Resource value))
                return (T)value;

            File stream = Open(contentName);

            var res = new T();

            if(await res.Load(stream))
            {
                res.Build();
            }

            cachedContent_.Add(contentName, res);

            return res;
        }

        public override void Dispose()
        {
            foreach (IDisposable value in cachedContent_.Values)
                value.Dispose();
            cachedContent_.Clear();
        }
    }
}
