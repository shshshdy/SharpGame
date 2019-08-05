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
            var filePath = FileUtil.StandardlizeFile(stream.Name);
            filePath = FileUtil.GetPath(filePath);
            FileSystem.AddResourceDir(filePath);

            try
            {
                string text = stream.ReadAllText();
                AstParser ast = new AstParser();
                if (ast.Parse(text))
                {
                    var node = ast.Root[0];
                    return LoadShader(resource, node);
                }
            }
            catch(Exception e)
            {
                Log.Error(e.Message);
            }
            finally
            {
                FileSystem.RemoveResourceDir(filePath);
            }


            return false;
        }

        bool LoadShader(Shader shader, AstNode node)
        {
            if (!string.IsNullOrEmpty(node.value))
            {
                shader.Name = node.value;
            }

            int passCount = node.GetChild("Pass", out var children);
            foreach (var passNode in children)
            {
                var pass = LoadPass(passNode);
                if (pass != null)
                {
                    shader.Add(pass);
                }
            }
            shader.Build();
            return true;
        }

        Pass LoadPass(AstNode node)
        {
            Pass pass = new Pass();
            if (!string.IsNullOrEmpty(node.value))
            {
                pass.Name = node.value;
            }
            else
            {
                pass.Name = "";
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

                    case "DepthTestEnable":
                        pass.DepthTestEnable = bool.Parse(kvp.Value[0].value);
                        break;

                    case "DepthWriteEnable":
                        pass.DepthWriteEnable = bool.Parse(kvp.Value[0].value);
                        break;

                    case "BlendMode":
                        pass.BlendMode = (BlendMode)Enum.Parse(typeof(BlendMode), kvp.Value[0].value);
                        break;

                    case "VertexShader":
                        pass.VertexShader = new ShaderModule(ShaderStage.Vertex, kvp.Value[0].value);
                        break;

                    case "PixelShader":
                        pass.PixelShader = new ShaderModule(ShaderStage.Fragment, kvp.Value[0].value);
                        break;

                    case "ResourceLayout":
                        pass.ResourceLayout = ReadResourceLayout(kvp.Value);
                        break;

                    case "PushConstant":
                        pass.PushConstant = ReadPushConstant(kvp.Value);
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

        ResourceLayout[] ReadResourceLayout(List<AstNode> layout)
        {
            List<ResourceLayout> layouts = new List<ResourceLayout>();
            foreach(var node in layout)
            {
                layouts.Add(ReadResourceLayout(node));
            }
            return layouts.ToArray(); ;
        }

        ResourceLayout ReadResourceLayout(AstNode node)
        {
            ResourceLayout layout = new ResourceLayout();
            node.GetChild("ResourceLayoutBinding", out var resourceLayoutBinding);
            foreach(var c in resourceLayoutBinding)
            {
                ResourceLayoutBinding binding = new ResourceLayoutBinding();
                foreach (var kvp in c.Children)
                {
                    switch (kvp.Key)
                    {
                        case "binding":
                            binding.binding = uint.Parse(kvp.Value[0].value);
                            break;
                        case "descriptorType":
                            binding.descriptorType = (DescriptorType)Enum.Parse(typeof(DescriptorType), kvp.Value[0].value);
                            break;
                        case "stageFlags":
                            binding.stageFlags = (ShaderStage)Enum.Parse(typeof(ShaderStage), kvp.Value[0].value);
                            break;
                        case "descriptorCount":
                            binding.descriptorCount = uint.Parse(kvp.Value[0].value);
                            break;
                    }
                }

                layout.Add(binding);
            }
        

            return layout;
        }

        PushConstantRange[] ReadPushConstant(List<AstNode> layout)
        {
            List<PushConstantRange> layouts = new List<PushConstantRange>();
            foreach (var node in layout)
            {
                layouts.Add(ReadPushConstant(node));
            }
            return layouts.ToArray(); ;
        }

        PushConstantRange ReadPushConstant(AstNode node)
        {
            PushConstantRange layout = new PushConstantRange();
            foreach (var kvp in node.Children)
            {
                switch (kvp.Key)
                {
                    case "stageFlags":
                        layout.stageFlags = (ShaderStage)Enum.Parse(typeof(ShaderStage), kvp.Value[0].value);
                        break;
                    case "offset":
                        layout.offset = int.Parse(kvp.Value[0].value);
                        break;
                    case "size":
                        layout.size = int.Parse(kvp.Value[0].value);
                        break;
                }
            }

            return layout;
        }

        IncludeResult IncludeHandler(string requestedSource, string requestingSource, CompileOptions.IncludeType type)
        {
            using (var file = FileSystem.GetFile(requestedSource))
            {
                var content = file.ReadAllText();
                return new IncludeResult(requestedSource, content);
            }

        }

        ShaderModule LoadShaderModel(ShaderStage shaderStage, string code)
        {
            var c = new ShaderCompiler();
            var o = new CompileOptions();

            o.Language = CompileOptions.InputLanguage.GLSL;
            o.Target = CompileOptions.Environment.Vulkan;
            o.IncludeCallback = IncludeHandler;

            ShaderCompiler.Stage stage = ShaderCompiler.Stage.Vertex;
            switch(shaderStage)
            {
                case ShaderStage.Vertex:
                    stage = ShaderCompiler.Stage.Vertex;
                    break;
                case ShaderStage.Fragment:
                    stage = ShaderCompiler.Stage.Fragment;
                    break;
                case ShaderStage.Geometry:
                    stage = ShaderCompiler.Stage.Geometry;
                    break;
                case ShaderStage.Compute:
                    stage = ShaderCompiler.Stage.Compute;
                    break;
                case ShaderStage.TessControl:
                    stage = ShaderCompiler.Stage.TessControl;
                    break;
                case ShaderStage.TessEvaluation:
                    stage = ShaderCompiler.Stage.TessEvaluation;
                    break;
            }

            var r = c.Preprocess(code, stage, o, "main");
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

            var res = c.Compile(bc, stage, o, "main");
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
