using SharpGame;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;


namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = -6)]
    public class NewRenderer : Sample
    {
        RenderPipeline renderer = new RenderPipeline();


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

                new Node("Camera", new vec3(0, 2, -10), glm.radians(10, 0, 0) )
                {
                    new Camera
                    {
                        NearClip = 0.5f,
                        FarClip = 100,
                    },

                },

            };

            camera = scene.GetComponent<Camera>(true);
            
            {
                var node = scene.CreateChild("Sky");
                var staticModel = node.AddComponent<StaticModel>();

                var importer = new AssimpModelReader 
                { 
                    vertexComponents = new[] 
                    { 
                        VertexComponent.Position, 
                        VertexComponent.Texcoord
                    }
                };
                var model = importer.Load("models/vegetation/skysphere.dae");
                staticModel.SetModel(model);
                staticModel.SetBoundingBox(new BoundingBox(-10000, 10000));
                var mat = new Material("shaders/SkySphere.shader");
                staticModel.SetMaterial(mat);
            }
            
            {
                var node = scene.CreateChild("Plane");
                var staticModel = node.AddComponent<StaticModel>();
                var model = GeometryUtil.CreatePlaneModel(100, 100, 32, 32, true);
                staticModel.SetModel(model);
                var mat = Resources.Load<Material>("materials/Grass.material");
                mat.SetTexture("NormalMap", Texture.Blue);
                staticModel.SetMaterial(mat);
            }

            MainView.Attach(camera, scene);

        }

    }
}
