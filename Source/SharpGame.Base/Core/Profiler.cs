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

    }


    internal class ThreadedProfiler
    {
        public string name;        
        public FreeList<ProfilerNode> blockPool = new FreeList<ProfilerNode>();
        private bool profiling = false;

        public ThreadedProfiler()
        {
        }

        public void Begin()
        {
            if(profiling)
            {
                return;
            }

            profiling = true;
        }

        public void End()
        {
            profiling = false;
        }
       

        public void BeginSample(string name)
        {
            int id = GetChild(name);
            cmdStack.Push(id);
            commands.Add(new Cmd { type = CmdType.Begin, nodeId = id, time = Stopwatch.GetTimestamp() });

        }

        public void EndSample()
        {
            int id = cmdStack.Pop();

            commands.Add(new Cmd { type = CmdType.End, nodeId = id, time = Stopwatch.GetTimestamp() });
        }

        struct Block
        {
            public StringID name;

            public Block(StringID name)
            {
                this.name = name;
            }

        }

        public enum CmdType
        {
            Begin,
            End
        }

        public struct Cmd
        {
            public CmdType type;
            public int nodeId;
            public long time;
        }

        FastList<Cmd> commands = new FastList<Cmd>();
        Stack<int> cmdStack = new Stack<int>();
        FastList<Block> blocks = new FastList<Block>();
        Dictionary<StringID, int> blockToID = new Dictionary<StringID, int>();

        public int GetChild(StringID name)
        {
            if(blockToID.TryGetValue(name, out int id))
            {
                return id;
            }

            blocks.Add(new Block(name));
            int newID = blocks.Count - 1;
            blockToID[name] = newID;
            return newID;
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
