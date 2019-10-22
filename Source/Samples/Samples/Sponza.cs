namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 2)]
    public class Sponza : Sample
    {
        public override void Init()
        {
            scene = new Scene
            {
                new Environment
                {
                    SunlightDir = glm.normalize(new vec3(-1.0f, -1.0f, 0.0f))
                },

                new Node("Camera", new vec3(1200, 35, -75), glm.radians(0, 270, 0) )
                {
                    new Camera
                    {
                        NearClip = 1.0f,
                        FarClip = 3000.0f,
                    },

                },
            };

            {
                var model = Resources.Load<Model>("Models/crysponza_bubbles/sponza.obj");
                var node = scene.CreateChild("sponza");
                var staticModel = node.AddComponent<StaticModel>();
                //staticModel.CastShadows = true;
                staticModel.SetModel(model);
            }

            camera = scene.GetComponent<Camera>(true);
            
            Renderer.MainView.Attach(camera, scene);

        }
    }
}

