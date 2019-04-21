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

        public ShaderStageInfo(ShaderStages shaderStages, string fileName, string funcName = "main")
        {
            Stage = shaderStages;
            FileName = fileName;
            FuncName = funcName;
            shaderModule = null;
        }

        internal ShaderModule shaderModule;
    }

    public class Shader : Resource
    {
        public ShaderStageInfo[] ShaderStageInfo { get; set; }

        public Shader()
        {            
        }

        public async override void Build()
        {
            var resourceCache = Get<ResourceCache>();
            var graphics = Get<Graphics>();
            for (int i = 0; i < ShaderStageInfo.Length; i++)
            {
                ShaderStageInfo[i].shaderModule = resourceCache.Load<ShaderModule>(ShaderStageInfo[i].FileName);
            }

        }

        public override void Dispose()
        {
            foreach(var stage in ShaderStageInfo)
            {
                stage.shaderModule?.Dispose();
            }

            base.Dispose();
        }

        public PipelineShaderStageCreateInfo[] GetShaderStageCreateInfos()
        {
            var shaderStageCreateInfo = new PipelineShaderStageCreateInfo[ShaderStageInfo.Length];
            for(int i = 0; i < ShaderStageInfo.Length; i++)
            {
                shaderStageCreateInfo[i] = new PipelineShaderStageCreateInfo(ShaderStageInfo[i].Stage,
                    ShaderStageInfo[i].shaderModule.shaderModule, ShaderStageInfo[i].FuncName);
            }
            return shaderStageCreateInfo;
        }
        
    }

    public class ComputeShader : Resource
    {
        public string FileName;
        public string FuncName;

        internal ShaderModule shaderModule;

        public ComputeShader()
        {
        }

        public ComputeShader(string fileName, string funcName = "main")
        {
            FileName = fileName;
            FuncName = funcName;
        }

        public async override void Build()
        {
            var resourceCache = Get<ResourceCache>();
            var graphics = Get<Graphics>();
            shaderModule = resourceCache.Load<ShaderModule>(FileName);
        }

        public PipelineShaderStageCreateInfo GetShaderStageCreateInfo()
        {
            return new PipelineShaderStageCreateInfo(ShaderStages.Compute, shaderModule.shaderModule, FuncName);
        }
    }
}
