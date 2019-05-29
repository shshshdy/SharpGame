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

        public List<ProfilerBlock> children;
    }

    internal class ThreadedProfiler
    {
        public string name;
        public ProfilerBlock root;
        public ProfilerBlock current;

        public void BeginSample(string name)
        {

        }

        public void EndSample()
        {

        }
    }


    public class Profiler
    {
        static ConcurrentDictionary<int, ThreadedProfiler> profiles = new ConcurrentDictionary<int, ThreadedProfiler>();

        static ThreadedProfiler ThreadedProfiler
        {
            get
            {
                var thread = System.Threading.Thread.CurrentThread;
                int threadID = thread.ManagedThreadId;
                if (!profiles.TryGetValue(threadID, out ThreadedProfiler threadedProfiler))
                {
                    threadedProfiler = new ThreadedProfiler
                    {
                        name = thread.Name
                    };
                    profiles.TryAdd(threadID, threadedProfiler);
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
