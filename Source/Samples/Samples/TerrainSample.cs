using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = -11)]
    public class TerrainSample : Sample
    {
        public override void Init()
        {
            scene = new Scene()
            {
                new Octree { },
                new DebugRenderer { },

                new Environment
                {
                    SunlightDir = glm.normalize(new vec3(-1.0f, -1.0f, 0.0f))
                },

                new Node("Camera", new vec3(0, 2, -10), glm.radians(10, 0, 0))
                {
                    new Camera
                    {
                        NearClip = 0.5f,
                        FarClip = 400,
                    },

                },

            };

            camera = scene.GetComponent<Camera>(true);

            var importer = new AssimpModelReader(VertexComponent.Position, VertexComponent.Texcoord);

            {
                var node = scene.CreateChild("Sky");
                var staticModel = node.AddComponent<StaticModel>();
                importer.scale = 5;
                var model = importer.Load("models/vegetation/skysphere.dae");
                staticModel.SetModel(model);
                staticModel.SetBoundingBox(new BoundingBox(-10000, 10000));
                var mat = new Material("shaders/SkySphere.shader");
                staticModel.SetMaterial(mat);
            }

            {
                var node = scene.CreateChild("Terrain");

                var terrain = node.AddComponent<Terrain>();
                terrain.GenerateTerrain();

                var mat = new Material("shaders/Terrain.shader");
                terrain.SetMaterial(mat);
            }


            MainView.Attach(camera, scene);

        }


    }





}
