using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class EarlyZPass : ScenePass
    {
        Shader depthShader;
        public EarlyZPass() : base(Pass.EarlyZ)
        {
            depthShader = Resources.Instance.Load<Shader>("shaders/shadow.shader");
        }
    }
}
