using System;
using System.Collections.Generic;
using ImGuiNET;
using SharpGame;

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
        public string Name { get; set; }

        protected Scene scene;
        protected Camera camera;
        protected Vector2 mousePos = Vector2.Zero;
        protected float yaw;
        protected float pitch;
        protected float rotSpeed = 0.5f;
        protected float wheelSpeed = 150.0f;
        protected float moveSpeed = 15.0f;
        protected Vector3 offset;

        public FileSystem FileSystem => FileSystem.Instance;
        public Resources Resources => Resources.Instance;
        public Graphics Graphics => Graphics.Instance;
        public Renderer Renderer => Renderer.Instance;

        public Sample()
        {
        }

        public virtual void Init()
        {
        }

        public virtual void Update()
        {
            if(camera == null)
            {
                return;
            }

            var input = Input.Instance;
            if(input.snapshot == null)
            {
                return;
            }

            if (mousePos == Vector2.Zero)
                mousePos = input.MousePosition;

            offset = Vector3.Zero;
            if (input.IsMouseDown(MouseButton.Right))
            {
                Vector2 delta = (input.MousePosition - mousePos) * (float)(Time.Delta * rotSpeed * camera.AspectRatio);

                if(pitch == 0)
                {
                    var rot = camera.Node.Rotation.ToEuler();
                    yaw = rot.Y;
                    pitch = rot.X;
                }

                yaw += delta.X;
                pitch += delta.Y;

                camera.Node.Rotation = Quaternion.RotationYawPitchRoll(yaw, pitch, 0);

                if (input.IsKeyPressed(Key.W))
                {
                    offset.Z += 1.0f;
                }

                if (input.IsKeyPressed(Key.S))
                {
                    offset.Z -= 1.0f;
                }

                if (input.IsKeyPressed(Key.A))
                {
                    offset.X -= 1.0f;
                }

                if (input.IsKeyPressed(Key.D))
                {
                    offset.X += 1.0f;
                }
            }

            if (input.IsMouseDown(MouseButton.Middle))
            {
                Vector2 delta = input.MousePosition - mousePos;
                offset.X = -delta.X;
                offset.Y = delta.Y;
            }

            camera.Node.Translate(offset * (Time.Delta * moveSpeed) + new Vector3(0, 0, input.WheelDelta * (Time.Delta * wheelSpeed)), TransformSpace.LOCAL);

            mousePos = input.MousePosition;
            
        }

        bool hudOpen;
        public virtual void OnGUI()
        {
            const float DISTANCE = 10.0f;
            var io = ImGui.GetIO();
            Vector2 window_pos = new Vector2(DISTANCE, DISTANCE);
            Vector2 window_pos_pivot = new Vector2(0.0f, 0.0f);
            ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
            ImGui.SetNextWindowBgAlpha(0.5f); // Transparent background
            if (ImGui.Begin("Camera", ref hudOpen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
            {
                ImGui.Value("yaw", MathUtil.RadiansToDegrees(yaw));
                ImGui.Value("pitch", MathUtil.RadiansToDegrees(pitch));
                //ImGui.Value("Msec", Msec);
            }
        }

        protected override void Destroy()
        {
            scene?.Dispose();

            base.Destroy();
        }
    }


}
