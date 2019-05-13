using System;
using System.Collections.Generic;
using ImGuiNET;
using SharpGame;
using VulkanCore;

namespace SharpGame.Samples
{
    public class SampleDescAttribute : System.Attribute
    {
        public string name;
        public string desc;
        public int sortOrder;
    }

    public class Sample : Object
    {
        public Graphics Graphics => Get<Graphics>();
        public Renderer Renderer => Get<Renderer>();
        public ResourceCache ResourceCache => Get<ResourceCache>();

        protected Scene scene_;
        protected Camera camera_;

        Vector2 mousePos_ = Vector2.Zero;
        float yaw_;
        float pitch_;
        float rotSpeed_ = 0.5f;
        float wheelSpeed_ = 150.0f;
        float moveSpeed_ = 15.0f;
        Vector3 offset_;


        public Sample()
        {
        }

        public virtual void Init()
        {
        }

        public virtual void Update()
        {
            if(camera_ == null)
            {
                return;
            }

            var input = Get<Input>();

            
            if (mousePos_ == Vector2.Zero)
                mousePos_ = input.MousePosition;

            offset_ = Vector3.Zero;
            if (input.IsMouseDown(MouseButton.Right))
            {
                Vector2 delta = (input.MousePosition - mousePos_) * Time.Delta * rotSpeed_ * camera_.AspectRatio;

                yaw_ += delta.X;
                pitch_ += delta.Y;

                camera_.Node.Rotation = Quaternion.RotationYawPitchRoll(yaw_, pitch_, 0);

                if (input.IsKeyPressed(Key.W))
                {
                    offset_.Z += 1.0f;
                }

                if (input.IsKeyPressed(Key.S))
                {
                    offset_.Z -= 1.0f;
                }

                if (input.IsKeyPressed(Key.A))
                {
                    offset_.X -= 1.0f;
                }

                if (input.IsKeyPressed(Key.D))
                {
                    offset_.X += 1.0f;
                }
            }

            if (input.IsMouseDown(MouseButton.Middle))
            {
                Vector2 delta = input.MousePosition - mousePos_;
                offset_.X = -delta.X;
                offset_.Y = delta.Y;
            }

            camera_.Node.Translate(offset_ * Time.Delta * moveSpeed_ + new Vector3(0, 0, input.WheelDelta * Time.Delta * wheelSpeed_), TransformSpace.LOCAL);

            mousePos_ = input.MousePosition;

        }

        public virtual void OnGUI()
        {
        }

        public virtual void Shutdown()
        {
            scene_?.Dispose();
        }
    }

    public class SampleApplication : Application
    {
        Sample current;
        int selected = 0;
        string[] sampleNames;

        protected override void Setup()
        {
            timer_ = CreateSubsystem<Timer>();
            fileSystem_ = CreateSubsystem<FileSystem>(gameWindow_);
            graphics_ = CreateSubsystem<Graphics>(gameWindow_);
            resourceCache_ = CreateSubsystem<ResourceCache>("../Content");
            renderer_ = CreateSubsystem<Renderer>();
            input_ = CreateSubsystem<Input>();

            CreateSubsystem<ImGUI>();

            this.SubscribeToEvent<BeginFrame>(HandleGUI);
            this.SubscribeToEvent<Update>(HandleUpdate);

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

        protected override void Shutdown()
        {
            if (current)
            {
                current.Shutdown();
            }

        }

        private void HandleUpdate(ref Update e)
        {
            if (current)
            {
                current.Update();
            }

        }

        private void HandleGUI(ref BeginFrame e)
        {
            return;
            var graphics = Get<Graphics>();


            if (ImGui.Begin("Sample", ImGuiWindowFlags.NoMove| ImGuiWindowFlags.NoResize))
            {
                ImGui.Text("FPS:");

                if(ImGui.Combo("Sample", ref selected, sampleNames, sampleNames.Length))
                {
                    SetSample(Activator.CreateInstance(allSamples[selected].Item4) as Sample);
                }
                

            }

            ImGui.End();

            ImGui.ShowDemoWindow();

            if (current)
            {
                current.OnGUI();
            }

        }


        void SetSample(Sample sample)
        {
            if (current != sample)
            {
                if (current)
                {
                    current.Shutdown();
                }

                current = sample;

                if (current)
                {
                    current.Init();
                }
            }

        }

    }

}