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
        public const int NumAvgSlots = 10;

        public ThreadedProfiler profiler;
        public ProfilerNode parent;

        public List<ProfilerNode> children;
        public double time;
        public string label;
        public ulong count;
        public double[] averageBuffer = new double[NumAvgSlots];
        public double averageSum;
        public double average;
        public int currentAverageSlot;

        public ProfilerNode()
        {
            children = new List<ProfilerNode>();
        }

        public void Reset(string label, ThreadedProfiler profiler, ProfilerNode parent)
        {
            this.label = label;
            this.profiler = profiler;
            this.parent = parent;
        }

        public bool HasChild(ProfilerNode node)
        {
            foreach(var c in children)
            {
                if(node == c)
                {
                    return true;
                }
            }

            return false;
        }

        public void Free()
        {
            profiler.blockPool.Free(this);            

            foreach (var c in children)
            {
                c.Free();
            }
        }

    }


    internal unsafe class ThreadedProfiler : ProfilerNode
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
            commands.Add(new Cmd { type = CmdType.BeginBlock, nodeId = id, time = Stopwatch.GetTimestamp() });

        }

        public void EndSample()
        {
            int id = cmdStack.Pop();

            commands.Add(new Cmd { type = CmdType.EndBlock, nodeId = id, time = Stopwatch.GetTimestamp() });
        }

        internal struct Block
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

        

        internal ProfilerNode GetNode(ProfilerNode parent, int id)
        {
            ref Block block = ref blocks.At(id);
            var node = blockPool.Request();
            node.Reset(block.name, this, parent);
            return node;
        }

        Dictionary<StringBuilder, ProfilerNode> m_pathItemMap = new Dictionary<StringBuilder, ProfilerNode>();
        ProfilerNode root = new ProfilerNode();
        void build()
        {
            StringBuilder path = new StringBuilder();

            ref Block it = ref blocks.At(0);

            var iter = m_pathItemMap.GetEnumerator();
            while(iter.MoveNext())
            {
                var n = iter.Current.Value;

                n.averageSum -= n.averageBuffer[n.currentAverageSlot % NumAvgSlots];
                // store new value to be part of average.
                n.averageBuffer[n.currentAverageSlot % NumAvgSlots] = n.time;
                // add new to sum to be averaged
                n.averageSum += n.time;
                n.average = n.averageSum / (double)Math.Min(n.currentAverageSlot + 1, NumAvgSlots);
                n.currentAverageSlot++;
                n.count = 0UL;
                n.time = 0.0;
            }

            buildItemTree(path, root);
        }

        void buildItemTree(StringBuilder path, ProfilerNode parent)
        {
            double startTime = 0;// (*it).m_timeStamp;

            for (int i = 0; i < commands.Count; i++)
            {
                ref Cmd cmd = ref commands.At(i);
                bool stop = false;

                switch (cmd.type)
                {
                    case CmdType.Message:
                        break;
                   
                        case CmdType.Counter:
                            {
                                path.Append(cmd.nodeId);
                            /*
                                if (!m_pathItemMap.TryGetValue(path, out ProfilerNode item))
                                {
                                    item = new ProfilerNode(profiler.getTokenString((*it).m_tokenId));
                                    m_pathItemMap[path] = item;
                                }
 /*
                                item->count += uint64_t((*it).m_value);
                                path.pop_back();

                                if (std::find(items.begin(), items.end(), item) == items.end())
                                {
                                    items.push_back(item);
                                }*/
                            }
                            break;
                        case CmdType.BeginBlock:
                            {
                                ProfilerNode item = handleBlock(parent, path, ref cmd);
                                if(!parent.HasChild(item))
                            {
                                parent.children.Add(item);
                            }
                            /*
                                if (std::find(items.begin(), items.end(), item) == items.end())
                                {
                                    items.push_back(item);
                                }*/
                        }
                            break;
                    case CmdType.EndBlock:
                        //return items;
                        return;
                    case CmdType.OneOff:
                        break;
                };
            }

        }
       
        ProfilerNode handleBlock(ProfilerNode parent, StringBuilder path, ref Cmd cmd)
        { 
            double startTime = cmd.time;
            Debug.Assert(cmd.type == CmdType.BeginBlock);           
            path.Append(cmd.nodeId);

            if(!m_pathItemMap.TryGetValue(path, out ProfilerNode result))
            {
                //result = new ProfilerNode(profiler.getTokenString((*it).m_tokenId));
                result = GetNode(parent, cmd.nodeId);
                m_pathItemMap[path] = result;
            }
         
            result.count += 1;
            /*
            ++it;

            result->children = buildItemTree(it, profiler, path);
            path.pop_back();
            result->time += (*it).m_timeStamp - startTime;
            ASSERT((*it).m_id == Profiler::CID_EndBlock);
*/
            //Debug.Assert(cmd.type == CmdType.BeginBlock);
            return result;
            
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
