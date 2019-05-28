using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace SharpGame
{
    public class ResourceGroup
    {
        public Type type;
        public Dictionary<string, Resource> resourceList;
    }

    public class Resources : System<Resources>
    {
        public FileSystem FileSystem => FileSystem.Instance;
        
        private readonly Dictionary<string, Resource> cachedContent = new Dictionary<string, Resource>();
        private Dictionary<Type, List<IResourceReader>> resourceReaders = new Dictionary<Type, List<IResourceReader>>();

        private Dictionary<Guid, string> idToPath = new Dictionary<Guid, string>();
        private Dictionary<string, Guid> filePathToID = new Dictionary<string, Guid>();
        public Resources()
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

        public string GetFilePath(Guid guid)
        {
            if(idToPath.TryGetValue(guid, out string path))
            {
                return path;
            }

            return null;
        }

        public Guid GetGuid(string file)
        {
            string filePath = FileUtil.StandardlizeFile(file);
            if(filePathToID.TryGetValue(filePath, out Guid guid))
            {
                return guid;
            }

            guid = Guid.NewGuid();
            filePathToID[file] = guid;
            idToPath[guid] = file;
            return guid;
        }

        public T Load<T>(Guid guid) where T : Resource, new()
        {
            if (!idToPath.TryGetValue(guid, out string path))
            {
                return null;
            }

            return Load<T>(path);
        }

        public T Load<T>(string resourceName) where T : Resource, new()
        {
            if (cachedContent.TryGetValue(resourceName, out Resource value))
                return (T)value;

            Type type = typeof(T);
            if (resourceReaders.TryGetValue(type, out List<IResourceReader> readers))
            {
                foreach (var reader in readers)
                {
                    var res = reader.Load(resourceName);
                    if(res != null)
                    {
                        cachedContent.Add(resourceName, res);
                        return res as T;
                    }

                }
            }

            File stream = FileSystem.GetFile(resourceName);
            if(stream == null)
            {
                return null;
            }

            var resource = new T();

            if(!resource.Load(stream))
            {
                return null;
            }

            cachedContent.Add(resourceName, resource);
            return resource;
        }

        public async Task<T> LoadAsync<T>(string resourceName) where T : Resource, new()
        {
            if (cachedContent.TryGetValue(resourceName, out Resource value))
                return (T)value;

            File stream = FileSystem.GetFile(resourceName);

            var res = new T();

            if (!await res.LoadAsync(stream))
            {
                return null;
            }

            cachedContent.Add(resourceName, res);

            return res;
        }

        public Resource GetResource(Type type, string contentName)
        {
            if (cachedContent.TryGetValue(contentName, out Resource value))
                return value;
            return null;
        }

        protected void RegisterResource(Resource resource)
        {

        }

        protected override void Destroy()
        {
            foreach (IDisposable value in cachedContent.Values)
                value.Dispose();

            cachedContent.Clear();
        }
    }
}
