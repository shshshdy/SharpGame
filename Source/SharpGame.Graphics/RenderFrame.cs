using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{

    public class BackBuffer
    {
        public int context;
        public int imageIndex;
        public Semaphore acquireSemaphore;
        public Semaphore preRenderSemaphore;
        public Semaphore computeSemaphore;
        public Semaphore renderSemaphore;
        public Fence presentFence;
    }
}
