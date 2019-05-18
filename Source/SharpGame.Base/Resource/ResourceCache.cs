using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace SharpGame
{
    public class ResourceGroup
    {
        public string name;

    }

    public class ResourceCache : System<ResourceCache>
    {
        public FileSystem FileSystem => FileSystem.Instance;
        
        public static string ContentRoot { get; set; }
        private readonly ConcurrentDictionary<string, Resource> cachedContent_ = new ConcurrentDictionary<string, Resource>();

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

        public T Load<T>(string contentName) where T : Resource, new()
        {
            if (cachedContent_.TryGetValue(contentName, out Resource value))
                return (T)value;

            File stream = Open(contentName);

            var res = new T();

            if(res.Load(stream))
            {
                res.Build();
            }

            cachedContent_.TryAdd(contentName, res);

            return res;
        }

        public async Task<T> LoadAsync<T>(string contentName) where T : Resource, new()
        {
            if (cachedContent_.TryGetValue(contentName, out Resource value))
                return (T)value;

            File stream = Open(contentName);

            var res = new T();

            if (await res.LoadAsync(stream))
            {
                res.Build();
            }

            cachedContent_.TryAdd(contentName, res);

            return res;
        }

        public Resource GetExistingResource(Type type, string contentName)
        {
            if (cachedContent_.TryGetValue(contentName, out Resource value))
                return value;
            return null;
        }

        protected override void Destroy()
        {
            foreach (IDisposable value in cachedContent_.Values)
                value.Dispose();

            cachedContent_.Clear();
        }
    }
}
