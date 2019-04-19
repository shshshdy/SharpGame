using System.IO;
using VulkanCore;

namespace SharpGame
{
    public struct ShaderStageInfo
    {
        public ShaderStages Stage;
        public string FileName;
        public string FuncName;

        public ShaderModule ShaderModule;
    }

    public class Shader : Object
    {
        public ShaderStageInfo[] ShaderStageInfo { get; set; }

        public Shader()
        {            
        }

        public void Load()
        {
            var resourceCache = Get<ResourceCache>();
            var graphics = Get<Graphics>();
            for (int i = 0; i < ShaderStageInfo.Length; i++)
            {
                ShaderStageInfo[i].ShaderModule = resourceCache.Load<ShaderModule>(ShaderStageInfo[i].FileName);
            }

        }

        public PipelineShaderStageCreateInfo[] GetShaderStageCreateInfos()
        {
            var shaderStageCreateInfo = new PipelineShaderStageCreateInfo[ShaderStageInfo.Length];
            for(int i = 0; i < ShaderStageInfo.Length; i++)
            {
                shaderStageCreateInfo[i] = new PipelineShaderStageCreateInfo(ShaderStageInfo[i].Stage,
                    ShaderStageInfo[i].ShaderModule, ShaderStageInfo[i].FuncName);
            }
            return shaderStageCreateInfo;
        }
        
        public static ShaderModule LoadShaderModule(IPlatform host, Graphics ctx, string path)
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

    public class ComputeShader : Object
    {

    }
}
