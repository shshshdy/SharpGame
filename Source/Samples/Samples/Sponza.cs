using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = -4)]
    public class Sponza : Sample
    {
        public override void Init()
        {
            scene = new Scene
            {
                new Environment
                {
                    SunlightDir = glm.normalize(new vec3(-1.0f, -1.0f, 0.0f))
                }
            };

            camera = scene.CreateChild("Camera").
                WithPosition(new vec3(1200, 35, -75))
                .WithRotation(glm.radians(0, 270, 0))
                .CreateComponent<Camera>();
            camera.FarClip = 3000.0f;

            {
                var model = Resources.Load<Model>(/*"Models/sponza/sponza.dae");//*/ "Models/crysponza_bubbles/sponza.obj");
                var node = scene.CreateChild("sponza");
                var staticModel = node.AddComponent<StaticModel>();
                //staticModel.CastShadows = true;
                staticModel.SetModel(model);
            }
            
            Renderer.MainView.Attach(camera, scene);

        }
    }
}

