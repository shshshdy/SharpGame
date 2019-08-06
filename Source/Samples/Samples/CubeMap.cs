using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = -7)]

    public class CubeMap : Sample
    {
        public override void Init()
        {
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0, 0, 0);
            //cameraNode.LookAt(Vector3.Zero);
            cameraNode.Rotation = Quaternion.FromEuler(MathUtil.DegreesToRadians(30), 0, 0);

            camera = cameraNode.CreateComponent<Camera>();
            camera.AspectRatio = (float)Graphics.Width / Graphics.Height;
            camera.FarClip = 3000.0f;

            var shader = Resources.Load<Shader>("Shaders/Textured.shader");
            {

                var model = Resources.Load<Model>("Models/cube.obj");
                var node = scene.CreateChild("Plane");
                node.Scaling = new Vector3(3.0f);
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);
                ref var m = ref node.WorldTransform;

                var colorMap = Resources.Load<Texture2D>("textures/StoneDiffuse.png");
                var mat = new Material(shader);
                mat.SetTexture("DiffMap", colorMap);

                staticModel.SetMaterial(mat);
            }

            Renderer.Instance.MainView.Attach(camera, scene);

        }
    }
}
