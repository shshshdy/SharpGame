using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static SharpGame.ThreadedProfiler;

namespace SharpGame
{
    public class PerfTree : Object
    {
        Dictionary<string, PerfNode> m_pathItemMap = new Dictionary<string, PerfNode>();
        public PerfNode root = new PerfNode();
        public FreeList<PerfNode> blockPool = new FreeList<PerfNode>();

        public const float sampleTime = 2;
        public float timer = sampleTime;

        public PerfTree()
        {
            root.Reset("Root", this, null);
        }

        public PerfNode GetNode(ThreadedProfiler profiler, PerfNode parent, int id)
        {
            ref Block block = ref profiler.blocks.At(id);
            var node = blockPool.Request();
            node.Reset(block.name, this, parent);
            return node;
        }


        public void Build(ThreadedProfiler profiler, bool sample)
        {
            StringBuilder path = new StringBuilder();
            path.Append(profiler.threadID);

            int idx = 0;

            var iter = m_pathItemMap.GetEnumerator();
            while (iter.MoveNext())
            {
                var n = iter.Current.Value;

                n.averageSum -= n.averageBuffer[n.currentAverageSlot % PerfNode.NumAvgSlots];
                n.averageBuffer[n.currentAverageSlot % PerfNode.NumAvgSlots] = n.time;
                n.averageSum += n.time;
                n.average = n.averageSum / Math.Min(n.currentAverageSlot + 1, PerfNode.NumAvgSlots);

                if (sample)
                {
                    n.averageMS = n.average * Timer.MilliSecsPerTick;

                    if(n.parent.IsRoot || n.IsRoot)
                    {
                        n.percent = 1.0f;
                        n.totalPercent = 1.0f;
                    }
                    else
                    {
                        n.percent = (float)(n.averageMS / n.parent.averageMS);

                        var r = n;
                        while(!r.parent.IsRoot)
                        {
                            r = r.parent;
                        }

                        n.totalPercent = (float)(n.averageMS / r.averageMS);
                    }

                }

                n.currentAverageSlot++;
                n.count = 0L;
                n.time = 0;
            }

            BuildItemTree(profiler, ref idx, path, root);
        }

        void BuildItemTree(ThreadedProfiler profiler, ref int idx, StringBuilder path, PerfNode parent)
        {
            if(idx >= profiler.NumCmds)
            {
                return;
            }


            double startTime = profiler[idx].time;

            for ( ; idx < profiler.NumCmds; idx++)
            {
                ref Cmd cmd = ref profiler[idx];

                switch (cmd.type)
                {
                    case CmdType.Message:
                        break;

                    case CmdType.Counter:
                        {
                            char c = (char)cmd.nodeId;
                            path.Append(c);
                            string strPath = path.ToString();
                            if (!m_pathItemMap.TryGetValue(strPath, out PerfNode item))
                            {
                                item = GetNode(profiler, parent, cmd.nodeId);
                                m_pathItemMap[strPath] = item;
                            }

                            item.count += cmd.value;
                            path.Remove(path.Length - 1, 1);

                            if (!parent.HasChild(item))
                            {
                                parent.children.Add(item);
                            }
                        }
                        break;
                    case CmdType.BeginBlock:
                        {
                            PerfNode item = HandleBlock(profiler, ref idx, parent, path);
                            if (!parent.HasChild(item))
                            {
                                parent.children.Add(item);
                            }
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

        PerfNode HandleBlock(ThreadedProfiler profiler, ref int idx, PerfNode parent, StringBuilder path)
        {
            ref Cmd cmd = ref profiler[idx];
            long startTime = cmd.time;
            Debug.Assert(cmd.type == CmdType.BeginBlock);
            path.Append((char)cmd.nodeId);

            string strPath = path.ToString();
            if (!m_pathItemMap.TryGetValue(strPath, out PerfNode result))
            {
                result = GetNode(profiler, parent, cmd.nodeId);
                m_pathItemMap[strPath] = result;
            }

            result.count += 1;
            
            ++idx;

            BuildItemTree(profiler, ref idx, path, result);

            path.Remove(path.Length - 1, 1);
            
            result.time += profiler[idx].time - startTime;

            Debug.Assert(profiler[idx].type == CmdType.EndBlock);
     
            return result;

        }


    }

}
