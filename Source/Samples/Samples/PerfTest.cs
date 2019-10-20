using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 4)]
    public class PerfTestScene : Sample
    {
        public override void Init()
        {
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera", new vec3(0, 1500, -1500), glm.radians(50, 0, 0));
            
            camera = cameraNode.CreateComponent<Camera>();
            camera.FarClip = 3000.0f;

            {
                var mat = new Material("Shaders/Basic.shader");
                mat.SetTexture("DiffMap", Texture.White);

                var model = Resources.Load<Model>("Models/sphere.obj");

                for (int i = 0; i < 50; i++)
                {
                    for (int j = 0; j < 50; j++)
                    {
                        for(int k = 0; k < 4; k++)
                        {
                            var node = scene.CreateChild("Model", new vec3(i * 50 - 25 * 50, 50 * k, j * 50 - 25 * 50));                            
                            var staticModel = node.AddComponent<StaticModel>();
                            staticModel.SetModel(model);
                            staticModel.SetMaterial(mat);
                        }
                    }
                }
            }

            Renderer.MainView.Attach(camera, scene);
            
        }
    }
}
