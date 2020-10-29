using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{

    public class RenderFrame
    {
        public int imageIndex;

        public Semaphore acquireSemaphore;

        public Semaphore preRenderSemaphore;
        public Semaphore computeSemaphore;
        public Semaphore renderSemaphore;

        public Fence presentFence;

        public RenderFrame()
        {
            acquireSemaphore = new Semaphore();
            preRenderSemaphore = new Semaphore();
            computeSemaphore = new Semaphore();
            renderSemaphore = new Semaphore();
        }
    }
}
