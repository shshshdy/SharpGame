﻿using System;
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
        public long time;
        public string label;
        public long count;
        public long[] averageBuffer = new long[NumAvgSlots];
        public long averageSum;
        public long average;
        public double averageMS;
        public float percent;
        public float totalPercent;

        public int currentAverageSlot;

        public bool IsRoot => parent == null;

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
