using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = -1)]
    public class Pbr : Sample
    {
        public override void Init()
        {
            base.Init();

            scene = new Scene
            {
                new Node("Camera", new vec3(120.0f, 0.0f, -50))
                {
                    new Camera
                    {
                        Fov = glm.radians(60)
                    },
                },

            };

            {
                var mat = new Material("Shaders/Pbr.shader");
                //mat.SetTexture("DiffMap", Texture.White);

                var model = Resources.Load<Model>("Models/sphere.gltf");

                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        var node = scene.CreateChild("Model", new vec3(i * 50 - 25 * 50, 50 * j, 0));
                        var staticModel = node.AddComponent<StaticModel>();
                        staticModel.SetModel(model);
                        staticModel.SetMaterial(mat);
                        
                    }
                }
            }

            camera = scene.GetComponent<Camera>(true);
            MainView.Attach(camera, scene);
        }
    }
}
