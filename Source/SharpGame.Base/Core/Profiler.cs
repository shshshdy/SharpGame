#define ENABLE_PROFILER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGame
{
    internal class ProfilerNode
    {
        public string name;
        public ThreadedProfiler profiler;
        public ProfilerNode parent;
        public List<ProfilerNode> children;
        long beginTime;
        long endTime;

        public ProfilerNode()
        {
        }

        public void Reset(string name, ThreadedProfiler profiler, ProfilerNode parent)
        {
            this.name = name;
            this.profiler = profiler;
            this.parent = parent;
            beginTime = endTime = 0;
        }

        public void Free()
        {
            profiler.blockPool.Free(this);            

            foreach (var c in children)
            {
                c.Free();
            }
        }

        public void Begin()
        {
            beginTime = Stopwatch.GetTimestamp();
        }

        public void End()
        {
            endTime = Stopwatch.GetTimestamp();
        }

        public ProfilerNode GetChild(string name)
        {
            foreach (var c in children)
            {
                if(c.name == name)
                {
                    return c;
                }
            }

            ProfilerNode child = profiler.blockPool.Request();
            child.Reset(name, profiler, this);            
            return child;
        }
    }


    internal class ThreadedProfiler
    {
        public string name;
        public ProfilerNode root;
        public ProfilerNode current;        

        public FreeList<ProfilerNode> blockPool = new FreeList<ProfilerNode>();
        private bool profiling = false;
        public ThreadedProfiler()
        {
            root = new ProfilerNode
            {
                profiler = this
            };
        }

        public void Begin()
        {
            if(profiling)
            {
                return;
            }

            current = root;
            profiling = true;
        }

        public void End()
        {
            if(current != root)
            {
                Log.Warn("profiler error");
                return;
            }

            foreach (var c in root.children)
            {
                c.Free();
            }

            profiling = false;
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
        public static void Begin()
        {
            ThreadedProfiler.Begin();
        }

        [Conditional("ENABLE_PROFILER")]
        public static void End()
        {
            ThreadedProfiler.End();
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
