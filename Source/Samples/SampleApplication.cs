﻿using ImGuiNET;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{

    public class SampleApplication : Application
    {
        Sample current;
        int selected = 0;
        string[] sampleNames;

        PerfTree perfTree;

        public SampleApplication() : base("../../../../../")
        {
        }

        protected override void Setup()
        {
            base.Setup();

            CreateSubsystem<Profiler>();
            perfTree = CreateSubsystem<PerfTree>();

            this.Subscribe<GUIEvent>(HandleGUI);
            this.Subscribe<Update>(HandleUpdate);

        }


        public List<(string, string, int, Type)> allSamples = new List<(string, string, int, Type)>();


        protected override void Init()
        {
            base.Init();

            var types = System.Reflection.Assembly.GetExecutingAssembly().GetTypes();

            foreach (var t in types)
            {
                if (t.IsSubclassOf(typeof(Sample)))
                {
                    var attr = t.GetCustomAttributes(typeof(SampleDescAttribute), true);
                    if (attr.Length == 0)
                    {
                        allSamples.Add((t.Name, "", 0, t));
                    }
                    else
                    {
                        var a = (SampleDescAttribute)attr[0];
                        allSamples.Add((string.IsNullOrEmpty(a.name) ? t.Name : a.name, a.desc, a.sortOrder, t));
                    }
                }
            }

            allSamples.Sort((v1, v2) => v1.Item3 - v2.Item3);
           
            sampleNames = new string[allSamples.Count];
            for (int i = 0; i < allSamples.Count; i++)
            {
                sampleNames[i] = allSamples[i].Item1;
            }

            if (allSamples.Count > 0)
            {
                SetSample(allSamples[0].Item4);
            }
        }

        private void HandleUpdate(in Update e)
        {
            if (current)
            {
                current.Update();
            }

        }

        protected override void Destroy(bool disposing)
        {
            if (current)
            {
                current.Dispose();
            }

            base.Destroy(disposing);
        }


        static int corner = 1;

        bool hudOpen;
        bool showStats = false;
        const float DISTANCE = 10.0f;
        
        private void HandleGUI(in GUIEvent e)
        {
            var io = ImGui.GetIO();
            corner = 1;
            if (corner != -1)
            {
                vec2 window_pos = new vec2(((corner & 1) != 0) ? io.DisplaySize.X - DISTANCE
                    : DISTANCE, (corner & 2) != 0 ? io.DisplaySize.Y - DISTANCE : DISTANCE);
                vec2 window_pos_pivot = new vec2((corner & 1) != 0 ? 1.0f : 0.0f, (corner & 2) != 0 ? 1.0f : 0.0f);
                ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
            }

            ImGui.SetNextWindowBgAlpha(0.5f);

            if (ImGui.Begin("HUD", ref hudOpen, (corner != -1 ? ImGuiWindowFlags.NoMove : 0) | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
            {
                ImGui.Text("Selected Sample:");
                if (ImGui.Combo("", ref selected, sampleNames, sampleNames.Length))
                {
                    SetSample(allSamples[selected].Item4);
                }

                ImGui.Separator();
            }

            ImGui.End();

            {
                vec2 window_pos = new vec2(DISTANCE, DISTANCE);
                vec2 window_pos_pivot = new vec2(0.0f, 0.0f);
                ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
                ImGui.SetNextWindowBgAlpha(0.5f); // Transparent background
            }

            if (ImGui.Begin("Settings", ref hudOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
            {
                ImGui.Value("Single Loop", singleLoop);
                ImGui.Value("Fps", Fps);
                ImGui.Value("Msec", Msec); 

                ImGui.Value("Draw Call", Stats.drawCall);
                ImGui.Value("Triangle Count", (uint)Stats.triCount);
                ImGui.Value("Draw Indirect", Stats.drawIndirect);
                ImGui.Value("Indirect Triangle Count", (uint)Stats.indirectTriCount);

                ImGui.Text(string.Format("Logic Wait : {0:F3}", Stats.logicWait * Timer.MilliSecsPerTick));
                ImGui.Text(string.Format("Render Wait : {0:F3}", Stats.renderWait * Timer.MilliSecsPerTick));

                ImGui.Checkbox("Multi-Threaded Work", ref SceneSubpass.MultiThreaded);
                ImGui.Checkbox("Show Stats", ref showStats);
                ImGui.Checkbox("DebugRenderer", ref FrameGraph.drawDebug);
                ImGui.Checkbox("Debug Scene", ref FrameGraph.debugOctree);
                ImGui.Checkbox("Show DebugImage", ref FrameGraph.debugImage);
            }

            ImGui.End();

            if (showStats)
            {
                corner = 0;
                vec2 window_pos = new vec2(io.DisplaySize.X / 2, io.DisplaySize.Y);
                vec2 window_pos_pivot = new vec2(0.5f, 1.0f);
                ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
                ImGui.SetNextWindowSize(io.DisplaySize * 0.6f);
                ImGui.SetNextWindowBgAlpha(0.5f);

                if (ImGui.Begin("Stats", ref showStats, ImGuiWindowFlags.None))
                {
                    perfTree.Draw();
                }

                ImGui.End();

            }


            if (current)
            {
                current.OnGUI();
            }
        }

        void SetSample(Type type)
        {
            var sample = Activator.CreateInstance(type) as Sample;
            if(sample)
            {
                sample.Name = type.Name;
                SetSample(sample);
            }
        }

        void SetSample(Sample sample)
        {
            if (current != sample)
            {
                if (current)
                {
                    current.Dispose();
                }

                current = sample;

                if (current)
                {
                    Title = current.Name;
                    current.Init();
                }
            }

        }

        public static void Main() => new SampleApplication().Run();

    }

}
