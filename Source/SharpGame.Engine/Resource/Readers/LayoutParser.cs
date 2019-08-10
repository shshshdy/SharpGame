using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    using static TokenType;

    public class Layout : Stmt
    {
        public LayoutType layoutType;
        public int set;
        public int binding;
        public int id;

        public string uniformType;
        public string uniformName;

        public class Attribute
        {
            public string name;
            public object value;
        }
    }

    public class LayoutParser : Parser
    {
        Scanner scanner;
        public LayoutParser(string path)
        {
            using (File file = FileSystem.Instance.GetFile(path))
            {
                string txt = file.ReadAllText();
                scanner = new Scanner(txt);
                this.Tokens = scanner.ScanTokens();
            }
        }

        public LayoutParser(List<Token> tokens) : base(tokens)
        {
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
                        var t = Consume(IDENTIFIER, "");
                   
                        switch(t.Lexeme)
                        {
                            case "bool":
                            case "int":
                            case "float":
                            case "vec2":
                            case "vec3":
                            case "vec4":
                            case "mat4":
                            case "sampler1D":
                            case "sampler2D":
                            case "sampler3D":
                            case "samplerCube":

                                layout.uniformType = t.Lexeme;
                                var uniformName = Advance();
                                layout.uniformName = uniformName.Lexeme;
                                Synchronize();
                                break;

                            default:
                                //todo: parse struct layout
                                layout.uniformType = "Struct";
                                layout.uniformName = t.Lexeme;
                                if(Check(TokenType.LEFT_BRACE))
                                {
                                    do
                                    {
                                        Advance();
                                    }
                                    while (!Check(RIGHT_BRACE) && !IsAtEnd());

                                    Advance();

                                    if(Check(IDENTIFIER))
                                    {
                                        var structName = Advance();
                                        layout.uniformName = structName.Lexeme;
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
                            case "float":
                            case "vec2":
                            case "vec3":
                            case "vec4":
                            case "mat4":

                                layout.uniformType = t.Lexeme;
                                var n = Advance();
                                layout.uniformName = n.Lexeme;
                                //TODO: parse default value
                                Synchronize();
                                break;

                            default:
                                
                                break;
                        }

                    }
                    else
                    {
                        //todo: parse in out

                        return null;
                    }


                    layout.layoutType = LayoutType.ResourceSet;

                    var attri = attributes.Find(a => a.name == "constant_id");
                    if(attri != null)
                    {
                        layout.layoutType = LayoutType.SpecializationConst;
                        layout.id = (int)(double)attri.value;
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
