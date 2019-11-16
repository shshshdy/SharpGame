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
        protected vec2 mousePos = vec2.Zero;

        protected bool firstMode = true;
        protected float yaw;
        protected float pitch;
        protected float roll;
        protected float rotSpeed = 0.5f;
        protected float wheelSpeed = 10.0f;
        protected float moveSpeed = 10.0f;
        protected vec3 offset;

        public FileSystem FileSystem => FileSystem.Instance;
        public Resources Resources => Resources.Instance;
        public Graphics Graphics => Graphics.Instance;
        public FrameGraph FrameGraph => FrameGraph.Instance;
        public RenderView MainView => Application.Instance.MainView;

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

            if (mousePos == vec2.Zero)
                mousePos = input.MousePosition;

            offset = vec3.Zero;

            if (firstMode)
            {
                if (input.IsMouseDown(MouseButton.Right))
                {
                    vec2 delta = (input.MousePosition - mousePos) * (Time.Delta * rotSpeed * new vec2(camera.AspectRatio, 1.0f));

                    if (yaw == 0 )
                    {
                        var rot = camera.Node.Rotation.EulerAngles;
                        yaw = rot.Y;
                        pitch = rot.X;
                    }

                    yaw += delta.X;
                    pitch += delta.Y;
                    pitch = glm.clamp(pitch, -glm.half_pi, glm.half_pi);
                    camera.Node.Rotation = glm.quatYawPitchRoll(yaw, pitch, 0);

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
                    vec2 delta = input.MousePosition - mousePos;
                    offset.X = -delta.X * camera.AspectRatio;
                    offset.Y = delta.Y;
                }

                camera.Node.Translate(offset * (Time.Delta * moveSpeed) + new vec3(0, 0, input.WheelDelta * wheelSpeed), TransformSpace.LOCAL);
            }
            else
            {
                if (input.IsMouseDown(MouseButton.Left))
                {
                    vec2 delta = (input.MousePosition - mousePos) * (Time.Delta * rotSpeed * 2 * new vec2(camera.AspectRatio, 1.0f));

                    camera.Node.RotateAround(vec3.Zero, glm.quatYawPitchRoll(delta.x, delta.y, 0), TransformSpace.WORLD);

                }

                camera.Node.Translate(new vec3(0, 0, input.WheelDelta * wheelSpeed), TransformSpace.LOCAL);
            }


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
                        ImGui.TextUnformatted("pos : " + camera.Node.Position.ToString("0.00"));
                        ImGui.TextUnformatted("rot : " + camera.Node.Rotation.EulerAngles.ToString("0.00"));
                        ImGui.SliderFloat("Rotate Speed: ", ref rotSpeed, 1, 100);
                        ImGui.SliderFloat("Move Speed: ", ref moveSpeed, 1, 1000);
                        ImGui.PopItemWidth();
                    }
                }

                ImGui.End();
             
            }

            Environment env = scene?.GetComponent<Environment>();
            if(env)
            {
                if (ImGui.Begin("HUD"))
                {
                    env.AmbientColor = ImGUI.Color4("Ambient Color", env.AmbientColor);
                    env.SunlightColor = ImGUI.Color4("Sunlight Color", env.SunlightColor);

                    Vector3 dir = (Vector3)env.SunlightDir;
                    ImGui.SliderFloat3("Light dir", ref dir, -1.0f, 1.0f);
                    env.SunlightDir = (vec3)dir;
                }

                ImGui.End();
            }
       
        }

        protected override void Destroy(bool disposing)
        {
            scene?.Dispose();

            base.Destroy(disposing);
        }
    }


}
