using System;
using System.Collections.Generic;
using System.IO;
using VulkanCore;


namespace SharpGame
{
    public class ResourceCache : Object
    {
        public static string ContentRoot { get; set; }
        private readonly Dictionary<string, IDisposable> _cachedContent = new Dictionary<string, IDisposable>();

        public ResourceCache(string contentRoot)
        {
            ContentRoot = contentRoot;
        }

        public T Load<T>(string contentName)
        {
            if (_cachedContent.TryGetValue(contentName, out IDisposable value))
                return (T)value;

            string path = Path.Combine(ContentRoot, contentName);
            string extension = Path.GetExtension(path);

            Graphics _ctx = Get<Graphics>();
            Type type = typeof(T);
            if (type == typeof(ShaderModule))
            {
                value = Shader.LoadShaderModule(path);
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
