using SharpGame;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;


namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 6)]
    public class NewRenderer : Sample
    {
        RenderPipeline renderer = new NewRenderPipeline();

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

            var importer = new AssimpModelReader
            {
                vertexComponents = new []
                {
                    VertexComponent.Position,
                    VertexComponent.Texcoord
                }
            };
            
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
                var node = scene.CreateChild("Plane");
                var staticModel = node.AddComponent<StaticModel>(); 
                //importer.scale = 25;
                var model = 
                    //importer.Load("models/vegetation/plane_circle.dae");
                GeometryUtil.CreatePlaneModel(100, 100, 32, 32, true);
                staticModel.SetModel(model);
                var mat = Resources.Load<Material>("materials/Grass.material");
                mat.SetTexture("NormalMap", Texture.Blue);
                mat.SetTexture("SpecMap", Texture.Black);
                staticModel.SetMaterial(mat);
            }
 
            {
                var node = scene.CreateChild("vegetation");
                var staticModel = node.AddComponent<StaticModelGroup>();

                importer.scale = 0.0025f;
                importer.vertexComponents = new[] 
                { VertexComponent.Position, VertexComponent.Normal, VertexComponent.Texcoord, VertexComponent.Color };
                var model = importer.Load("models/vegetation/plants.dae");
                staticModel.SetModel(model);

                var mat = new Material("shaders/IndirectDraw.shader");
                mat.Shader.Main.VertexLayout = new VertexLayout(importer.vertexComponents, 
                    new[]{ VertexComponent.Float3, VertexComponent.Float3, VertexComponent.Float1, VertexComponent.Int1 });
                Texture tex = Resources.Load<Texture>("models/vegetation/textures/texturearray_plants_bc3_unorm.ktx");
                mat.SetTexture("DiffMap", tex);
                staticModel.SetMaterial(mat);
            }

            MainView.Attach(camera, scene, renderer);

        }

    }
}
