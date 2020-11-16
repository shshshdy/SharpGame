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
        protected float rotSpeed = 0.5f;
        protected float wheelSpeed = 1000.0f;
        protected float moveSpeed = 100.0f;
     
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

            vec2 delta_rotation = new(0);
            vec3 delta_translation = new(0.0f, 0.0f, 0.0f);
            if (firstMode)
            {
                vec2 delta = input.MousePosition - mousePos;
                if (input.IsMouseDown(MouseButton.Right))
                {
                    delta_rotation = delta * (Time.Delta * rotSpeed * new vec2(camera.AspectRatio, 1.0f));

                    if (input.IsKeyPressed(Key.W))
                    {
                        delta_translation.Z += 1.0f;
                    }

                    if (input.IsKeyPressed(Key.S))
                    {
                        delta_translation.Z -= 1.0f;
                    }

                    if (input.IsKeyPressed(Key.A))
                    {
                        delta_translation.X -= 1.0f;
                    }

                    if (input.IsKeyPressed(Key.D))
                    {
                        delta_translation.X += 1.0f;
                    }
                }


                if (input.IsMouseDown(MouseButton.Middle))
                {
                    delta_translation.X = -delta.X * camera.AspectRatio;
                    delta_translation.Y = delta.Y;
                }

                delta_translation *= (Time.Delta * moveSpeed);
                delta_translation.z += input.WheelDelta * wheelSpeed * Time.Delta;

                if (delta_rotation != glm.vec2(0.0f, 0.0f) || delta_translation != glm.vec3(0.0f, 0.0f, 0.0f))
                {
                    quat qx = glm.angleAxis(delta_rotation.y, glm.vec3(1.0f, 0.0f, 0.0f));
                    quat qy = glm.angleAxis(delta_rotation.x, glm.vec3(0.0f, 1.0f, 0.0f));

                    quat orientation = glm.normalize(qy * camera.Node.Rotation * qx);

                    camera.Node.Translate(delta_translation * glm.conjugate(orientation), TransformSpace.PARENT);
                    camera.Node.Rotation = orientation;
                }

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
            MainView.Attach(null, null, null);
            camera = null;

            scene?.Dispose();
            scene = null;

            base.Destroy(disposing);
        }
    }


}
