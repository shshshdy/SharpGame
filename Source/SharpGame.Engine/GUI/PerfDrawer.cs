using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public static class PerfDrawer
    {
        public static void Draw(this PerfTree tree)
        {
            ImGuiNET.ImGui.SameLine(400);
            ImGuiNET.ImGui.Text("Count"); ImGuiNET.ImGui.SameLine(500);
            ImGuiNET.ImGui.Text("Time(ms)"); ImGuiNET.ImGui.SameLine(600);
            ImGuiNET.ImGui.Text("Percent"); ImGuiNET.ImGui.SameLine(800);
            ImGuiNET.ImGui.Text("Total Percent");

            ImGuiNET.ImGui.Separator();
            tree.timer += Time.Delta;

            bool sample = false;
            if (tree.timer >= PerfTree.sampleTime)
            {
                sample = true;
                tree.timer = 0;
            }

            var it = Profiler.Profilers.GetEnumerator();
            while (it.MoveNext())
            {
                var profiler = it.Current.Value;
                tree.Build(profiler, sample);
            }

            tree.root.Draw(0);

            tree.root.Free();
        }

        public static void Draw(this PerfNode node, int depth)
        {
            bool collapse = ImGuiNET.ImGui.TreeNodeEx(node.label, ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.FramePadding);
            if (!node.IsRoot)
            {
                ImGuiNET.ImGui.SameLine(400);
                ImGuiNET.ImGui.Text(string.Format("{0:D}", node.count)); ImGuiNET.ImGui.SameLine(500);
                ImGuiNET.ImGui.Text(string.Format("{0:F4}", node.averageMS)); ImGuiNET.ImGui.SameLine(600);
                ImGuiNET.ImGui.Text(string.Format("{0:P1}%%", node.percent)); ImGuiNET.ImGui.SameLine(800);
                ImGuiNET.ImGui.Text(string.Format("{0:P1}%%", node.totalPercent));
            }

            if (depth < 3 && !collapse)
            {
                ImGuiNET.ImGui.TreePush();
            }

            if (collapse || depth < 3)
            {
                foreach (var c in node.children)
                {
                    c.Draw(depth + 1);
                }

                ImGuiNET.ImGui.TreePop();
            }


        }

    }
}
