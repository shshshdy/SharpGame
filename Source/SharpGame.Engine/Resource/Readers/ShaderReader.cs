﻿#define NEW_RELECTION
using SharpShaderCompiler;
using SharpSPIRVCross;
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
            FileSystem.AddResourceDir(filePath + "Common");
            FileSystem.AddResourceDir(filePath + "GLSL");

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

            int propCount = node.GetChild("Properties", out var properties);
            if (propCount > 0)
            {
                foreach (var prop in properties)
                {
                    ReadShaderProperties(shader, prop);
                }
            }

            return true;
        }

        bool ReadShaderProperties(Shader shader, AstNode node)
        {
            if(node.Children == null)
            {
                return false;
            }

            if(shader.Properties == null)
            {
                shader.Properties = new Dictionary<string, ShaderProperty>();
            }

            foreach (var kvp in node.Children)
            {
                if(ReadProperty(shader, kvp.Key, kvp.Value[0].value, out var prop))
                {
                    shader.Properties.Add(kvp.Key, prop);
                }

            }

            return true;
        }

        bool ReadProperty(Shader shader, string key, string val, out ShaderProperty prop)
        {
            foreach (var pass in shader.Pass)
            {
                var binding = pass.PipelineLayout.GetBinding(key);
                if(binding == null)
                {
                    Log.Warn("Unknown property : " + key);
                    continue;
                }

                if (binding.IsTexture)
                {
                    prop = new ShaderProperty
                    {
                        type = UniformType.Texture,
                        value = val
                    };
                    return true;
                }

                if(pass.GetPushConstant(key, out var pushConst))
                {
                    prop = new ShaderProperty
                    {
                        //type = UniformType.Texture,
                        //value = val
                    };
                    return true;
                }

            }
            
            prop = default;
            return false;

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

//                     case "ResourceLayout":
//                         pass.PipelineLayout.ResourceLayout = ReadResourceLayout(kvp.Value);
//                         break;

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

                    case "TessControl":
                        pass.HullShader = LoadShaderModelFromFile(ShaderStage.TessControl, kvp.Value[0].value, pass.Defines);
                        break;

                    case "TessEvaluation":
                        pass.DomainShader = LoadShaderModelFromFile(ShaderStage.TessEvaluation, kvp.Value[0].value, pass.Defines);
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
                    case "@TessControl":
                        pass.HullShader = LoadShaderModel(ShaderStage.TessControl, kvp.Value[0].value, pass.Defines);
                        break;
                    case "@TessEvaluation":
                        pass.DomainShader = LoadShaderModel(ShaderStage.TessEvaluation, kvp.Value[0].value, pass.Defines);
                        break;
                }
            }

            return pass;
        }

        DescriptorSetLayout[] ReadResourceLayout(List<AstNode> layout)
        {
            List<DescriptorSetLayout> layouts = new List<DescriptorSetLayout>();
            foreach(var node in layout)
            {
                var resLayout = ReadResourceLayout(node);
                resLayout.Set = layouts.Count;
                layouts.Add(resLayout);
            }
            return layouts.ToArray(); ;
        }

        DescriptorSetLayout ReadResourceLayout(AstNode node)
        {
            DescriptorSetLayout layout = new DescriptorSetLayout();            
            node.GetChild("ResourceLayoutBinding", out var resourceLayoutBinding);

            foreach(var c in resourceLayoutBinding)
            {
                DescriptorSetLayoutBinding binding = new DescriptorSetLayoutBinding
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
            pass.PipelineLayout.PushConstantNames = new List<string>();
            List<PushConstantRange> layouts = new List<PushConstantRange>();
            foreach (var node in layout)
            {
                if (!string.IsNullOrEmpty(node.value))
                {
                    pass.PipelineLayout.PushConstantNames.Add(node.value);
                }
                else
                {
                    Debug.Assert(false);
                    pass.PipelineLayout.PushConstantNames.Add(string.Empty);
                }

                layouts.Add(ReadPushConstant(node));
            }

            pass.PipelineLayout.PushConstant = layouts.ToArray(); ;
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

        static Dictionary<string, IncludeResult> includes = new Dictionary<string, IncludeResult>();
        static IncludeResult IncludeHandler(string requestedSource, string requestingSource, CompileOptions.IncludeType type)
        {
            if(includes.TryGetValue(requestedSource, out var res))
            {
                return res;
            }

            using (var file = FileSystem.Instance.GetFile(requestedSource))
            {
                var content = file.ReadAllText();
                res = new IncludeResult(requestedSource, content);
                includes[requestedSource] = res;
                return res;
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
            List<string> saveLines = new List<string>();
            string ver = "";
            string includeFile = "";
            StringReader reader = new StringReader(code);

            while (true)
            {
                string line = reader.ReadLine();
                if (line == null)
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
                    var incFile = str.Substring(8);
                    if(string.IsNullOrEmpty(includeFile))
                    {
                        includeFile = incFile;
                    }

                    if (ReadInclude(incFile, saveLines, out ver))
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

            if (defs != null)
            {
                foreach (var def in defs)
                {
                    sb.Append("#define ").Append(def);
                }
            }

            foreach (var line in saveLines)
            {
                sb.AppendLine(line);
            }

            sb.Append(reader.ReadToEnd());

            code = sb.ToString();

            return CreateShaderModule(shaderStage, code, includeFile);
        }

        public static ShaderReflection ReflectionShaderModule(string source, byte[] bytecode)
        {
#if NEW_RELECTION
            ShaderReflection refl = new ShaderReflection();
            refl.descriptorSets = new List<UniformBlock>();

            using (var context = new SharpSPIRVCross.Context())
            {
                var ir = context.ParseIr(bytecode);
                var compiler = context.CreateCompiler(Backend.GLSL, ir);

                var caps = compiler.GetDeclaredCapabilities();
                var extensions = compiler.GetDeclaredExtensions();
                var resources = compiler.CreateShaderResources();
                foreach (var uniformBuffer in resources.GetResources(SharpSPIRVCross.ResourceType.UniformBuffer))
                {
                    Console.WriteLine($"ID: {uniformBuffer.Id}, BaseTypeID: {uniformBuffer.BaseTypeId}, TypeID: {uniformBuffer.TypeId}, Name: {uniformBuffer.Name})");
                    var set = compiler.GetDecoration(uniformBuffer.Id, SpvDecoration.DescriptorSet);
                    var binding = compiler.GetDecoration(uniformBuffer.Id, SpvDecoration.Binding);
                    var type = compiler.GetSpirvType(uniformBuffer.TypeId);
                    Console.WriteLine($"  Set: {set}, Binding: {binding}");

                    var descriptorType = DescriptorType.UniformBuffer;
                    if (uniformBuffer.Name.EndsWith("dynamic"))
                    {
                        descriptorType = DescriptorType.UniformBufferDynamic;
                    }

                    UniformBlock u = new UniformBlock
                    {
                        name = uniformBuffer.Name,
                        set = (int)set,
                        binding = binding,
                        descriptorType = descriptorType
                    };

                    refl.descriptorSets.Add(u);
                }

                foreach (var input in resources.GetResources(SharpSPIRVCross.ResourceType.StageInput))
                {
                    Console.WriteLine($"ID: {input.Id}, BaseTypeID: {input.BaseTypeId}, TypeID: {input.TypeId}, Name: {input.Name})");
                    var location = compiler.GetDecoration(input.Id, SpvDecoration.Location);
                    Console.WriteLine($"  Location: {location}");
                }

                foreach (var sampledImage in resources.GetResources(SharpSPIRVCross.ResourceType.SampledImage))
                {
                    var set = compiler.GetDecoration(sampledImage.Id, SpvDecoration.DescriptorSet);
                    var binding = compiler.GetDecoration(sampledImage.Id, SpvDecoration.Binding);
                    var base_type = compiler.GetSpirvType(sampledImage.BaseTypeId);
                    var type = compiler.GetSpirvType(sampledImage.TypeId);
                    Console.WriteLine($"  Set: {set}, Binding: {binding}");

                    UniformBlock u = new UniformBlock
                    {
                        name = sampledImage.Name,
                        set = (int)set,
                        binding = binding,
                        descriptorType = DescriptorType.CombinedImageSampler
                    };
                    refl.descriptorSets.Add(u);
                }

                //compiler.Options.SetOption(CompilerOption.GLSL_Version, 50);
                //var glsl_source = compiler.Compile();
            }

            return refl;
#else


            LayoutParser layoutParser = new LayoutParser(source);
            var refl = layoutParser.Reflection();
            return refl;
#endif
        }

        public static ShaderModule CreateShaderModule(ShaderStage shaderStage, string code, string includeFile)
        {
            ShaderCompiler.Stage stage = ShaderCompiler.Stage.Vertex;
            switch (shaderStage)
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

            var c = new ShaderCompiler();
            CompileOptions o = new CompileOptions(IncludeHandler)
            {
                Language = CompileOptions.InputLanguage.GLSL,
                Target = CompileOptions.Environment.Vulkan,
            };

            var r = c.Preprocess(code, stage, o, "main");
            if (r.NumberOfErrors > 0)
            {
                Log.Error(r.ErrorMessage);
            }

            if (r.CompileStatus != CompileResult.Status.Success)
            {
                return null;
            }

            var source = r.GetString();

            var res = c.Compile(source, stage, o, includeFile, "main");
            if (res.NumberOfErrors > 0)
            {
                Log.Error(res.ErrorMessage);
            }

            if (res.CompileStatus != CompileResult.Status.Success)
            {
                return null;
            }

            var refl = ReflectionShaderModule(source, res.GetBytes());

            var shaderModule = new ShaderModule(shaderStage, res.GetBytes())
            {
                ShaderReflection = refl
            };

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
