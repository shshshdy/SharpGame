using System;
using System.Collections.Generic;
using System.IO;
using VulkanCore;


namespace SharpGame
{
    public class ResourceCache : Object
    {
        private readonly IPlatform _host;
        private readonly string _contentRoot;
        private readonly Dictionary<string, IDisposable> _cachedContent = new Dictionary<string, IDisposable>();

        public ResourceCache(IPlatform host, string contentRoot)
        {
            _host = host;
            _contentRoot = contentRoot;
        }

        public T Load<T>(string contentName)
        {
            if (_cachedContent.TryGetValue(contentName, out IDisposable value))
                return (T)value;

            string path = Path.Combine(_contentRoot, contentName);
            string extension = Path.GetExtension(path);

            Graphics _ctx = Get<Graphics>();
            Type type = typeof(T);
            if (type == typeof(ShaderModule))
            {
                value = Shader.Load(_host, _ctx, path);
            }
            else if (type == typeof(Texture))
            {
                if (extension.Equals(".ktx", StringComparison.OrdinalIgnoreCase))
                {
                    value = Texture.Load(_host, _ctx, path);
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
