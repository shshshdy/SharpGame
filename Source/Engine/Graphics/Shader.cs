using System.IO;
using VulkanCore;

namespace SharpGame
{
    public class Shader
    {
        internal ShaderModule[] shaderModules;

        public static ShaderModule Load(IPlatform host, Graphics ctx, string path)
        {
            const int defaultBufferSize = 4096;
            using (Stream stream = host.Open(path))
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms, defaultBufferSize);
                return ctx.Device.CreateShaderModule(new ShaderModuleCreateInfo(ms.ToArray()));
            }
        }
    }
}
