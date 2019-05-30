using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{

    public class SampleApplication : Application
    {
        Sample current;
        int selected = 0;
        string[] sampleNames;

        Profiler profiler;

        public SampleApplication() : base("../../../../../")
        {
        }

        protected override void Setup()
        {
            base.Setup();

            profiler = CreateSubsystem<Profiler>();

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
                SetSample(Activator.CreateInstance(allSamples[0].Item4) as Sample);

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

        float[] fps = new float[100];

        private void HandleGUI(GUIEvent e)
        {
            var io = ImGui.GetIO();
            corner = 1;
            if (corner != -1)
            {
                System.Numerics.Vector2 window_pos = new System.Numerics.Vector2(((corner & 1) != 0) ? io.DisplaySize.X - DISTANCE
                    : DISTANCE, (corner & 2) != 0 ? io.DisplaySize.Y - DISTANCE : DISTANCE);
                System.Numerics.Vector2 window_pos_pivot = new System.Numerics.Vector2((corner & 1) != 0 ? 1.0f : 0.0f, (corner & 2) != 0 ? 1.0f : 0.0f);
                ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
            }

            ImGui.SetNextWindowBgAlpha(0.5f); // Transparent background
            if (ImGui.Begin("Perf HUD", ref hudOpen, (corner != -1 ? ImGuiWindowFlags.NoMove : 0) | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
            {
                ImGui.Text("Selected Sample:");
                if (ImGui.Combo("", ref selected, sampleNames, sampleNames.Length))
                {
                    SetSample(sampleNames[selected]);
                }

                ImGui.Separator();

                ImGui.Text(string.Format("Fps : {0}", Fps));
                ImGui.Text(string.Format("Msec : {0}", Msec));
                ImGui.Text(string.Format("ImageCount : {0}", graphics.ImageCount));
                ImGui.Text(string.Format("ImageIndex : {0}", graphics.currentImage));

                ImGui.Text(string.Format("FrameBegin : {0}",this.Stats.FrameBegin));
                ImGui.Text(string.Format("FrameEnd : {0}", this.Stats.FrameEnd));
                ImGui.Text(string.Format("RenderBegin : {0}", this.Stats.RenderBegin));
                ImGui.Text(string.Format("RenderEnd : {0}", this.Stats.RenderEnd));
                ImGui.Text(string.Format("LogicWait : {0}", this.Stats.LogicWait));
                ImGui.Text(string.Format("RenderWait : {0}", this.Stats.RenderWait));

                if(ImGui.Checkbox("Show stats", ref showStats))
                {

                }
            }

            ImGui.End();

            if(showStats)
            {
                corner = 0;
                System.Numerics.Vector2 window_pos = new System.Numerics.Vector2(io.DisplaySize.X / 2, io.DisplaySize.Y / 2);
                System.Numerics.Vector2 window_pos_pivot = new System.Numerics.Vector2(0.5f, 0.5f);
                ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
               ImGui.SetNextWindowBgAlpha(0.5f); // Transparent background
                if (ImGui.Begin("Stats", ref showStats, (corner != -1 ? ImGuiWindowFlags.NoMove : 0) | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
                {

                    fps[(Time.FrameNum / 10) % fps.Length] = Fps;
                    ImGui.PlotLines("fps:", ref fps[0], fps.Length);
                }

                ImGui.End();

            }
 

            if (current)
            {
                current.OnGUI();
            }


        }

        void SetSample(string typeName)
        {
            var type = Type.GetType("SharpGame.Samples." + typeName);
            if(type == null)
            {
                type = Type.GetType("SharpGame." + typeName);
            }

            if (type == null)
            {
                type = Type.GetType(typeName);
            }

            if (type == null)
            {
                return;
            }

            var sample = Activator.CreateInstance(type); 
            SetSample(sample as Sample);
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
                    current.Init();
                }
            }

        }

        public static void Main() => new SampleApplication().Run();

    }

}
