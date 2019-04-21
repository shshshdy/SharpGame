using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class ShaderModule : Resource
    {
        public byte[] Code { get; set; }

        internal VulkanCore.ShaderModule shaderModule;
        public async override void Load(Stream stream)
        {
            var graphics = Get<Graphics>();
            const int defaultBufferSize = 4096;
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms, defaultBufferSize);
                Code = ms.ToArray();
            }
        }

        public async override void Build()
        {
            var graphics = Get<Graphics>();
            shaderModule = graphics.Device.CreateShaderModule(new ShaderModuleCreateInfo(Code));
        }

        public override void Dispose()
        {
            shaderModule?.Dispose();
            Code = null;

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

    }
}
