using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class SimpleSSRSubpass : FullScreenSubpass
    {
        public SimpleSSRSubpass() : base("post/ssr.frag")
        {
            this[0, 0] = "global";
        }

        protected override void CreateResources()
        {
        }

        protected override void OnBindResources()
        {
        }
    }

    public class SSR_ProjectionSubpass : FullScreenSubpass
    {
        public SSR_ProjectionSubpass() : base("post/ssr_proj.frag")
        {
            this[0, 0] = "global";
        }

        protected override void CreateResources()
        {
        }

        protected override void OnBindResources()
        {
        }
    }

    public class SSRRSubpass : FullScreenSubpass
    {
        public SSRRSubpass() : base("post/ssrr.frag")
        {
            this[0, 0] = "global";
        }

        protected override void CreateResources()
        {
        }

        protected override void OnBindResources()
        {
        }
    }
}
