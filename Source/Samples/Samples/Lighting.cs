
namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 2)]
    public class Lighting : Sample
    {
        public override void Init()
        {
            base.Init();

            scene = new Scene
            {
                new Environment
                {
                    SunlightDir = glm.normalize(new vec3(-1.0f, -1.0f, 0.0f))
                },
            };

            var cameraNode = scene.CreateChild("Camera", new vec3(-10.0f, -13.0f, 0));
            cameraNode.EulerAngles = glm.radians(0, 90, 0);

            camera = cameraNode.CreateComponent<Camera>();
            camera.Fov = glm.radians(60);

            var node = scene.CreateChild("Mesh");

            var staticModel = node.AddComponent<StaticModel>();
            staticModel.SetModel("models/sibenik/sibenik.dae");// "models/voyager/voyager.dae");
            
            Renderer.MainView.Attach(camera, scene);
        }


    }
}
