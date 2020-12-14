using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class SSAOSubpass : FullScreenSubpass
    {
        public SSAOSubpass() : base("post/ssao.frag")
        {
        }

        protected override void CreateResources()
        {
        }

        protected override void BindResources()
        {
        }
    }
}
