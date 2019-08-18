using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    using static TokenType;

    public enum LayoutType
    {
        ResourceSet,
        PushConstant,
        SpecializationConst
    }

    public class Layout : Stmt
    {
        public LayoutType layoutType;
        public string objectType;
        public string objectName;
        public DescriptorType descriptorType;
        public int set;
        public int binding;
        public uint id;
        public bool isDynamic = false;        
        public List<BlockMember> structMembers;
    
        public class Attribute
        {
            public string name;
            public object value;
        }
    }

    public class LayoutParser : Parser
    {
        Scanner scanner;
        public LayoutParser(File file)
        {
            string txt = file.ReadAllText();
            scanner = new Scanner(txt);
            this.Tokens = scanner.ScanTokens();

        }

        public LayoutParser(string source)
        {
            scanner = new Scanner(source);
            this.Tokens = scanner.ScanTokens();
        }

        public LayoutParser(List<Token> tokens) : base(tokens)
        {
        }

        public ShaderReflection Reflection()
        {
            var layouts = Parse();
            ShaderReflection shaderReflection = new ShaderReflection();
            foreach(var stmt in layouts)
            {
                Layout layout = stmt as Layout;
                switch(layout.layoutType)
                {
                    case LayoutType.ResourceSet:
                        if(shaderReflection.descriptorSets == null)
                        {
                            shaderReflection.descriptorSets = new List<UniformBlock>();
                        }

                        UniformBlock uniformBlock = new UniformBlock
                        {
                            name = layout.objectName,
                            set = layout.set,
                            binding = layout.binding,
                            descriptorType = layout.descriptorType,
                        };

                        shaderReflection.descriptorSets.Add(uniformBlock);

                        break;
                    case LayoutType.PushConstant:
                        if (shaderReflection.pushConstants == null)
                        {
                            shaderReflection.pushConstants = new List<BlockMember>();
                        }
                        break;
                    case LayoutType.SpecializationConst:
                        if (shaderReflection.specializationConsts == null)
                        {
                            shaderReflection.specializationConsts = new List<SpecializationConst>();
                        }

                        SpecializationConst specializationConst = new SpecializationConst
                        {
                            name = layout.objectName,
                            id = layout.id
                        };

                        shaderReflection.specializationConsts.Add(specializationConst);
                        break;
                    default:
                        break;
                }
            }

            return shaderReflection;
        }

        protected override Stmt Declaration()
        {
            try
            {
                if (Match(LAYOUT))
                {
                    Consume(LEFT_PAREN, "Expected '(' before layout attribute.");

                    var attributes = new List<Layout.Attribute>();
                    while (!Check(RIGHT_PAREN) && !IsAtEnd())
                    {
                        attributes.Add(ParseAttribute());
                    }

                    Consume(RIGHT_PAREN, "Expected ')' after layout attribute.");

                    Layout layout = new Layout();
                    if (Match(UNIFORM))
                    {
                        if (Match(RESTRICT))
                        {
                        }

                        if (Match(WRITEONLY))
                        {
                        }

                        if (Match(READONLY))
                        {
                        }

                        var t = Consume(IDENTIFIER, "");

                        switch (t.Lexeme)
                        {
                            case "bool":
                            case "int":
                            case "uint":
                            case "float":
                            case "vec2":
                            case "vec3":
                            case "vec4":
                            case "ivec2":
                            case "ivec3":
                            case "ivec4":
                            case "uvec2":
                            case "uvec3":
                            case "uvec4":
                            case "bvec2":
                            case "bvec3":
                            case "bvec4":
                            case "mat2":
                            case "mat3":
                            case "mat4":
                                {
                                    layout.objectType = t.Lexeme;
                                    var uniformName = Advance();
                                    layout.objectName = uniformName.Lexeme;
                                    Synchronize();
                                }
                                break;

                            case "texture1D":
                            case "texture2D":
                            case "texture3D":
                            case "textureCube":
                                {
                                    layout.descriptorType = DescriptorType.SampledImage;                                   
                                    layout.objectType = t.Lexeme;
                                    var samplerImgName = Advance();
                                    layout.objectName = samplerImgName.Lexeme;
                                    Synchronize();
                                }
                                break;
                            case "sampler":
                                {
                                    layout.descriptorType = DescriptorType.Sampler;                                    
                                    layout.objectType = t.Lexeme;
                                    var samplerName = Advance();
                                    layout.objectName = samplerName.Lexeme;
                                    Synchronize();
                                }
                                break;
                            case "sampler1D":
                            case "sampler2D":
                            case "sampler3D":
                            case "samplerCube":
                            case "sampler1DArray":
                            case "sampler2DArray":
                                {
                                    layout.descriptorType = DescriptorType.CombinedImageSampler;                                    
                                    layout.objectType = t.Lexeme;
                                    var combinedSamplerName = Advance();
                                    layout.objectName = combinedSamplerName.Lexeme;
                                    Synchronize();
                                }
                                break;
                            case "image2D":
                                {
                                    layout.descriptorType = DescriptorType.StorageImage;                                    
                                    layout.objectType = t.Lexeme;
                                    var objName = Advance();
                                    layout.objectName = objName.Lexeme;
                                    Synchronize();
                                }
                                break;
                            case "samplerBuffer":
                                {
                                    layout.descriptorType = DescriptorType.UniformTexelBuffer;                                    
                                    layout.objectType = t.Lexeme;
                                    var objName = Advance();
                                    layout.objectName = objName.Lexeme;
                                    Synchronize();
                                }
                                break;
                            case "imageBuffer":                           
                            case "uimageBuffer":
                                {
                                    layout.descriptorType = DescriptorType.StorageTexelBuffer;                                    
                                    layout.objectType = t.Lexeme;
                                    var objName = Advance();
                                    layout.objectName = objName.Lexeme;
                                    Synchronize();
                                }
                                break;

                            default:
                                layout.objectType = "UniformBuffer";
                                layout.objectName = t.Lexeme;
                                if (layout.objectName.EndsWith("_dynamic"))
                                {
                                    layout.isDynamic = true;
                                    layout.descriptorType = DescriptorType.UniformBufferDynamic;
                                }
                                else
                                {
                                    layout.descriptorType = DescriptorType.UniformBuffer;
                                }

                                if (Check(LEFT_BRACE))
                                {
                                    do
                                    {
                                        Advance();
                                        //todo: parse struct layout
                                    }
                                    while (!Check(RIGHT_BRACE) && !IsAtEnd());

                                    Advance();

                                    if (Check(IDENTIFIER))
                                    {
                                        var structName = Advance();
                                        layout.objectName = structName.Lexeme;
                                    }

                                    Synchronize();
                                }
                                break;
                        }
                    }
                    else if (Match(CONST))
                    {
                        var t = Consume(IDENTIFIER, "");

                        switch (t.Lexeme)
                        {
                            case "bool":
                            case "int":
                            case "uint":
                            case "float":
                            case "vec2":
                            case "vec3":
                            case "vec4":
                            case "ivec2":
                            case "ivec3":
                            case "ivec4":
                            case "uvec2":
                            case "uvec3":
                            case "uvec4":
                            case "bvec2":
                            case "bvec3":
                            case "bvec4":
                            case "mat2":
                            case "mat3":
                            case "mat4":

                                layout.objectType = t.Lexeme;
                                var n = Advance();
                                layout.objectName = n.Lexeme;
                                //layout.value = n.Literal;
                                //TODO: parse default value
                                Synchronize();
                                break;

                            default:

                                break;
                        }

                    }
                    else if (Match(IN))
                    {
                        return null;
                    }
                    else if (Match(OUT))
                    {
                        return null;
                    }
                    else if (Match(INOUT))
                    {
                        return null;
                    }
                    else
                    {
                        var t = Consume(IDENTIFIER, "");
                        switch (t.Lexeme)
                        {
                            case "buffer":
                                layout.objectType = "StorageBuffer";
                                layout.objectName = t.Lexeme;
                                if (layout.objectName.EndsWith("_dynamic"))
                                {
                                    layout.isDynamic = true;
                                    layout.descriptorType = DescriptorType.StorageBufferDynamic;
                                }
                                else
                                {
                                    layout.descriptorType = DescriptorType.StorageBuffer;
                                }

                                if (Check(LEFT_BRACE))
                                {
                                    do
                                    {
                                        Advance();
                                        //todo: parse struct layout
                                    }
                                    while (!Check(RIGHT_BRACE) && !IsAtEnd());

                                    Advance();

                                    if (Check(IDENTIFIER))
                                    {
                                        var structName = Advance();
                                        layout.objectName = structName.Lexeme;
                                    }

                                    Synchronize();
                                }

                                break;
                        }
                    }


                    layout.layoutType = LayoutType.ResourceSet;

                    var attri = attributes.Find(a => a.name == "constant_id");
                    if(attri != null)
                    {
                        layout.layoutType = LayoutType.SpecializationConst;
                        layout.id = (uint)(double)attri.value;
                    }

                    attri = attributes.Find(a => a.name == "push_constant");
                    if (attri != null)
                    {
                        layout.layoutType = LayoutType.PushConstant;
                    }

                    attri = attributes.Find(a => a.name == "set");
                    if (attri != null)
                    {
                        layout.set = (int)(double)attri.value;
                    }

                    attri = attributes.Find(a => a.name == "binding");
                    if (attri != null)
                    {
                        layout.binding = (int)(double)attri.value;
                    }

                    return layout;
                }
                Advance();
                return null;
            }
            catch (ParseError)
            {
                Synchronize();
                return null;
            }

        }

        Layout.Attribute ParseAttribute()
        {
            var key = Advance();
            var attr = new Layout.Attribute();
            attr.name = key.Lexeme;
            if (Match(EQUAL))
            {
                var val = Advance();
                attr.value = val.Literal;
            }

            if (Check(COMMA))
            {
                Advance();
            }

            return attr;

        }

    }
}
