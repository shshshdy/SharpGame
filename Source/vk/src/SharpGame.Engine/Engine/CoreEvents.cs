using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct BeginFrame
    {
        public int frameNum;
        public float timeTotal;
        public float timeDelta;
    }

    public struct Update
    {
        public float timeTotal;
        public float timeDelta;
    }

    public struct PostUpdate
    {
        public float timeTotal;
        public float timeDelta;
    }

    public struct PostRenderUpdate
    {

    }

    public struct EndFrame
    {
    }


}
