using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ShaderReader : ResourceReader<Shader>
    {
        protected override bool OnLoad(Shader resource, File stream)
        {
            return resource.Load(stream);
        }
    }
}
