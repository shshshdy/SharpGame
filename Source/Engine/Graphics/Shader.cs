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

        public override void Dispose()
        {
            foreach(var stage in ShaderStageInfo)
            {
                stage.ShaderModule?.Dispose();
            }

            base.Dispose();
        }

        public PipelineShaderStageCreateInfo[] GetShaderStageCreateInfos()
        {
            var shaderStageCreateInfo = new PipelineShaderStageCreateInfo[ShaderStageInfo.Length];
            for(int i = 0; i < ShaderStageInfo.Length; i++)
            {
                shaderStageCreateInfo[i] = new PipelineShaderStageCreateInfo(ShaderStageInfo[i].Stage,
                    ShaderStageInfo[i].ShaderModule.shaderModule, ShaderStageInfo[i].FuncName);
            }
            return shaderStageCreateInfo;
        }
        
    }

    public class ComputeShader : Resource
    {
        public ShaderStages Stage;
        public string FileName;
        public string FuncName;
        public ShaderModule ShaderModule;
        

        public async override void Load()
        {
            var resourceCache = Get<ResourceCache>();
            var graphics = Get<Graphics>();
            ShaderModule = resourceCache.Load<ShaderModule>(FileName);
        }

        public PipelineShaderStageCreateInfo GetShaderStageCreateInfo()
        {
            return new PipelineShaderStageCreateInfo(Stage, ShaderModule.shaderModule, FuncName);
        }
    }
}
