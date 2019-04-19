using System.IO;
using System.Threading.Tasks;
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

    public class Shader : Resource
    {
        public ShaderStageInfo[] ShaderStageInfo { get; set; }

        public Shader()
        {            
        }

        public async override void Load()
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
        
        public static ShaderModule LoadShaderModule(string path)
        {
            var graphics = Get<Graphics>();
            var fileSystem = Get<FileSystem>();
            const int defaultBufferSize = 4096;
            
            using (Stream stream = fileSystem.Open(path))
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms, defaultBufferSize);
                return graphics.Device.CreateShaderModule(new ShaderModuleCreateInfo(ms.ToArray()));
            }
        }

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
        }
    }

    public class ComputeShader : Object
    {

    }
}
