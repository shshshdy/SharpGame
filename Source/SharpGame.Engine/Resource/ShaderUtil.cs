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
                pass.VertexShader = CreateShaderModule(ShaderStage.Vertex, vertexShader);
            }

            if (!string.IsNullOrEmpty(pixelShader))
            {
                pass.PixelShader = CreateShaderModule(ShaderStage.Fragment, pixelShader);
            }

            if (!string.IsNullOrEmpty(geometryShader))
            {
                pass.GeometryShader = CreateShaderModule(ShaderStage.Geometry, geometryShader);
            }

            if (!string.IsNullOrEmpty(hullShader))
            {
                pass.HullShader = CreateShaderModule(ShaderStage.TessControl, hullShader);
            }

            if (!string.IsNullOrEmpty(domainShader))
            {
                pass.DomainShader = CreateShaderModule(ShaderStage.TessEvaluation, domainShader);
            }

            if (!string.IsNullOrEmpty(computeShader))
            {
                pass.ComputeShader = CreateShaderModule(ShaderStage.Compute, computeShader);
            }

            pass.Build();
            return pass;
        }

        public static Pass CreatePass(string computeShader)
        {
            Pass pass = new Pass();
            if (!string.IsNullOrEmpty(computeShader))
            {
                pass.ComputeShader = CreateShaderModule(ShaderStage.Compute, computeShader);
            }

            pass.Build();
            return pass;
        }

        public static ShaderModule CreateShaderModule(ShaderStage stage, string fileName, string funcName = "main")
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
