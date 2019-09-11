using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 4)]
    public class Sponza : Sample
    {
        public override void Init()
        {
            scene = new Scene();

            var camera = scene.CreateChild("Camera").
                WithPosition(new vec3(0, 20, -30)).WithRotation(glm.radians(30, 0, 0))
                        .CreateComponent<Camera>();
            camera.FarClip = 3000.0f;

            {
                var model = Resources.Load<Model>("Models/crysponza_bubbles/sponza.obj");
                var node = scene.CreateChild("sponza");
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);
            }
            
            Renderer.MainView.Attach(camera, scene);

        }
    }
}

