using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 3)]
    public class Sponza : Sample
    {
        public override void Init()
        {
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0, 20, -30);
            cameraNode.Rotation = Quaternion.FromEuler(MathUtil.DegreesToRadians(30), 0, 0);

            camera = cameraNode.CreateComponent<Camera>();
            camera.AspectRatio = (float)Graphics.Width / Graphics.Height;
            camera.FarClip = 3000.0f;

            {
                var model = Resources.Load<Model>("Models/crysponza_bubbles/sponza.obj");
                var node = scene.CreateChild("sponza");
                node.Scaling = new Vector3(1.0f);
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);
            }
            
            Renderer.MainView.Attach(camera, scene);

        }
    }
}

