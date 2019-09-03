
namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 2)]
    public class Lighting : Sample
    {
        public override void Init()
        {
            base.Init();

            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera", new vec3(0.0f, 2.0f, -10));
          
            camera = cameraNode.CreateComponent<Camera>();
            camera.Fov = MathUtil.Radians(60);
            camera.AspectRatio = (float)Graphics.Width / Graphics.Height;

            var node = scene.CreateChild("Mesh");
            node.Yaw(MathUtil.Radians(90), TransformSpace.LOCAL);

            var staticModel = node.AddComponent<StaticModel>();
            staticModel.SetModel("models/voyager/voyager.dae");

            var colorMap = Resources.Load<Texture>("models/voyager/voyager_bc3_unorm.ktx");

            var mat = new Material("Shaders/LitSolid.shader");
            mat.SetTexture("DiffMap", colorMap);
            staticModel.SetMaterial(mat);

            Renderer.MainView.Attach(camera, scene);
        }


    }
}
