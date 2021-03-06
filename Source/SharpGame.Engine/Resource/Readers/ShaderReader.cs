﻿#define SHARP_SHADER_COMPILER

using SharpSPIRVCross;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

#if SHARP_SHADER_COMPILER
using SharpShaderCompiler;
#else
using shaderc;
#endif

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
                        pass.FillMode = (VkPolygonMode)Enum.Parse(typeof(VkPolygonMode), kvp.Value[0].value);
                        break;

                    case "CullMode":
                        pass.CullMode = (VkCullModeFlags)Enum.Parse(typeof(VkCullModeFlags), kvp.Value[0].value);
                        break;

                    case "FrontFace":
                        pass.FrontFace = (VkFrontFace)Enum.Parse(typeof(VkFrontFace), kvp.Value[0].value);
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

                    //case "PushConstant":
                    //    ReadPushConstant(pass, kvp.Value);
                   //     break;

                    case "VertexShader":
                        pass.VertexShader = LoadShaderModelFromFile(VkShaderStageFlags.Vertex, kvp.Value[0].value, pass.Defines);
                        break;

                    case "PixelShader":
                        pass.PixelShader = LoadShaderModelFromFile(VkShaderStageFlags.Fragment, kvp.Value[0].value, pass.Defines);
                        break;

                    case "ComputeShader":
                        pass.ComputeShader = LoadShaderModelFromFile(VkShaderStageFlags.Compute, kvp.Value[0].value, pass.Defines);
                        break;

                    case "GeometryShader":
                        pass.GeometryShader = LoadShaderModelFromFile(VkShaderStageFlags.Geometry, kvp.Value[0].value, pass.Defines);
                        break;

                    case "TessControl":
                        pass.TessControlShader = LoadShaderModelFromFile(VkShaderStageFlags.TessellationControl, kvp.Value[0].value, pass.Defines);
                        break;

                    case "TessEvaluation":
                        pass.TessEvaluationShader = LoadShaderModelFromFile(VkShaderStageFlags.TessellationEvaluation, kvp.Value[0].value, pass.Defines);
                        break;

                    case "@VertexShader":
                        pass.VertexShader = LoadShaderModel(VkShaderStageFlags.Vertex, kvp.Value[0].value, pass.Defines);
                        break;
                    case "@PixelShader":
                        pass.PixelShader = LoadShaderModel(VkShaderStageFlags.Fragment, kvp.Value[0].value, pass.Defines);
                        break;
                    case "@ComputeShader":
                        pass.ComputeShader = LoadShaderModel(VkShaderStageFlags.Compute, kvp.Value[0].value, pass.Defines);
                        break;
                    case "@GeometryShader":
                        pass.GeometryShader = LoadShaderModel(VkShaderStageFlags.Geometry, kvp.Value[0].value, pass.Defines);
                        break;
                    case "@TessControl":
                        pass.TessControlShader = LoadShaderModel(VkShaderStageFlags.TessellationControl, kvp.Value[0].value, pass.Defines);
                        break;
                    case "@TessEvaluation":
                        pass.TessEvaluationShader = LoadShaderModel(VkShaderStageFlags.TessellationEvaluation, kvp.Value[0].value, pass.Defines);
                        break;
                }
            }

            return pass;
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
                        layout.stageFlags = (VkShaderStageFlags)Enum.Parse(typeof(VkShaderStageFlags), kvp.Value[0].value);
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

        ShaderModule LoadShaderModelFromFile(VkShaderStageFlags shaderStage, string file, string[] defs)
        {
            using (File stream = FileSystem.Instance.GetFile(file))
            {
                return LoadShaderModel(shaderStage, stream.ReadAllText(), defs);
            }
        }

        ShaderModule LoadShaderModel(VkShaderStageFlags shaderStage, string code, string[] defs)
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

        public static ShaderModule CreateShaderModule(VkShaderStageFlags shaderStage, string code, string includeFile)
        {
#if SHARP_SHADER_COMPILER
            ShaderCompiler.Stage stage = ShaderCompiler.Stage.Vertex;
            switch (shaderStage)
            {
                case VkShaderStageFlags.Vertex:
                    stage = ShaderCompiler.Stage.Vertex;
                    break;
                case VkShaderStageFlags.Fragment:
                    stage = ShaderCompiler.Stage.Fragment;
                    break;
                case VkShaderStageFlags.Geometry:
                    stage = ShaderCompiler.Stage.Geometry;
                    break;
                case VkShaderStageFlags.Compute:
                    stage = ShaderCompiler.Stage.Compute;
                    break;
                case VkShaderStageFlags.TessellationControl:
                    stage = ShaderCompiler.Stage.TessControl;
                    break;
                case VkShaderStageFlags.TessellationEvaluation:
                    stage = ShaderCompiler.Stage.TessEvaluation;
                    break;
            }

            var c = new ShaderCompiler();
            var o = new CompileOptions(IncludeHandler)
            {
                Language = CompileOptions.InputLanguage.GLSL,
                Target = CompileOptions.Environment.Vulkan,
                GenerateDebug = true,
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

            uint len = res.GetCode(out var codePointer);
            var refl = ReflectionShaderModule(source, codePointer, len/4);
            var shaderModule = new ShaderModule(shaderStage, codePointer, len)
            {
                ShaderReflection = refl
            };
#else
            shaderc.ShaderKind stage = shaderc.ShaderKind.VertexShader;
            switch (shaderStage)
            {
                case VkShaderStageFlags.Vertex:
                    stage = shaderc.ShaderKind.VertexShader;
                    break;
                case VkShaderStageFlags.Fragment:
                    stage = shaderc.ShaderKind.FragmentShader;
                    break;
                case VkShaderStageFlags.Geometry:
                    stage = shaderc.ShaderKind.GeometryShader;
                    break;
                case VkShaderStageFlags.Compute:
                    stage = shaderc.ShaderKind.ComputeShader;
                    break;
                case VkShaderStageFlags.TessControl:
                    stage = shaderc.ShaderKind.TessControlShader;
                    break;
                case VkShaderStageFlags.TessEvaluation:
                    stage = shaderc.ShaderKind.TessEvaluationShader;
                    break;
            }

            shaderc.Compiler.GetSpvVersion(out shaderc.SpirVVersion version, out uint revision);
            
            shaderc.Options o = new shaderc.Options() //new ShadercOptions()
            {
                //SourceLanguage = shaderc.SourceLanguage.Glsl,
                //TargetSpirVVersion = new shaderc.SpirVVersion(1,5),    
                //Optimization = shaderc.OptimizationLevel.Performance
            };

            o.EnableDebugInfo();
            
            o.IncludeDirectories.Add(FileSystem.WorkSpace + "data/shaders/common");
            o.IncludeDirectories.Add(FileSystem.WorkSpace + "data/shaders/glsl");

            var c = new shaderc.Compiler(o);
            var res = c.Compile(code, includeFile, stage); 
            
            if (res.ErrorCount > 0)
            {
                Log.Error(res.ErrorMessage);
            }

            if (res.Status != Status.Success)
            {
                return null;
            }

            var refl = ReflectionShaderModule(code, res.CodePointer, res.CodeLength/4);
            var shaderModule = new ShaderModule(shaderStage, res.CodePointer, res.CodeLength)
            {
                ShaderReflection = refl
            };
#endif


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

        static Dictionary<string, object> includes = new Dictionary<string, object>();

#if SHARP_SHADER_COMPILER
        static IncludeResult IncludeHandler(string requestedSource, string requestingSource, CompileOptions.IncludeType type)
        {
            if(includes.TryGetValue(requestedSource, out var res))
            {
                return res as IncludeResult;
            }

            using (var file = FileSystem.Instance.GetFile(requestedSource))
            {
                var content = file.ReadAllText();
                res = new IncludeResult(requestedSource, content);
                includes[requestedSource] = res;
                return res as IncludeResult;
            }

        }
#else

        class ShadercOptions : shaderc.Options
        {
            protected override bool TryFindInclude(string source, string include, IncludeType incType, out string incFile, out string incContent)
            {
                incFile = include;
                if (includes.TryGetValue(include, out var res))
                {
                    incContent = res as string;
                    return false;
                }

                using var file = FileSystem.Instance.GetFile(include);
                var code = file.ReadAllText();
                includes[incFile] = code;
                incContent = code;
                return false;
            }
        }

#endif

        public static ShaderReflection ReflectionShaderModule(string source, IntPtr bytecode, uint len)
        {
            ShaderReflection refl = new ShaderReflection();
            refl.descriptorSets = new List<ShaderResourceInfo>();

            using (var context = new SharpSPIRVCross.Context())
            {
                var ir = context.ParseIr(bytecode, len);
                var compiler = context.CreateCompiler(Backend.GLSL, ir);

                var caps = compiler.GetDeclaredCapabilities();
                var extensions = compiler.GetDeclaredExtensions();
                var resources = compiler.CreateShaderResources();

                Action<SharpSPIRVCross.ResourceType> CollectShaderResources = (SharpSPIRVCross.ResourceType resourceType) =>
                {
                    if (resourceType == SharpSPIRVCross.ResourceType.PushConstant)
                    {
                        foreach (var rs in resources.GetResources(resourceType))
                        {
                            var set = compiler.GetDecoration(rs.Id, SpvDecoration.DescriptorSet);
                            var binding = compiler.GetDecoration(rs.Id, SpvDecoration.Binding);
                            var type = compiler.GetSpirvType(rs.TypeId);

                            if (type.MemberCount > 0)
                            {
                                refl.pushConstants = new List<BufferMember>();
                                compiler.GetDeclaredStructSize(type, out int size);

                                //Console.WriteLine($"  struct, size:{size}");
                                //u.size = (uint)size;
                                for (int i = 0; i < type.MemberCount; i++)
                                {
                                    var memberName = compiler.GetMemberName(rs.BaseTypeId, (uint)i);

                                    compiler.GetStructMemberOffset(type, i, out int offset);
                                    compiler.GetDeclaredStructMemberSize(type, i, out int memberSize);
                                    //Console.WriteLine($"  Member Name:{memberName}, Offset:{offset}, size：{memberSize}");
                                    refl.pushConstants.Add(new BufferMember { name = memberName, offset = offset, size = memberSize });

                                }
                            }
                        }


                         return;
                    }



                    foreach (var rs in resources.GetResources(resourceType))
                    {
                        //Console.WriteLine($"ID: {rs.Id}, BaseTypeID: {rs.BaseTypeId}, TypeID: {rs.TypeId}, Name: {rs.Name})");
                        var set = compiler.GetDecoration(rs.Id, SpvDecoration.DescriptorSet);
                        var binding = compiler.GetDecoration(rs.Id, SpvDecoration.Binding);
                        var type = compiler.GetSpirvType(rs.TypeId);
                        //Console.WriteLine($"  Set: {set}, Binding: {binding}");

                        bool isDynamic = rs.Name.EndsWith("dynamic", StringComparison.CurrentCultureIgnoreCase);
                        bool isInline = rs.Name.EndsWith("inline", StringComparison.CurrentCultureIgnoreCase);

                        var descriptorType = resourceType switch
                        {
                            SharpSPIRVCross.ResourceType.UniformBuffer => isInline ? VkDescriptorType.InlineUniformBlockEXT 
                                :(isDynamic ? VkDescriptorType.UniformBufferDynamic : VkDescriptorType.UniformBuffer),
                            SharpSPIRVCross.ResourceType.StorageBuffer => isDynamic ? VkDescriptorType.StorageBufferDynamic : VkDescriptorType.StorageBuffer,
                            SharpSPIRVCross.ResourceType.StageInput => throw new NotImplementedException(),
                            SharpSPIRVCross.ResourceType.StageOutput => throw new NotImplementedException(),
                            SharpSPIRVCross.ResourceType.SubpassInput => VkDescriptorType.InputAttachment,
                            SharpSPIRVCross.ResourceType.StorageImage => type.ImageDimension == SpvDim.DimBuffer ? VkDescriptorType.StorageTexelBuffer : VkDescriptorType.StorageImage,
                            SharpSPIRVCross.ResourceType.SampledImage => VkDescriptorType.CombinedImageSampler,
                            SharpSPIRVCross.ResourceType.AtomicCounter => throw new NotImplementedException(),
                            SharpSPIRVCross.ResourceType.PushConstant => throw new NotImplementedException(),
                            SharpSPIRVCross.ResourceType.SeparateImage => VkDescriptorType.SampledImage,
                            SharpSPIRVCross.ResourceType.SeparateSamplers => VkDescriptorType.Sampler,
                            SharpSPIRVCross.ResourceType.AccelerationStructure => throw new NotImplementedException(),
                            _ => throw new NotImplementedException(),
                        };

                        var u = new ShaderResourceInfo
                        {
                            name = rs.Name,
                            set = (int)set,
                            binding = binding,
                            descriptorType = descriptorType
                        };

                        if(type.MemberCount > 0)
                        {
                            u.members = new List<BufferMember>();
                            compiler.GetDeclaredStructSize(type, out int size);
                            //Console.WriteLine($"  struct, size:{size}");
                            u.size = (uint)size;
                            for (int i = 0; i < type.MemberCount; i++)
                            {
                                var memberName = compiler.GetMemberName(rs.BaseTypeId, (uint)i);

                                compiler.GetStructMemberOffset(type, i, out int offset);
                                compiler.GetDeclaredStructMemberSize(type, i, out int memberSize);
                                //Console.WriteLine($"  Member Name:{memberName}, Offset:{offset}, size：{memberSize}");

//                                 compiler.GetStructMemberArrayStride(type, i, out int sz);
//                                 compiler.GetStructMemberMatrixStride(type, i, out int stride);
//                                 Console.WriteLine($"  ArrayStride:{sz}, MatrixStride:{stride}");

                                u.members.Add(new BufferMember { name = memberName, offset = offset, size = memberSize });
                               
                            }
                        }

                        refl.descriptorSets.Add(u);
                    }
                };

                CollectShaderResources(SharpSPIRVCross.ResourceType.UniformBuffer);
                CollectShaderResources(SharpSPIRVCross.ResourceType.StorageBuffer);

                foreach (var input in resources.GetResources(SharpSPIRVCross.ResourceType.StageInput))
                {
                    //Console.WriteLine($"ID: {input.Id}, BaseTypeID: {input.BaseTypeId}, TypeID: {input.TypeId}, Name: {input.Name})");
                    var location = compiler.GetDecoration(input.Id, SpvDecoration.Location);
                    //Console.WriteLine($"  Location: {location}");
                }

                CollectShaderResources(SharpSPIRVCross.ResourceType.StorageImage);
                CollectShaderResources(SharpSPIRVCross.ResourceType.SampledImage);
                CollectShaderResources(SharpSPIRVCross.ResourceType.PushConstant);


                //compiler.Options.SetOption(CompilerOption.GLSL_Version, 50);
                //var glsl_source = compiler.Compile();
            }

            refl.descriptorSets?.Sort((x, y) => { return x.set * 1000 + (int)x.binding - (y.set*1000 + (int)y.binding); });
            return refl;
         }

    }

}
