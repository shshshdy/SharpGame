#define ENABLE_PROFILER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGame
{
    internal class ProfilerBlock
    {
        public string name;
        public ThreadedProfiler profiler;
        public ProfilerBlock parent;
        public List<ProfilerBlock> children;
        private Stopwatch stopwatch = new Stopwatch();
        long time;

        public ProfilerBlock()
        {
        }

        public void Reset(string name, ThreadedProfiler profiler, ProfilerBlock parent)
        {
            this.name = name;
            this.profiler = profiler;
            this.parent = parent;
            time = 0;
        }

        public void Free()
        {
            profiler.blockPool.Free(this);
            stopwatch.Stop();

            foreach (var c in children)
            {
                c.Free();
            }
        }

        public void Begin()
        {
            stopwatch.Reset();
            stopwatch.Start();
        }

        public void End()
        {
            time = stopwatch.ElapsedTicks;

        }

        public ProfilerBlock GetChild(string name)
        {
            foreach (var c in children)
            {
                if(c.name == name)
                {
                    return c;
                }
            }

            ProfilerBlock child = profiler.blockPool.Request();
            child.Reset(name, profiler, this);            
            return child;
        }
    }

    internal class ThreadedProfiler
    {
        public string name;
        public ProfilerBlock root;
        public ProfilerBlock current;

        public FreeList<ProfilerBlock> blockPool = new FreeList<ProfilerBlock>();
        public ThreadedProfiler()
        {
            current = root = new ProfilerBlock();
            root.profiler = this;
        }

        public void BeginSample(string name)
        {
            current = current.GetChild(name);
            current.Begin();
        }

        public void EndSample()
        {
            current.End();
            if (current.parent != null)
                current = current.parent;
        }
    }


    public class Profiler : System<Profiler>
    {
        ConcurrentDictionary<int, ThreadedProfiler> profilers = new ConcurrentDictionary<int, ThreadedProfiler>();

        public Profiler()
        {

        }

        static Profiler self => Instance;

        static ThreadedProfiler ThreadedProfiler
        {
            get
            {
                var thread = System.Threading.Thread.CurrentThread;
                int threadID = thread.ManagedThreadId;
                if (!self.profilers.TryGetValue(threadID, out ThreadedProfiler threadedProfiler))
                {
                    threadedProfiler = new ThreadedProfiler
                    {
                        name = thread.Name
                    };
                    self.profilers.TryAdd(threadID, threadedProfiler);
                }

                return threadedProfiler;
            }
        }

        [Conditional("ENABLE_PROFILER")]
        public static void BeginSample(string name)
        {
            ThreadedProfiler.BeginSample(name);
        }

        [Conditional("ENABLE_PROFILER")]
        public static void EndSample()
        {
            ThreadedProfiler.EndSample();
        }
    }
}
