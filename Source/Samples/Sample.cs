﻿using System;
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

        public virtual void OnGUI()
        {
            if(camera)
            {
                if (ImGui.Begin("HUD"))
                {
                    ImGui.TextUnformatted("Camera");
                    ImGui.TextUnformatted("pos : " + camera.Node.Position.ToString("0:0.00"));
                    ImGui.TextUnformatted("rot : " + camera.Node.Rotation.ToEuler().ToString("0:0.00"));
                    ImGui.SliderFloat("rot speed: ", ref rotSpeed, 1, 100);
                    ImGui.SliderFloat("move speed: ", ref moveSpeed, 1, 100);
                }

                ImGui.End();
             
            }
       
        }

        protected override void Destroy()
        {
            scene?.Dispose();

            base.Destroy();
        }
    }


}
