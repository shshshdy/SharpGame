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

        public PipelineRasterizationStateCreateInfo RasterizationStateCreateInfo { get; set; }
        public PipelineMultisampleStateCreateInfo MultisampleStateCreateInfo { get; set; }
        public PipelineColorBlendStateCreateInfo ColorBlendStateCreateInfo { get; set; }
        public PipelineDepthStencilStateCreateInfo DepthStencilStateCreateInfo { get; set; }

        public Shader()
        {            
        }

        public static Shader Default = new Shader
        {
            RasterizationStateCreateInfo = new PipelineRasterizationStateCreateInfo
            {
                PolygonMode = PolygonMode.Fill,
                CullMode = CullModes.Back,
                FrontFace = FrontFace.CounterClockwise,
                LineWidth = 1.0f
            },

            MultisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo
            {
                RasterizationSamples = SampleCounts.Count1,
                MinSampleShading = 1.0f
            },

            ColorBlendStateCreateInfo = new PipelineColorBlendStateCreateInfo( new[] 
            {
                new PipelineColorBlendAttachmentState
                {
                    SrcColorBlendFactor = BlendFactor.One,
                    DstColorBlendFactor = BlendFactor.Zero,
                    ColorBlendOp = BlendOp.Add,
                    SrcAlphaBlendFactor = BlendFactor.One,
                    DstAlphaBlendFactor = BlendFactor.Zero,
                    AlphaBlendOp = BlendOp.Add,
                    ColorWriteMask = ColorComponents.All
                }
            }),

            ShaderStageInfo = new[]
            {
                new ShaderStageInfo
                {
                    Stage = ShaderStages.Vertex,
                    FileName = "Shader.vert.spv",
                    FuncName = "main"
                },

                new ShaderStageInfo
                {
                    Stage = ShaderStages.Fragment,
                    FileName = "Shader.frag.spv",
                    FuncName = "main"
                }
            }
        };


        public void Load()
        {
            var resourceCache = Get<ResourceCache>();
            for(int i = 0; i < ShaderStageInfo.Length; i++)
            {
                ShaderStageInfo[i].ShaderModule = resourceCache.Load<ShaderModule>(ShaderStageInfo[i].FileName);
            }
        }

        public PipelineShaderStageCreateInfo[] GetPipelineShaderStageCreateInfos()
        {
            var shaderStageCreateInfo = new PipelineShaderStageCreateInfo[ShaderStageInfo.Length];
            for(int i = 0; i < ShaderStageInfo.Length; i++)
            {
                shaderStageCreateInfo[i] = new PipelineShaderStageCreateInfo(ShaderStageInfo[i].Stage,
                    ShaderStageInfo[i].ShaderModule, ShaderStageInfo[i].FuncName);
            }
            return shaderStageCreateInfo;
        }
        
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

    public class ComputeShader : Object
    {

    }
}
