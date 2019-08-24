using System;
using System.Collections.Generic;
using System.Numerics;
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
        protected float wheelSpeed = 50.0f;
        protected float moveSpeed = 100.0f;
        protected Vector3 offset;

        public FileSystem FileSystem => FileSystem.Instance;
        public Resources Resources => Resources.Instance;
        public Graphics Graphics => Graphics.Instance;
        public Renderer Renderer => Renderer.Instance;

        public static bool debugImage = false;
        protected float debugImageHeight = 200.0f;
        List<Texture> debugImages = new List<Texture>();
       
        public Sample()
        {
            (this).Subscribe((GUIEvent e) => OnDebugImage());
        }

        public virtual void Init()
        {
        }

        public void DebugImage(bool enable, float height = 200.0f)
        {
            debugImage = enable;
            debugImageHeight = height;
        }

        public void AddDebugImage(params Texture[] textures)
        {
            foreach (var tex in textures)
            {
                debugImages.Add(tex);
            }
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
                Vector2 delta = (input.MousePosition - mousePos) * (Time.Delta * rotSpeed * new Vector2(camera.AspectRatio, 1.0f));

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
                offset.X = -delta.X * camera.AspectRatio;
                offset.Y = delta.Y;
            }

            camera.Node.Translate(offset * (Time.Delta * moveSpeed) + new Vector3(0, 0, input.WheelDelta * wheelSpeed), TransformSpace.LOCAL);

            mousePos = input.MousePosition;
            
        }

        bool openCamera = true;
        public virtual void OnGUI()
        {
            if(camera)
            {
                if (ImGui.Begin("HUD"))
                {
                    if(ImGui.CollapsingHeader("Camera", ref openCamera))
                    {
                        ImGui.PushItemWidth(120);
                        ImGui.TextUnformatted("pos : " + camera.Node.Position.ToString("0:0.00"));
                        ImGui.TextUnformatted("rot : " + camera.Node.Rotation.ToEuler().ToString("0:0.00"));
                        ImGui.SliderFloat("Rotate Speed: ", ref rotSpeed, 1, 100);
                        ImGui.SliderFloat("Move Speed: ", ref moveSpeed, 1, 1000);
                        ImGui.PopItemWidth();
                    }
                }

                ImGui.End();
             
            }
       
        }

        void OnDebugImage()
        {
            if(!debugImage || debugImages.Count == 0)
            {
                return;
            }

            var io = ImGui.GetIO();
            {
                Vector2 window_pos = new Vector2(10, io.DisplaySize.Y - 10);
                Vector2 window_pos_pivot = new Vector2(0.0f, 1.0f);
                ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
                ImGui.SetNextWindowBgAlpha(0.5f); // Transparent background
            }

            if (ImGui.Begin("DebugImage", ref debugImage, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
            {
                foreach(var tex in debugImages)
                {
                    float scale = tex.width / (float)tex.height;
                    if(scale > 1)
                    {
                        ImGUI.Image(tex, new Vector2(debugImageHeight, debugImageHeight/ scale)); ImGui.SameLine();
                    }
                    else
                    {
                        ImGUI.Image(tex, new Vector2(scale * debugImageHeight, debugImageHeight)); ImGui.SameLine();
                    }
                    
                }
            }

            ImGui.End();

        }

        protected override void Destroy()
        {
            debugImages.Clear();
            scene?.Dispose();

            base.Destroy();
        }
    }


}
