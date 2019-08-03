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

            return pass;
        }
    }

}
