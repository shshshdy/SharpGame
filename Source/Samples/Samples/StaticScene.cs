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

            var cameraNode = scene.CreateChild("Camera", new Vector3(0, 20, -30), MathUtil.Radians(30, 0, 0));
            camera = cameraNode.CreateComponent<Camera>();

            {
                var model = GeometricPrimitive.CreatePlaneModel(100, 100);
                var node = scene.CreateChild("Plane");
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);
                var mat = Resources.Load<Material>("materials/Grass.material");
            
                staticModel.SetMaterial(mat);
            }

            {               
                var mat = new Material("Shaders/LitSolid.shader");
                var tex = Texture.LoadFromFile("textures/oak_bark.ktx", Format.R8g8b8a8Unorm, SamplerAddressMode.Repeat);
                mat.SetTexture("DiffMap", tex);

                var mat1 = new Material("Shaders/LitSolid.shader");
                var tex1 = Texture.LoadFromFile("textures/oak_leafs.ktx", Format.R8g8b8a8Unorm, SamplerAddressMode.ClampToEdge);
                mat1.SetTexture("DiffMap", tex1);

                List<Geometry> geoList = new List<Geometry>();
                List<BoundingBox> bboxList = new List<BoundingBox>();
                AssimpModelReader.Import("Models/oak_trunk.dae", geoList, bboxList);
                AssimpModelReader.Import("Models/oak_leafs.dae", geoList, bboxList);
                var model = Model.Create(geoList, bboxList);// Resources.Load<Model>("Models/Mushroom.mdl");

                for(int i = 0; i < 400; i++)
                {
                    var node = scene.CreateChild("Model");
                    node.Position = new Vector3(MathUtil.Random(-40, 20), 0, MathUtil.Random(-40, 20));
                    node.Rotation = Quaternion.FromEuler(0, MathUtil.Radians(MathUtil.Random(0, 90)), 0);
                    node.Scaling = new Vector3(4.0f);
                    var staticModel = node.AddComponent<StaticModel>();
                    staticModel.SetModel(model);
                    staticModel.SetMaterial(0, mat);
                    staticModel.SetMaterial(1, mat1);
                    //staticModel.SetMaterial(mat);
                }
            }

            Renderer.MainView.Attach(camera, scene);

        }
    }
}
