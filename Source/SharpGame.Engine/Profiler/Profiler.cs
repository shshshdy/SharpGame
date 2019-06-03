#define ENABLE_PROFILER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SharpGame
{
    public class ThreadedProfiler
    {
        public string name;
        public int threadID;
        private bool profiling = false;

        FastList<Cmd> commands = new FastList<Cmd>();
        Stack<int> cmdStack = new Stack<int>();
        public FastList<Block> blocks = new FastList<Block>();
        Dictionary<StringID, int> blockToID = new Dictionary<StringID, int>();

        FastList<Cmd> publicCmds = new FastList<Cmd>();

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

            publicCmds = Interlocked.Exchange(ref commands, publicCmds);

            commands.Clear();
        }
       
        public void BeginSample(string name)
        {
            int id = GetChild(name);
            cmdStack.Push(id);
            commands.Add(new Cmd { type = CmdType.BeginBlock, nodeId = id, time = Stopwatch.GetTimestamp() });
        }

        public void EndSample()
        {
            int id = cmdStack.Pop();

            commands.Add(new Cmd { type = CmdType.EndBlock, nodeId = id, time = Stopwatch.GetTimestamp() });
        }

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

        public ref Cmd this[int index] => ref publicCmds.At(index);
        public int NumCmds => publicCmds.Count;

        public struct Block
        {
            public StringID name;

            public Block(StringID name)
            {
                this.name = name;
            }

        }

        public enum CmdType
        {
            NewToken,
            Message,
            Counter,
            BeginBlock,
            EndBlock,
            OneOff,
            Max,

        }

        [StructLayout(LayoutKind.Explicit, Size = 4)]
        public struct Cmd
        {
            [FieldOffset(0)]
            public CmdType type;
            [FieldOffset(4)]
            public int nodeId;
            [FieldOffset(8)]
            public long time;
            [FieldOffset(8)]
            public long value;
        }

    }


    public class Profiler : System<Profiler>
    {
        ConcurrentDictionary<int, ThreadedProfiler> profilers = new ConcurrentDictionary<int, ThreadedProfiler>();
        
        static Profiler self => Instance;

        public static ConcurrentDictionary<int, ThreadedProfiler> Profilers => Instance.profilers;

        public static ThreadedProfiler ThreadedProfiler
        {
            get
            {
                var thread = System.Threading.Thread.CurrentThread;
                int threadID = thread.ManagedThreadId;
                if (!self.profilers.TryGetValue(threadID, out ThreadedProfiler threadedProfiler))
                {
                    threadedProfiler = new ThreadedProfiler
                    {
                        name = thread.Name,
                        threadID = threadID
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
