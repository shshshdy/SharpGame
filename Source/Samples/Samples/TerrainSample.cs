using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 11)]
    public class TerrainSample : Sample
    {
        Terrain terrain;
        public override void Init()
        {
            scene = new Scene()
            {
                new DebugRenderer { },

                new Node("Camera", new vec3(18.0f, 22.5f, 57.5f), glm.radians(12.0f, -159.0f, 0.0f))
                {
                    new Camera
                    {
                        NearClip = 0.1f,
                        FarClip = 512.0f,
                        Fov = glm.radians(60)
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

                ImGui.Checkbox("Enable Tessellation", ref terrain.Tessellation);
                ImGui.InputFloat("Tessellation Factor", ref terrain.TessellationFactor, 0.1f);
                
            }

            ImGui.End();
        }
    }





}
