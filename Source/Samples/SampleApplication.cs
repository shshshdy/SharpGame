using ImGuiNET;
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

        protected override void Destroy()
        {
            if (current)
            {
                current.Dispose();
            }

        }

        private void HandleUpdate(Update e)
        {
            if (current)
            {
                current.Update();
            }

        }


        static int corner = 1;

        bool hudOpen;
        bool showStats = false;
        const float DISTANCE = 10.0f;
        
        private void HandleGUI(GUIEvent e)
        {
            var io = ImGuiNET.ImGui.GetIO();
            corner = 1;
            if (corner != -1)
            {
                Vector2 window_pos = new Vector2(((corner & 1) != 0) ? io.DisplaySize.X - DISTANCE
                    : DISTANCE, (corner & 2) != 0 ? io.DisplaySize.Y - DISTANCE : DISTANCE);
                Vector2 window_pos_pivot = new Vector2((corner & 1) != 0 ? 1.0f : 0.0f, (corner & 2) != 0 ? 1.0f : 0.0f);
                ImGuiNET.ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
            }

            ImGuiNET.ImGui.SetNextWindowBgAlpha(0.5f);

            if (ImGuiNET.ImGui.Begin("Perf HUD", ref hudOpen, (corner != -1 ? ImGuiWindowFlags.NoMove : 0) | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
            {
                ImGuiNET.ImGui.Text("Selected Sample:");
                if (ImGuiNET.ImGui.Combo("", ref selected, sampleNames, sampleNames.Length))
                {
                    SetSample(allSamples[selected].Item4);
                }

                ImGuiNET.ImGui.Separator();

                if (current)
                {
                    current.OnGUI();
                }

            }

            ImGuiNET.ImGui.End();

            {
                Vector2 window_pos = new Vector2(DISTANCE, DISTANCE);
                Vector2 window_pos_pivot = new Vector2(0.0f, 0.0f);
                ImGuiNET.ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
                ImGuiNET.ImGui.SetNextWindowBgAlpha(0.5f); // Transparent background
            }

            if (ImGuiNET.ImGui.Begin("Settings", ref hudOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
            {
                ImGuiNET.ImGui.Value("Single Loop", singleLoop);
                ImGuiNET.ImGui.Value("Fps", Fps);
                ImGuiNET.ImGui.Value("Msec", Msec);
                ImGuiNET.ImGui.Value("Draw Call", Stats.drawCall);
                ImGuiNET.ImGui.Value("Triangle Count", Stats.triCount);
                //ImGui.Text(string.Format("ImageCount : {0}", graphics.ImageCount));
                //ImGui.Text(string.Format("ImageIndex : {0}", graphics.currentImage));

                ImGuiNET.ImGui.Text(string.Format("Logic Wait : {0:F3}", Stats.LogicWait * Timer.MilliSecsPerTick));
                ImGuiNET.ImGui.Text(string.Format("Render Wait : {0:F3}", Stats.RenderWait * Timer.MilliSecsPerTick));

                ImGuiNET.ImGui.Checkbox("Multi-Threaded Work", ref ScenePass.MultiThreaded);
                ImGuiNET.ImGui.Checkbox("Show Stats", ref showStats);
            }

            ImGuiNET.ImGui.End();

            if (showStats)
            {
                corner = 0;
                Vector2 window_pos = new Vector2(io.DisplaySize.X / 2, io.DisplaySize.Y);
                Vector2 window_pos_pivot = new Vector2(0.5f, 1.0f);
                ImGuiNET.ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
                ImGuiNET.ImGui.SetNextWindowSize(io.DisplaySize * 0.6f);
                ImGuiNET.ImGui.SetNextWindowBgAlpha(0.5f);

                if (ImGuiNET.ImGui.Begin("Stats", ref showStats, ImGuiWindowFlags.None))
                {
                    perfTree.Draw();
                }

                ImGuiNET.ImGui.End();

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
