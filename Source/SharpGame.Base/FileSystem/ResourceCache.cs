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
        
        private readonly ConcurrentDictionary<string, Resource> cachedContent_ = new ConcurrentDictionary<string, Resource>();


        Dictionary<Type, IResourceReader> assetReaders_ = new Dictionary<Type, IResourceReader>();

        public ResourceCache()
        {
        }
        
        public T Load<T>(string contentName) where T : Resource, new()
        {
            if (cachedContent_.TryGetValue(contentName, out Resource value))
                return (T)value;

            Type type = typeof(T);
            if (assetReaders_.TryGetValue(type, out IResourceReader reader))
            {
                return reader.Load(contentName) as T;
            }

            File stream = FileSystem.OpenFile(contentName);

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

            File stream = FileSystem.OpenFile(contentName);

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
