using SharpShaderCompiler;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ShaderReader : ResourceReader<Shader>
    {
        public ShaderReader() : base(".shader")
        {
        }

        protected override bool OnLoad(Shader resource, File stream)
        {
            string text = stream.ReadAllText();
            AstParser ast = new AstParser();
            if (ast.Parse(text))
            {
                var node = ast.Root[0];
                return LoadShader(resource, node);
            }

            return false;
        }

        bool LoadShader(Shader shader, AstNode node)
        {
            int passCount = node.GetChild("Pass", out var children);
            foreach (var passNode in children)
            {
                var pass = LoadPass(passNode);
                if(pass != null)
                    shader.Pass.Add(pass);
            }

            return true;
        }

        Pass LoadPass(AstNode node)
        {
            Pass pass = new Pass();

            if(!string.IsNullOrEmpty(node.value))
            {
                pass.Name = node.value;
            }

            foreach (var kvp in node.Children)
            {
                switch (kvp.Key)
                {
                    case "FillMode":
                        pass.FillMode = (PolygonMode)Enum.Parse(typeof(PolygonMode), kvp.Value[0].value);
                        break;

                    case "CullMode":
                        pass.CullMode = (CullMode)Enum.Parse(typeof(CullMode), kvp.Value[0].value);
                        break;

                    case "FrontFace":
                        pass.FrontFace = (FrontFace)Enum.Parse(typeof(FrontFace), kvp.Value[0].value);
                        break;

                    case "VertexShader":
                        pass.VertexShader = new ShaderModule(ShaderStage.Vertex, kvp.Value[0].value);
                        break;

                    case "PixelShader":
                        pass.PixelShader = new ShaderModule(ShaderStage.Fragment, kvp.Value[0].value);
                        break;

                    case "ResourceLayout":
                        pass.PixelShader = new ShaderModule(ShaderStage.Fragment, kvp.Value[0].value);
                        break;
                    case "PushConstant":
                        pass.PixelShader = new ShaderModule(ShaderStage.Fragment, kvp.Value[0].value);
                        break;

                    case "@VertexShader":
                        pass.VertexShader = LoadShaderModel(ShaderStage.Vertex, kvp.Value[0].value);
                        break;
                    case "@PixelShader":
                        pass.PixelShader = LoadShaderModel(ShaderStage.Fragment, kvp.Value[0].value);
                        break;
                }
            }

            return pass;
        }

        IncludeResult IncludeHandler(string requestedSource, string requestingSource, CompileOptions.IncludeType type)
        {
            return new IncludeResult(requestedSource, "");
        }

        ShaderModule LoadShaderModel(ShaderStage shaderStage, string code)
        {
            var c = new ShaderCompiler();
            var o = new CompileOptions();

            o.Language = CompileOptions.InputLanguage.GLSL;
            o.IncludeCallback = IncludeHandler;

            var r = c.Preprocess(code, ShaderCompiler.Stage.Vertex, o, "main");
            if(r.NumberOfErrors > 0)
            {
                Log.Error(r.ErrorMessage);
            }

            if(r.CompileStatus != CompileResult.Status.Success)
            {
                return null;
            }

            var bc = r.GetString();
            //todo: parse shader

            var res = c.Compile(bc, ShaderCompiler.Stage.Vertex, o, "main");
            if (res.NumberOfErrors > 0)
            {
                Log.Error(res.ErrorMessage);
            }

            if (res.CompileStatus != CompileResult.Status.Success)
            {
                return null;
            }

            return new ShaderModule(shaderStage, res.GetBytes());
        }
    }

}
