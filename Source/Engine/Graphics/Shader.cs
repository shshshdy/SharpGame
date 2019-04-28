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
        public Dictionary<string, Pass> Passes { get; set; } = new Dictionary<string, Pass>();

        public Shader()
        {
        }

        public Shader(string name, params Pass[] passes)
        {
            Name = name;

            foreach(var pass in passes)
            {
                AddPass(pass);
            }
        }

        public void AddPass(Pass pass)
        {
            Passes.Add(pass.Name, pass);
        }

        public Pass GetPass(string name)
        {
            if(Passes.TryGetValue(name, out Pass pass))
            {
                return pass;
            }

            return null;
        }

        protected override void OnBuild()
        {
            var it = Passes.GetEnumerator();
            while(it.MoveNext())
            {
                it.Current.Value.Build();
            }

        }

        public override void Dispose()
        {
            var it = Passes.GetEnumerator();
            while (it.MoveNext())
            {
                it.Current.Value.Dispose();
            }

            Passes.Clear();

            base.Dispose();
        }
    }

    public class Pass : IDisposable
    {
        private string name_;
        [IgnoreDataMember]
        public string Name { get => name_; set => name_ = string.Intern(value); }

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
        private bool builded_ = false;

        public static readonly string shadow = string.Intern("shadow");
        public static readonly string depth = string.Intern("depth");
        public static readonly string clear = string.Intern("clear");
        public static readonly string main = string.Intern("main");
        
        public Pass()
        {            
        }

        public Pass(string fileName, string funcName = "main")
        {
            ComputeShader = new ShaderModule(ShaderStages.Compute, fileName, funcName);
            Build();
        }

        public Pass(string name, ShaderModule vertexShader, ShaderModule pixelShader, ShaderModule geometryShader = null,
            ShaderModule hullShader = null, ShaderModule domainShader = null, ShaderModule computeShader = null)
        {
            Name = name;
            VertexShader = vertexShader;
            PixelShader = pixelShader;
            GeometryShader = geometryShader;
            HullShader = hullShader;
            DomainShader = domainShader;
            ComputeShader = computeShader;

            Build();
        }

        public Pass(string name, params ShaderModule[] shaderModules)
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

            Build();
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
            builded_ = true;

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
    

}
