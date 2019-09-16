using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class Environment : Component
    {
        public Color4 AmbientColor { get; set; } = new Color4(0.3f, 0.3f, 0.3f, 1.0f);
        public Color4 SunlightColor { get; set; } = new Color4(1.0f, 1.0f, 1.0f, 1.0f);

        vec3 sunlightDir;
        public vec3 SunlightDir
        {
            get => sunlightDir;
            set
            {
                sunlightDir = glm.normalize(value);
            }
        }

        public Texture skybox;
    }
}
