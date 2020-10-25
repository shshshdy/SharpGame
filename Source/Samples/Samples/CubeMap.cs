using ImGuiNET;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = -7)]

    public class CubeMap : Sample
    {
        Material material;
        float lodBias = 1.0f;

        string[] names = {"sphere", "teapot", "torusknot" };
        string[] filenames = { "models/sphere.obj", "models/teapot.dae", "models/torusknot.obj" };
        int selected;
        StaticModel staticModel;

        public override void Init()
        {
            scene = new Scene
            {
                new Node("Camera", new vec3(0, 5, -10), glm.radians(30, 0, 0))
                {
                    new Camera
                    {
                        Fov = glm.radians(60)
                    },
                },
            };

            camera = scene.GetComponent<Camera>(true);
          
            var cubeMap = Resources.Load<Texture>("textures/cubemap_yokohama_bc3_unorm.ktx");
            {
                var model = Resources.Load<Model>("Models/cube.obj");
                var node = scene.CreateChild("Sky");
                node.Scaling = new vec3(30.0f);
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);

                var mat = new Material("Shaders/Skybox.shader");
                mat.SetTexture("EnvMap", cubeMap);

                staticModel.SetMaterial(mat);
            }
                        
            {
                material = new Material("Shaders/Reflect.shader");
                material.SetTexture("ReflMap", cubeMap);
                material.SetShaderParameter("lodBias", lodBias);

                var node = scene.CreateChild("Model");
                node.Scaling = new vec3(0.1f);

                staticModel = node.AddComponent<StaticModel>();
                SetModel(filenames[0]);
                staticModel.SetMaterial(material);

            }

            MainView.Attach(camera, scene);

        }

        void SetModel(string filePath)
        {
            var model = Resources.Load<Model>(filePath);

            staticModel.SetModel(model);
        }

        public override void OnGUI()
        {
            if (ImGui.Begin("HUD"))
            {
                if (ImGui.Combo("Model", ref selected, names, names.Length))
                {
                    SetModel(filenames[selected]);
                }

                if (ImGui.SliderFloat("lodBias", ref lodBias, 0, 10))
                {
                    material.SetShaderParameter("lodBias", lodBias);
                }
            }

        }

    }
}
