using SharpGame.Sdl2;
using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame
{
    public class SampleApp : Application
    {
        protected float zoom;
        protected float zoomSpeed = 50f;
        protected Vector3 rotation;
        protected float rotationSpeed = 1f;
        protected Vector3 cameraPos = new Vector3();
        protected Vector2 mousePos;

        protected vkCamera camera = new vkCamera();

        protected Vulkan.VkClearColorValue defaultClearColor => new Vulkan.VkClearColorValue(0.025f, 0.025f, 0.025f, 1.0f);

        protected override void CreateWindow()
        {
            base.CreateWindow();

            NativeWindow.MouseWheel += OnMouseWheel;
            NativeWindow.MouseMove += OnMouseMove;
            NativeWindow.MouseDown += OnMouseDown;
            NativeWindow.KeyDown += OnKeyDown;
        }

        private void OnMouseDown(MouseEvent e)
        {
            if (e.Down)
            {
                mousePos = new Vector2(snapshot.MousePosition.X, snapshot.MousePosition.Y);
            }
        }

        private void OnMouseMove(MouseMoveEventArgs e)
        {
            if (e.State.IsButtonDown(MouseButton.Right))
            {
                int posx = (int)e.MousePosition.X;
                int posy = (int)e.MousePosition.Y;
                zoom += (mousePos.Y - posy) * .005f * zoomSpeed;
                camera.translate(new Vector3(-0.0f, 0.0f, (mousePos.Y - posy) * .005f * zoomSpeed));
                mousePos = new Vector2(posx, posy);
            }

            if (e.State.IsButtonDown(MouseButton.Left))
            {
                int posx = (int)e.MousePosition.X;
                int posy = (int)e.MousePosition.Y;
                rotation.X += (mousePos.Y - posy) * 1.25f * rotationSpeed;
                rotation.Y -= (mousePos.X - posx) * 1.25f * rotationSpeed;
                camera.rotate(new Vector3((mousePos.Y - posy) * camera.rotationSpeed, -(mousePos.X - posx) * camera.rotationSpeed, 0.0f));
                mousePos = new Vector2(posx, posy);
            }

            if (e.State.IsButtonDown(MouseButton.Middle))
            {
                int posx = (int)e.MousePosition.X;
                int posy = (int)e.MousePosition.Y;
                cameraPos.X -= (mousePos.X - posx) * 0.01f;
                cameraPos.Y -= (mousePos.Y - posy) * 0.01f;
                camera.translate(new Vector3(-(mousePos.X - posx) * 0.01f, -(mousePos.Y - posy) * 0.01f, 0.0f));
                mousePos.X = posx;
                mousePos.Y = posy;
            }
        }

        private void OnMouseWheel(MouseWheelEventArgs e)
        {
            var wheelDelta = e.WheelDelta;
            zoom += wheelDelta * 0.005f * zoomSpeed;
            camera.translate(new Vector3(0.0f, 0.0f, wheelDelta * 0.005f * zoomSpeed));
        }

        private void OnKeyDown(KeyEvent e)
        {
            KeyPressed(e.Key);
        }

        protected virtual void KeyPressed(Key key)
        {
        }
    }


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

        System.Numerics.Vector2 mousePos_ = System.Numerics.Vector2.Zero;
        float yaw_;
        float pitch_;
        float rotSpeed_ = 0.5f;
        float wheelSpeed_ = 150.0f;
        float moveSpeed_ = 15.0f;
        System.Numerics.Vector3 offset_;


        public Sample()
        {
        }

        public virtual void Init()
        {
        }

        public virtual void Update()
        {
            if (camera_ == null)
            {
                return;
            }

            var input = Input.Instance;

            if (mousePos_ == System.Numerics.Vector2.Zero)
                mousePos_ = input.MousePosition;

            offset_ = System.Numerics.Vector3.Zero;
            if (input.IsMouseDown(MouseButton.Right))
            {
                System.Numerics.Vector2 delta = (input.MousePosition - mousePos_) * Time.Delta * rotSpeed_ * camera_.AspectRatio;

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
            /*
            if (input.IsMouseDown(MouseButton.Middle))
            {
                Vector2 delta = input.MousePosition - mousePos_;
                offset_.X = -delta.X;
                offset_.Y = delta.Y;
            }

            camera_.Node.Translate(offset_ * Time.Delta * moveSpeed_ + new Vector3(0, 0, input.WheelDelta * Time.Delta * wheelSpeed_), TransformSpace.LOCAL);
            */
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
