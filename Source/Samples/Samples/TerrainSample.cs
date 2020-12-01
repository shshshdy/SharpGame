using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = -11)]
    public class TerrainSample : Sample
    {
        Terrain terrain;
        public override void Init()
        {
            scene = new Scene()
            {
                new Environment
                {
                    SunlightDir = glm.normalize(new vec3(-1.0f, -1.0f, 0.0f))
                },

                new Node("Camera", new vec3(0, 2, -10), glm.radians(10, 0, 0))
                {
                    new Camera
                    {
                        NearClip = 0.5f,
                        FarClip = 400,
                    },

                },

            };

            camera = scene.GetComponent<Camera>(true);
            
            var importer = new AssimpModelReader(VertexComponent.Position, VertexComponent.Texcoord);

            {
                var node = scene.CreateChild("Sky");
                var staticModel = node.AddComponent<StaticModel>();
                importer.scale = 5;
                var model = importer.Load("models/vegetation/skysphere.dae");
                staticModel.SetModel(model);
                var mat = new Material("shaders/SkySphere.shader");
                staticModel.SetMaterial(mat);
            }
            
            {
                var node = scene.CreateChild("Terrain");

                terrain = node.AddComponent<Terrain>();
                terrain.GenerateTerrain();

            }


            MainView.Attach(camera, scene);

        }

        public override void OnGUI()
        {

            if (ImGui.Begin("HUD"))
            {
                bool val = terrain.WireframeMode;
                if(ImGui.Checkbox("Wireframe Mode", ref val))
                {
                    terrain.WireframeMode = val;
                }
            }

            ImGui.End();
        }
    }





}
