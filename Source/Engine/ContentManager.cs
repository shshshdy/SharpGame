using System;
using System.Collections.Generic;
using System.IO;
using VulkanCore;
using static SharpGame.Loader;

namespace SharpGame
{
    public class ContentManager : IDisposable
    {
        private readonly IPlatform _host;
        private readonly Graphics _ctx;
        private readonly string _contentRoot;
        private readonly Dictionary<string, IDisposable> _cachedContent = new Dictionary<string, IDisposable>();

        public ContentManager(IPlatform host, Graphics ctx, string contentRoot)
        {
            _host = host;
            _ctx = ctx;
            _contentRoot = contentRoot;
        }

        public T Load<T>(string contentName)
        {
            if (_cachedContent.TryGetValue(contentName, out IDisposable value))
                return (T)value;

            string path = Path.Combine(_contentRoot, contentName);
            string extension = Path.GetExtension(path);

            Type type = typeof(T);
            if (type == typeof(ShaderModule))
            {
                value = LoadShaderModule(_host, _ctx, path);
            }
            else if (type == typeof(Texture))
            {
                if (extension.Equals(".ktx", StringComparison.OrdinalIgnoreCase))
                {
                    value = LoadKtxVulkanImage(_host, _ctx, path);
                }
            }

            if (value == null)
                throw new NotImplementedException("Content type or extension not implemented.");

            _cachedContent.Add(contentName, value);
            return (T)value;
        }

        public void Dispose()
        {
            foreach (IDisposable value in _cachedContent.Values)
                value.Dispose();
            _cachedContent.Clear();
        }
    }
}
