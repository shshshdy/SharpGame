using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 0)]
    public class CascadeShadowMap : Sample
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

                new Node("Camera", new vec3(0, 2, -30), glm.radians(10, 0, 0) )
                {
                    new Camera
                    {
                        NearClip = 0.5f,
                        FarClip = 100,
                    },

                },

            };

            camera = scene.GetComponent<Camera>(true);
            camera.Node.LookAt(vec3.Zero);

            {
                var model = /*Resources.Load<Model>("models/plane.obj");//*/ GeometricPrimitive.CreatePlaneModel(100, 100, 32, 32, true);
                var node = scene.CreateChild("Plane");
                //node.Scaling = new vec3(5, 5, 5);
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);
                var mat = Resources.Load<Material>("materials/Grass.material");
                mat.SetTexture("NormalMap", Texture.Blue);
                staticModel.SetMaterial(mat);
            }

            {
                KtxTextureReader texReader = new KtxTextureReader
                {
                    Format = Format.R8g8b8a8Unorm,
                    SamplerAddressMode = SamplerAddressMode.Repeat,
                };

                var mat = new Material("Shaders/LitSolid.shader");
                var tex = texReader.Load("textures/oak_bark.ktx");
                mat.SetTexture("DiffMap", tex);

                var mat1 = new Material("Shaders/LitSolid.shader");
                var tex1 = texReader.Load("textures/oak_leafs.ktx");
                mat1.SetTexture("DiffMap", tex1);

                List<Geometry> geoList = new List<Geometry>();
                List<BoundingBox> bboxList = new List<BoundingBox>();
                AssimpModelReader.Import("Models/oak_trunk.dae", geoList, bboxList);
                AssimpModelReader.Import("Models/oak_leafs.dae", geoList, bboxList);
                var model = Model.Create(geoList, bboxList);

                for(int i = 0; i < 400; i++)
                {
                    var node = scene.CreateChild("Model");
                    node.Position = new vec3(MathUtil.Random(-40, 40), 0, MathUtil.Random(-40, 40));
                    node.Rotation = new quat(new vec3(0, glm.radians(MathUtil.Random(0, 360)), 0));
                    node.Scaling = new vec3(MathUtil.Random(2.0f, 4.0f));
                    var staticModel = node.AddComponent<StaticModel>();
                    staticModel.CastShadows = true;
                    staticModel.SetModel(model);
                    staticModel.SetMaterial(0, mat);
                    staticModel.SetMaterial(1, mat1);
                }
            }

            Renderer.MainView.Attach(camera, scene);

        }
    }
}
