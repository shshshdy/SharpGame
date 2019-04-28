using System;
using System.Collections.Generic;
using System.IO;
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

        public Stream Open(string file)
        {
            string filePath = Path.Combine(ContentRoot, file);
            return FileSystem.Open(filePath);
        }

        public StreamReader OpenText(string file)
        {
            var stream = Open(file);
            return new StreamReader(stream);
        }

        public T Load<T>(string contentName) where T : Resource, new()
        {
            if (cachedContent_.TryGetValue(contentName, out Resource value))
                return (T)value;

            Stream stream = Open(contentName);
            var res = new T();
            res.Load(stream);
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
