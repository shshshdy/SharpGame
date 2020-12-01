using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ShaderUtil
    {
        public static Pass CreatePass(string vertexShader, string pixelShader, string geometryShader = null,
            string hullShader = null, string domainShader = null, string computeShader = null)
        {
            Pass pass = new Pass();
            if (!string.IsNullOrEmpty(vertexShader))
            {
                pass.VertexShader = CreateShaderModule(VkShaderStageFlags.Vertex, vertexShader);
            }

            if (!string.IsNullOrEmpty(pixelShader))
            {
                pass.PixelShader = CreateShaderModule(VkShaderStageFlags.Fragment, pixelShader);
            }

            if (!string.IsNullOrEmpty(geometryShader))
            {
                pass.GeometryShader = CreateShaderModule(VkShaderStageFlags.Geometry, geometryShader);
            }

            if (!string.IsNullOrEmpty(hullShader))
            {
                pass.TessControlShader = CreateShaderModule(VkShaderStageFlags.TessellationControl, hullShader);
            }

            if (!string.IsNullOrEmpty(domainShader))
            {
                pass.TessEvaluationShader = CreateShaderModule(VkShaderStageFlags.TessellationEvaluation, domainShader);
            }

            if (!string.IsNullOrEmpty(computeShader))
            {
                pass.ComputeShader = CreateShaderModule(VkShaderStageFlags.Compute, computeShader);
            }

            pass.Build();
            return pass;
        }

        public static Pass CreatePass(string computeShader)
        {
            Pass pass = new Pass();
            if (!string.IsNullOrEmpty(computeShader))
            {
                pass.ComputeShader = CreateShaderModule(VkShaderStageFlags.Compute, computeShader);
            }

            pass.Build();
            return pass;
        }

        public static ShaderModule CreateShaderModule(VkShaderStageFlags stage, string fileName, string funcName = "main")
        {
            using (File stream = FileSystem.Instance.GetFile(fileName))
            {
                var source = stream.ReadAllText();
                var shaderModule = ShaderReader.CreateShaderModule(stage, source, fileName);
                shaderModule.Build();
                return shaderModule;
            }
        }


    }
}
