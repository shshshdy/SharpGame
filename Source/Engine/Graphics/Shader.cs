using System.IO;
using VulkanCore;

namespace SharpGame
{
    internal static partial class Shader
    {
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
