using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame
{
    public class Shader : Resource
    {
        public string Name { get; set; }
        public Dictionary<string, Pass> Shaders { get; set; }

        public Shader()
        {
        }

        public Shader(Pass[] passes)
        {
            foreach(var pass in passes)
            {
                Shaders.Add(pass.Name, pass);
            }
        }


        public override void Build()
        {
            var it = Shaders.GetEnumerator();
            while(it.MoveNext())
            {
                it.Current.Value.Build();
            }

        }

        public override void Dispose()
        {
            var it = Shaders.GetEnumerator();
            while (it.MoveNext())
            {
                it.Current.Value.Dispose();
            }

            Shaders.Clear();

            base.Dispose();
        }
    }

    public class Pass : IDisposable
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

        public Pass()
        {            
        }

        public Pass(string fileName, string funcName = "main")
        {
            ComputeShader = new ShaderModule(ShaderStages.Compute, fileName, funcName);
        }

        public Pass(ShaderModule[] shaderModules)
        {
            foreach(var sm in shaderModules)
            {
                switch (sm.Stage)
                {
                    case ShaderStages.Vertex:
                        VertexShader = sm;
                        break;
                    case ShaderStages.Fragment:
                        PixelShader = sm;
                        break;
                    case ShaderStages.Geometry:
                        GeometryShader = sm;
                        break;
                    case ShaderStages.TessellationControl:
                        HullShader = sm;
                        break;
                    case ShaderStages.TessellationEvaluation:
                        DomainShader = sm;
                        break;
                    case ShaderStages.Compute:
                        ComputeShader = sm;
                        break;

                }
            }
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

        public void Build()
        {
            VertexShader?.Build();
            GeometryShader?.Build();
            PixelShader?.Build();
            HullShader?.Build();
            DomainShader?.Build();
            ComputeShader?.Build();
        }

        public void Dispose()
        {
            foreach(var stage in this.GetShaderModules())
            {
                stage?.Dispose();
            }

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

    public static class ShaderHelper
    {
        public static Shader Shader(string name, params Pass[] passes)
        {
            return new Shader(passes) { Name = name };
        }

        public static Pass Pass(string name, ShaderModule vertexShader, ShaderModule pixelShader, ShaderModule geometryShader = null,
            ShaderModule hullShader = null, ShaderModule domainShader = null, ShaderModule computeShader = null)
        {
            return new Pass
            {
                Name = name,
                VertexShader = vertexShader,
                PixelShader = pixelShader,
                GeometryShader = geometryShader,
                HullShader = hullShader,
                DomainShader = domainShader,
                ComputeShader = computeShader,
            };
        }

        public static Pass Pass(string name, ShaderModule[] shaderModules)
        {
            return new Pass(shaderModules)
            {
                Name = name
            };
        }

        public static ShaderModule VertexShader(string name, string entry = "main")
        {
            return new ShaderModule { Stage = ShaderStages.Vertex, File = name, FuncName = entry };
        }

        public static ShaderModule PixelShader(string name, string entry = "main")
        {
            return new ShaderModule { Stage = ShaderStages.Fragment, File = name, FuncName = entry };
        }
    }

}
