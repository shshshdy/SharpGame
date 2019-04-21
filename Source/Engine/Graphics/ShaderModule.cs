using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class ShaderModule : Resource
    {
        internal VulkanCore.ShaderModule shaderModule;

        public async override void Load()
        {
        }

        public override void Dispose()
        {
            shaderModule?.Dispose();

            base.Dispose();
        }

        public static ShaderModule Load(string path)
        {
            var graphics = Get<Graphics>();
            var fileSystem = Get<FileSystem>();
            const int defaultBufferSize = 4096;

            using (Stream stream = fileSystem.Open(path))
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms, defaultBufferSize);

                return new ShaderModule
                {
                    shaderModule = graphics.Device.CreateShaderModule(new ShaderModuleCreateInfo(ms.ToArray()))
                };
            }
        }
        /*
        public static async Task<ShaderModule> LoadShaderModuleAsync(string path)
        {
            var graphics = Get<Graphics>();
            var fileSystem = Get<FileSystem>();
            const int defaultBufferSize = 4096;
            path = Path.Combine(ResourceCache.ContentRoot, path);
            using (Stream stream = fileSystem.Open(path))
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms, defaultBufferSize);
                return graphics.Device.CreateShaderModule(new ShaderModuleCreateInfo(ms.ToArray()));
            }
        }*/
    }
}
