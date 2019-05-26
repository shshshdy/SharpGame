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
        
        private readonly Dictionary<string, Resource> cachedContent = new Dictionary<string, Resource>();

        private Dictionary<Type, List<IResourceReader>> resourceReaders = new Dictionary<Type, List<IResourceReader>>();

        public ResourceCache()
        {
        }

        public void RegisterAssertReader(IResourceReader reader)
        {
            if(!resourceReaders.TryGetValue(reader.ResourceType, out List<IResourceReader> readers))
            {
                readers = new List<IResourceReader>();
                resourceReaders[reader.ResourceType] = readers;
            }

            readers.Add(reader);
        }

        public T Load<T>(string contentName) where T : Resource, new()
        {
            if (cachedContent.TryGetValue(contentName, out Resource value))
                return (T)value;

            Type type = typeof(T);
            if (resourceReaders.TryGetValue(type, out List<IResourceReader> readers))
            {
                foreach (var reader in readers)
                {
                    var res = reader.Load(contentName);
                    if(res != null)
                    {
                        cachedContent.Add(contentName, res);
                        return res as T;
                    }

                }
            }

            File stream = FileSystem.OpenFile(contentName);

            var resource = new T();

            if(!resource.Load(stream))
            {
                return null;
            }

            cachedContent.Add(contentName, resource);
            return resource;
        }

        public async Task<T> LoadAsync<T>(string contentName) where T : Resource, new()
        {
            if (cachedContent.TryGetValue(contentName, out Resource value))
                return (T)value;

            File stream = FileSystem.OpenFile(contentName);

            var res = new T();

            if (!await res.LoadAsync(stream))
            {
                return null;
            }

            cachedContent.Add(contentName, res);

            return res;
        }

        public Resource GetExistingResource(Type type, string contentName)
        {
            if (cachedContent.TryGetValue(contentName, out Resource value))
                return value;
            return null;
        }

        protected override void Destroy()
        {
            foreach (IDisposable value in cachedContent.Values)
                value.Dispose();

            cachedContent.Clear();
        }
    }
}
