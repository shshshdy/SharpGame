using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame
{
    public class Pass
    {

    }

    public class Shader : Resource
    {
        public string Name { get; set; }

        public ShaderModule VertexShader { get; set; }
        public ShaderModule GeometryShader { get; set; }
        public ShaderModule PixelShader { get; set; }
        public ShaderModule HullShader { get; set; }
        public ShaderModule DomainShader { get; set; }
        public ShaderModule ComputeShader { get; set; }

        [IgnoreDataMember]
        public DescriptorSetLayout DescriptorSetLayout { get; set; }
        [IgnoreDataMember]
        public DescriptorPool DescriptorPool { get; set; }
        [IgnoreDataMember]
        public DescriptorSet DescriptorSet { get; set; }

        [IgnoreDataMember]
        public bool IsComputeShader => ComputeShader != null;

        public Shader()
        {            
        }

        public Shader(string fileName, string funcName = "main")
        {
            ComputeShader = new ShaderModule(ShaderStages.Compute, fileName, funcName);
        }

        public IEnumerable<ShaderModule> GetShaderModules()
        {
            yield return VertexShader;
            yield return GeometryShader;
            yield return PixelShader;
            yield return HullShader;
            yield return DomainShader;
            yield return ComputeShader;
        }

        public override void Build()
        {
            VertexShader?.Build();
            GeometryShader?.Build();
            PixelShader?.Build();
            HullShader?.Build();
            DomainShader?.Build();
            ComputeShader?.Build();
        }

        public override void Dispose()
        {
            foreach(var stage in this.GetShaderModules())
            {
                stage?.Dispose();
            }

            base.Dispose();
        }

        public PipelineShaderStageCreateInfo[] GetShaderStageCreateInfos()
        {
            var shaderStageCreateInfo = new List<PipelineShaderStageCreateInfo>();
            foreach(var sm in GetShaderModules())
            {
                if(sm != null)
                {
                    var shaderStage = new PipelineShaderStageCreateInfo(sm.Stage,
                        sm.shaderModule, sm.FuncName);
                    shaderStageCreateInfo.Add(shaderStage);
                }
            }
            return shaderStageCreateInfo.ToArray();
        }

        public PipelineShaderStageCreateInfo GetComputeStageCreateInfo()
        {
            if(ComputeShader != null)
            {
                return new PipelineShaderStageCreateInfo(ShaderStages.Compute, ComputeShader.shaderModule, ComputeShader.FuncName);
            }

            return default;
        }
    }


}
