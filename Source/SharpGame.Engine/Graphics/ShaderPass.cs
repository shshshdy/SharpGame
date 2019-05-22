using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Vulkan;

namespace SharpGame
{

    public class ShaderPass : DisposeBase
    {
        public static readonly StringID Shadow = "shadow";
        public static readonly StringID Depth = "depth";
        public static readonly StringID Clear = "clear";
        public static readonly StringID Main = "main";

        private StringID name_;
        public StringID Name
        {
            get => name_;
            set
            {
                name_ = value;
                passID = GetID(value);
            }
        }

        [IgnoreDataMember]
        public ulong passID;

        [DataMember]
        public ShaderModule VertexShader { get; set; }
        [DataMember]
        public ShaderModule GeometryShader { get; set; }
        [DataMember]
        public ShaderModule PixelShader { get; set; }
        [DataMember]
        public ShaderModule HullShader { get; set; }
        [DataMember]
        public ShaderModule DomainShader { get; set; }
        [DataMember]
        public ShaderModule ComputeShader { get; set; }

        //[IgnoreDataMember]
        public ResourceLayout[] ResourceLayout { get; set; }
        public ResourceSet[] ResourceSet { get; set; }

        [IgnoreDataMember]
        public bool IsComputeShader => ComputeShader != null;
        private bool builded_ = false;

        static List<StringID> passList = new List<StringID>();
        static ShaderPass()
        {
            passList.Add(Main);
        }

        public static ulong GetID(StringID pass)
        {
            if (pass.IsNullOrEmpty)
            {
                return 0;
            }


            for (int i = 0; i < passList.Count; i++)
            {
                if (passList[i] == pass)
                {
                    return (ulong)(1 << i);
                }
            }

            if (passList.Count >= 64)
            {
                return 0;
            }

            passList.Add(pass);
            return (ulong)(1 << (passList.Count - 1));
        }

        public ShaderPass()
        {
        }

        public ShaderPass(string vertexShader = null, string pixelShader = null, string geometryShader = null,
            string hullShader = null, string domainShader = null, string computeShader = null)
        {
            if (!string.IsNullOrEmpty(vertexShader))
            {
                VertexShader = new ShaderModule(ShaderStage.Vertex, vertexShader);
            }

            if (!string.IsNullOrEmpty(pixelShader))
            {
                PixelShader = new ShaderModule(ShaderStage.Fragment, pixelShader);
            }

            if (!string.IsNullOrEmpty(geometryShader))
            {
                GeometryShader = new ShaderModule(ShaderStage.Geometry, geometryShader);
            }

            if (!string.IsNullOrEmpty(hullShader))
            {
                HullShader = new ShaderModule(ShaderStage.TessellationControl, hullShader);
            }

            if (!string.IsNullOrEmpty(domainShader))
            {
                DomainShader = new ShaderModule(ShaderStage.TessellationEvaluation, domainShader);
            }

            if (!string.IsNullOrEmpty(computeShader))
            {
                ComputeShader = new ShaderModule(ShaderStage.Compute, computeShader);
            }

            Build();
        }

        public ShaderPass(string name, params ShaderModule[] shaderModules)
        {
            foreach (var sm in shaderModules)
            {
                switch (sm.Stage)
                {
                    case ShaderStage.Vertex:
                        VertexShader = sm;
                        break;
                    case ShaderStage.Fragment:
                        PixelShader = sm;
                        break;
                    case ShaderStage.Geometry:
                        GeometryShader = sm;
                        break;
                    case ShaderStage.TessellationControl:
                        HullShader = sm;
                        break;
                    case ShaderStage.TessellationEvaluation:
                        DomainShader = sm;
                        break;
                    case ShaderStage.Compute:
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
            if (builded_)
            {
                return;
            }

            builded_ = true;

            VertexShader?.Build();
            GeometryShader?.Build();
            PixelShader?.Build();
            HullShader?.Build();
            DomainShader?.Build();
            ComputeShader?.Build();
        }

        protected override void Destroy()
        {
            foreach (var stage in this.GetShaderModules())
            {
                stage?.Dispose();
            }

        }

        public unsafe uint GetShaderStageCreateInfos(VkPipelineShaderStageCreateInfo* shaderStageCreateInfo)
        {
            uint count = 0;
            foreach (var sm in GetShaderModules())
            {
                if (sm != null)
                {
                    var shaderStage = VkPipelineShaderStageCreateInfo.New();
                    shaderStage.stage = (VkShaderStageFlags)sm.Stage;
                    shaderStage.module = sm.shaderModule;
                    shaderStage.pName = Strings.main;// sm.FuncName;
                    shaderStageCreateInfo[count++] = shaderStage;
                }
            }
            return count;
        }

        public unsafe VkPipelineShaderStageCreateInfo GetComputeStageCreateInfo()
        {
            if (ComputeShader != null)
            {
                var shaderStage = VkPipelineShaderStageCreateInfo.New();
                shaderStage.stage = VkShaderStageFlags.Compute;
                shaderStage.module = ComputeShader.shaderModule;
                shaderStage.pName = Strings.main;// sm.FuncName;
                return shaderStage;
            }

            return default;
        }
    }
}
