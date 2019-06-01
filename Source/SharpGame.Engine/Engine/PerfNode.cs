using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class PerfNode
    {
        public const int NumAvgSlots = 10;

        public PerfTree profiler;
        public PerfNode parent;

        public List<PerfNode> children;
        public double time;
        public string label;
        public long count;
        public double[] averageBuffer = new double[NumAvgSlots];
        public double averageSum;
        public double average;
        public int currentAverageSlot;

        public PerfNode()
        {
            children = new List<PerfNode>();
        }

        public void Reset(string label, PerfTree profiler, PerfNode parent)
        {
            this.label = label;
            this.profiler = profiler;
            this.parent = parent;
        }

        public bool HasChild(PerfNode node)
        {
            foreach (var c in children)
            {
                if (node == c)
                {
                    return true;
                }
            }

            return false;
        }

        public void Draw()
        {
            ImGui.TreeNode(label);
            ImGui.TreePush();
            foreach (var c in children)
            {
                c.Draw();
            }
            ImGui.TreePop();
        }

        public void Free()
        {
            profiler.blockPool.Free(this);

            foreach (var c in children)
            {
                c.Free();
            }

            children.Clear();
        }

    }

}
