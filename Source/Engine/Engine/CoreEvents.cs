using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct BeginFrame
    {
        public int frameNum_;
        public float timeTotal_;
        public float timeDelta_;
    }

    public struct Update
    {
        public float timeTotal_;
        public float timeDelta_;
    }

    public struct PostUpdate
    {
        public float timeTotal_;
        public float timeDelta_;
    }

    public struct PostRenderUpdate
    {

    }

    public struct EndFrame
    {
    }


}
