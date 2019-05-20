using System;
using System.Collections.Generic;
using ImGuiNET;
using SharpGame;

namespace SharpGame
{
    public class SampleDescAttribute : System.Attribute
    {
        public string name;
        public string desc;
        public int sortOrder;
    }

    public class Sample : Object
    {
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

            var input = Input.Instance;

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


}
