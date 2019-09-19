
namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 2)]
    public class Lighting : Sample
    {
        FrameGraph frameGraph;
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

            var cameraNode = scene.CreateChild("Camera", new vec3(-8.0f, -5.0f, 0));
            cameraNode.EulerAngles = glm.radians(0, 90, 0);

            camera = cameraNode.CreateComponent<Camera>();

            var node = scene.CreateChild("Mesh");

            var staticModel = node.AddComponent<StaticModel>();
            staticModel.SetModel("models/sibenik/sibenik_bubble.fbx");// "models/voyager/voyager.dae");

            frameGraph = new FrameGraph
            {
                new ShadowPass(),
                new LightComputePass(),
                new ScenePass()

            };

            Renderer.MainView.Attach(camera, scene, frameGraph);
        }


    }
}
