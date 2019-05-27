using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ShaderReader : ResourceReader<Shader>
    {
        public ShaderReader(): base("")
        {
        }

        protected override bool OnLoad(Shader resource, File stream)
        {
            return resource.Load(stream);
        }
    }
}
