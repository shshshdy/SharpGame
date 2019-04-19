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

        public PipelineLayoutCreateInfo PipelineLayoutInfo { get; set; }

        public PipelineLayout pipelineLayout;

        public Shader()
        {            
        }

        public static Shader Default = new Shader
        {
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
            var graphics = Get<Graphics>();
            for (int i = 0; i < ShaderStageInfo.Length; i++)
            {
                ShaderStageInfo[i].ShaderModule = resourceCache.Load<ShaderModule>(ShaderStageInfo[i].FileName);
            }

            pipelineLayout = graphics.Device.CreatePipelineLayout(PipelineLayoutInfo);
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
