using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 1)]
    public class Pbr : Sample
    {
        List<Material> materials = new List<Material>();
        public override void Init()
        {
            base.Init();

            scene = new Scene
            {
                new DebugRenderer(),

                new Node("Camera", new vec3(0.0f, 0.0f, -20))
                {
                    new Camera
                    {
                        Fov = glm.radians(60),
                        FarClip = 1000.0f
                    },
                },

            };

            {
                Shader shader = Resources.Load<Shader>("Shaders/Pbr.shader");

                AssimpModelReader importer = new AssimpModelReader();
                importer.SetVertexComponents(new[] { VertexComponent.Position, VertexComponent.Normal });
                var model = importer.Load("Models/sphere.gltf");


                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        var node = scene.CreateChild("Model", new vec3(i * 4 - 6.0f, 4 * j - 6.0f, 0));
                        var staticModel = node.AddComponent<StaticModel>();
                        staticModel.SetModel(model);

                        var mat = new Material(shader);
                        SetRandomMaterial(mat);
                        staticModel.SetMaterial(mat);
                        materials.Add(mat);
                    }
                }
            }

            camera = scene.GetComponent<Camera>(true);
            MainView.Attach(camera, scene);
        }

        void SetRandomMaterial(Material material)
        {
            material.SetShaderParameter("r", glm.random());
            material.SetShaderParameter("g", glm.random());
            material.SetShaderParameter("b", glm.random());          
            material.SetShaderParameter("ambient", 0.0025f);
            material.SetShaderParameter("roughness", glm.clamp(glm.random(), 0.005f, 1.0f));
            material.SetShaderParameter("metallic", glm.clamp(glm.random(), 0.005f, 1.0f));
        }


        public override void OnGUI()
        {
            if (ImGui.Begin("HUD"))
            {
                if (ImGui.Button("Randomize"))
                {
                    foreach(var mat in materials)
                    {
                        SetRandomMaterial(mat);
                    }
                }

            }

        }

    }
}
