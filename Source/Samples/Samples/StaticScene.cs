using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 0)]
    public class StaticScene : Sample
    {
        public override void Init()
        {
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera", new vec3(0, 2, -30), glm.radians(10, 0, 0));
            camera = cameraNode.CreateComponent<Camera>();

            {
                var model = GeometricPrimitive.CreatePlaneModel(100, 100, 32, 32);
                var node = scene.CreateChild("Plane");
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);
                var mat = Resources.Load<Material>("materials/Grass.material");
            
                staticModel.SetMaterial(mat);
            }
           // if (false)
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
                var model = Model.Create(geoList, bboxList);// Resources.Load<Model>("Models/Mushroom.mdl");

                for(int i = 0; i < 100; i++)
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
