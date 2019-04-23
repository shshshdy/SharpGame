using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    struct BeginFrame
    {
        public int frameNum_;
        public float timeTotal_;
        public float timeDelta_;
    }

    struct Update
    {
        public float timeTotal_;
        public float timeDelta_;
    }

    struct PostUpdate
    {
        public float timeTotal_;
        public float timeDelta_;
    }

    struct EndFrame
    {
    }


}
