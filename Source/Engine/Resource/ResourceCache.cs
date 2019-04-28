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
        private readonly Dictionary<string, Resource> _cachedContent = new Dictionary<string, Resource>();

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

        public T Load<T>(string contentName) where T : Resource
        {
            if (_cachedContent.TryGetValue(contentName, out Resource value))
                return (T)value;

            string path = Path.Combine(ContentRoot, contentName);
            string extension = Path.GetExtension(path);

            Graphics _ctx = Get<Graphics>();
            Type type = typeof(T);
            if (type == typeof(ShaderModule))
            {
                value = ShaderModule.Load(path);
            }
            else if (type == typeof(Texture))
            {
                if (extension.Equals(".ktx", StringComparison.OrdinalIgnoreCase))
                {
                    value = Texture.Load(path);
                }
            }

            if (value == null)
                throw new NotImplementedException("Content type or extension not implemented.");

            _cachedContent.Add(contentName, value);
            return (T)value;
        }

        public override void Dispose()
        {
            foreach (IDisposable value in _cachedContent.Values)
                value.Dispose();
            _cachedContent.Clear();
        }
    }
}
