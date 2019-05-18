using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;

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
                viewUpdated = true;
            }

            if (e.State.IsButtonDown(MouseButton.Left))
            {
                int posx = (int)e.MousePosition.X;
                int posy = (int)e.MousePosition.Y;
                rotation.X += (mousePos.Y - posy) * 1.25f * rotationSpeed;
                rotation.Y -= (mousePos.X - posx) * 1.25f * rotationSpeed;
                camera.rotate(new Vector3((mousePos.Y - posy) * camera.rotationSpeed, -(mousePos.X - posx) * camera.rotationSpeed, 0.0f));
                mousePos = new Vector2(posx, posy);
                viewUpdated = true;
            }

            if (e.State.IsButtonDown(MouseButton.Middle))
            {
                int posx = (int)e.MousePosition.X;
                int posy = (int)e.MousePosition.Y;
                cameraPos.X -= (mousePos.X - posx) * 0.01f;
                cameraPos.Y -= (mousePos.Y - posy) * 0.01f;
                camera.translate(new Vector3(-(mousePos.X - posx) * 0.01f, -(mousePos.Y - posy) * 0.01f, 0.0f));
                viewUpdated = true;
                mousePos.X = posx;
                mousePos.Y = posy;
            }
        }

        private void OnMouseWheel(MouseWheelEventArgs e)
        {
            var wheelDelta = e.WheelDelta;
            zoom += wheelDelta * 0.005f * zoomSpeed;
            camera.translate(new Vector3(0.0f, 0.0f, wheelDelta * 0.005f * zoomSpeed));
            viewUpdated = true;
        }

        private void OnKeyDown(KeyEvent e)
        {
            KeyPressed(e.Key);
        }

        protected virtual void KeyPressed(Key key)
        {
        }
    }
}
