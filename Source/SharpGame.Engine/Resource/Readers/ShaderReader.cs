using SharpShaderCompiler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            filePath = FileUtil.GetPath(stream.Name);           
            FileSystem.AddResourceDir(filePath);
            FileSystem.AddResourceDir(filePath + "GLSL");
            FileSystem.AddResourceDir(filePath + "Common");

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
                FileSystem.RemoveResourceDir(filePath + "GLSL");
                FileSystem.RemoveResourceDir(filePath + "Common");
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

                    case "DepthTest":
                    case "DepthTestEnable":
                        pass.DepthTestEnable = bool.Parse(kvp.Value[0].value);
                        break;

                    case "DepthWrite":
                    case "DepthWriteEnable":
                        pass.DepthWriteEnable = bool.Parse(kvp.Value[0].value);
                        break;

                    case "BlendMode":
                        pass.BlendMode = (BlendMode)Enum.Parse(typeof(BlendMode), kvp.Value[0].value);
                        break;

                    case "ResourceLayout":
                        pass.ResourceLayout = ReadResourceLayout(kvp.Value);
                        break;

                    case "PushConstant":
                        ReadPushConstant(pass, kvp.Value);
                        break;

                    case "VertexShader":
                        pass.VertexShader = LoadShaderModelFromFile(ShaderStage.Vertex, kvp.Value[0].value, pass.Defines);
                        break;

                    case "PixelShader":
                        pass.PixelShader = LoadShaderModelFromFile(ShaderStage.Fragment, kvp.Value[0].value, pass.Defines);
                        break;

                    case "ComputeShader":
                        pass.ComputeShader = LoadShaderModelFromFile(ShaderStage.Compute, kvp.Value[0].value, pass.Defines);
                        break;

                    case "GeometryShader":
                        pass.GeometryShader = LoadShaderModelFromFile(ShaderStage.Geometry, kvp.Value[0].value, pass.Defines);
                        break;

                    case "@VertexShader":
                        pass.VertexShader = LoadShaderModel(ShaderStage.Vertex, kvp.Value[0].value, pass.Defines);
                        break;
                    case "@PixelShader":
                        pass.PixelShader = LoadShaderModel(ShaderStage.Fragment, kvp.Value[0].value, pass.Defines);
                        break;
                    case "@ComputeShader":
                        pass.ComputeShader = LoadShaderModel(ShaderStage.Compute, kvp.Value[0].value, pass.Defines);
                        break;
                    case "@GeometryShader":
                        pass.GeometryShader = LoadShaderModel(ShaderStage.Geometry, kvp.Value[0].value, pass.Defines);
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
                var resLayout = ReadResourceLayout(node);
                resLayout.Set = layouts.Count;
                layouts.Add(resLayout);
            }
            return layouts.ToArray(); ;
        }

        ResourceLayout ReadResourceLayout(AstNode node)
        {
            ResourceLayout layout = new ResourceLayout();            
            layout.PerMaterial = (string.Compare(node.value, "PerMaterial", true) == 0);            

            node.GetChild("ResourceLayoutBinding", out var resourceLayoutBinding);
            foreach(var c in resourceLayoutBinding)
            {
                ResourceLayoutBinding binding = new ResourceLayoutBinding
                {
                    binding = (uint)layout.Bindings.Count
                };

                if (!string.IsNullOrEmpty(c.value))
                {
                    binding.name = c.value;
                }

                foreach (var kvp in c.Children)
                {                   
                    switch (kvp.Key)
                    {
                        case "Binding":
                            binding.binding = uint.Parse(kvp.Value[0].value);
                            break;
                        case "DescriptorType":
                            binding.descriptorType = (DescriptorType)Enum.Parse(typeof(DescriptorType), kvp.Value[0].value);
                            break;
                        case "StageFlags":
                            binding.stageFlags = (ShaderStage)Enum.Parse(typeof(ShaderStage), kvp.Value[0].value);
                            break;
                        case "DescriptorCount":
                            binding.descriptorCount = uint.Parse(kvp.Value[0].value);
                            break;
                    }
                }

                layout.Add(binding);
            }        

            return layout;
        }

        void ReadPushConstant(Pass pass, List<AstNode> layout)
        {
            pass.PushConstantNames = new List<string>();
            List<PushConstantRange> layouts = new List<PushConstantRange>();
            foreach (var node in layout)
            {
                if (!string.IsNullOrEmpty(node.value))
                {
                    pass.PushConstantNames.Add(node.value);
                }
                else
                {
                    Debug.Assert(false);
                    pass.PushConstantNames.Add(string.Empty);
                }

                layouts.Add(ReadPushConstant(node));
            }

            pass.PushConstant = layouts.ToArray(); ;
        }

        PushConstantRange ReadPushConstant(AstNode node)
        {
            PushConstantRange layout = new PushConstantRange();
            foreach (var kvp in node.Children)
            {
                switch (kvp.Key)
                {
                    case "StageFlags":
                        layout.stageFlags = (ShaderStage)Enum.Parse(typeof(ShaderStage), kvp.Value[0].value);
                        break;
                    case "Offset":
                        layout.offset = int.Parse(kvp.Value[0].value);
                        break;
                    case "Size":
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

        ShaderModule LoadShaderModelFromFile(ShaderStage shaderStage, string file, string[] defs)
        {
            using (File stream = FileSystem.Instance.GetFile(file))
            {
                return LoadShaderModel(shaderStage, stream.ReadAllText(), defs);
            }
        }

        ShaderModule LoadShaderModel(ShaderStage shaderStage, string code, string[] defs)
        {
            var c = new ShaderCompiler();
            var o = new CompileOptions
            {
                Language = CompileOptions.InputLanguage.GLSL,
                Target = CompileOptions.Environment.Vulkan,
                IncludeCallback = IncludeHandler
            };

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

            List<string> saveLines = new List<string>();
            string ver = "";

            StringReader reader = new StringReader(code);

            while(true)
            {
                string line = reader.ReadLine();
                if(line == null)
                {
                    break;
                }

                string str = line.TrimStart();
                if (str.StartsWith("#version"))
                {
                    ver = line;
                    break;
                }

                if (str.StartsWith("#include"))
                {
                    if(ReadInclude(str.Substring(8), saveLines, out ver))
                    {
                        break;
                    }
                }
                else
                {
                    saveLines.Add(line);
                }
            }



            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ver);
            //sb.Append("#define VULKAN 1");

            if (defs != null)
            {
                foreach (var def in defs)
                {
                    sb.Append("#define ").Append(def);
                }
            }

            foreach(var line in saveLines)
            {
                sb.AppendLine(line);
            }

            sb.Append(reader.ReadToEnd());

            code = sb.ToString();

            var r = c.Preprocess(code, stage, o, "main");
            if(r.NumberOfErrors > 0)
            {
                Log.Error(r.ErrorMessage);
            }

            if(r.CompileStatus != CompileResult.Status.Success)
            {
                return null;
            }

            var source = r.GetString();

            LayoutParser layoutParser = new LayoutParser(source);
            var refl = layoutParser.Reflection();

            var res = c.Compile(source, stage, o, "main");
            if (res.NumberOfErrors > 0)
            {
                Log.Error(res.ErrorMessage);
            }

            if (res.CompileStatus != CompileResult.Status.Success)
            {
                return null;
            }

            var shaderModule = new ShaderModule(shaderStage, res.GetBytes());
            shaderModule.ShaderReflection = refl;
            return shaderModule;
        }

        bool ReadInclude(string path, List<string> strs, out string ver)
        {
            path = path.Trim(new char[] {' ', '\t', '"' });

            ver = "";

            using (File file = FileSystem.OpenFile(path))
            {
                if(file == null)
                {
                    return false;
                }

                using (StreamReader sr = new StreamReader(file))
                {
                    while(!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();

                        if (line.TrimStart().StartsWith("#version"))
                        {
                            ver = line;
                            strs.Add(sr.ReadToEnd());
                            break;
                        }
                        else
                        {
                            strs.Add(line);
                        }

                    }

                }

            }

            return !string.IsNullOrEmpty(ver);
        }

    }

}
