using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;


namespace SharpGame
{
    using ResourceMap = Dictionary<string, Resource>;

    public class ResourceGroup
    {
        public Type type;
        public ResourceMap resourceList;
    }

    public class Resources : System<Resources>
    {
        public FileSystem FileSystem => FileSystem.Instance;
        
        private readonly ResourceMap cachedResources = new ResourceMap();
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

        public T Load<T>(ResourceRef resourceRef) where T : Resource, new()
        {
            var res = Load(resourceRef.type, resourceRef.FileID) as T;
            if (res != null)
            {
                return res;
            }

            return Load<T>(resourceRef.FilePath);
        }

        public Resource Load(ResourceRef resourceRef)
        {
            var res = Load(resourceRef.type, resourceRef.FileID);
            if(res != null)
            {
                resourceRef.resource = res;
                return res;
            }

            res = Load(resourceRef.type, resourceRef.FilePath);
            resourceRef.resource = res;
            return res;
        }

        public T Load<T>(Guid guid) where T : Resource, new()
        {
            if (!idToPath.TryGetValue(guid, out string path))
            {
                return null;
            }

            return Load<T>(path);
        }

        public Resource Load(Type type, Guid guid)
        {
            if (!idToPath.TryGetValue(guid, out string path))
            {
                return null;
            }

            return Load(type, path);

        }

        public T Load<T>(string resourceName) where T : Resource, new()
        {
            return Load(typeof(T), resourceName) as T;
        }

        public Resource Load(Type type, string resourceName)
        {
            if (cachedResources.TryGetValue(resourceName, out Resource value))
                return value;

            if (resourceReaders.TryGetValue(type, out List<IResourceReader> readers))
            {
                foreach (var reader in readers)
                {
                    var res = reader.LoadResource(resourceName);
                    if (res != null)
                    {
                        RegisterResource(resourceName, res);
                        return res;
                    }

                }
            }

            File stream = FileSystem.GetFile(resourceName);
            if (stream == null)
            {
                return null;
            }
            try
            {
                Resource resource = null;
                resource = ReadDefault(type, stream);
                if(resource != null)
                {
                    resource.Build();
                    return resource;
                }

                resource = Activator.CreateInstance(type) as Resource;

                if (!resource.Load(stream))
                {
                    return null;
                }

                RegisterResource(resourceName, resource);
                return resource;
            }
            catch(Exception e)
            {
                Log.Error(e.Message);
                return null;
            }
            finally
            {
                stream.Dispose();
            }
        }

        private Resource ReadDefault(Type type, File stream)
        {
            int firstByte = stream.ReadByte();
            stream.Seek(0);
            if (firstByte == '{')
            {
                return Utf8Json.JsonSerializer.NonGeneric.Deserialize(type, stream) as Resource;
            }
            else
            {
                return MessagePack.MessagePackSerializer.NonGeneric.Deserialize(type, stream) as Resource;
            }

        }

        public async Task<T> LoadAsync<T>(string resourceName) where T : Resource, new()
        {
            if (cachedResources.TryGetValue(resourceName, out Resource value))
                return (T)value;

            File stream = FileSystem.GetFile(resourceName);

            var res = new T();

            if (!await res.LoadAsync(stream))
            {
                return null;
            }

            RegisterResource(resourceName, res);

            return res;
        }

        public Resource GetResource(Type type, string contentName)
        {
            if (cachedResources.TryGetValue(contentName, out Resource value))
                return value;
            return null;
        }

        protected void RegisterResource(string resourceName, Resource resource)
        {
            Guid guid = GetGuid(resourceName);
            resource.Guid = guid;
            cachedResources.Add(resourceName, resource);
        }

        protected override void Destroy()
        {
            foreach (var res in cachedResources.Values)
            {
                if(res.Release() != 0)
                {
                    Log.Warn("Resource properly disposed : " + res.FileName);
                }
            }

            cachedResources.Clear();
        }
    }
}
