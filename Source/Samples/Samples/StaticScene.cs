using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 1)]
    public class StaticScene : Sample
    {

        public override void Init()
        {
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0, 0, -5);
            camera = cameraNode.CreateComponent<Camera>();

            Renderer.Instance.MainView.Attach(camera, scene);

        }
    }
}
