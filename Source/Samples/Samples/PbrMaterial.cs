using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 1)]
    public class PbrMaterial : Sample
    {
        public struct MaterialData
        {
            public string name;
            public float roughness;
            public float metallic;
            public float r, g, b;

            public MaterialData(string n, vec3 c, float r, float m)
            { 
                this.name = n;
                this.roughness = r;
                this.metallic = m;
                this.r = c.x;
                this.g = c.y;
                this.b = c.z;
            }

            public void Apply(Material material)
            {
                material.SetShaderParameter("r", r);
                material.SetShaderParameter("g", g);
                material.SetShaderParameter("b", b);
                material.SetShaderParameter("ambient", 0.0025f);
                //material.SetShaderParameter("roughness", roughness);
                //material.SetShaderParameter("metallic", metallic);
            }

        }

        List<Material> materials = new List<Material>();
        FastList<MaterialData> materialDatas = new FastList<MaterialData>();
        int materialIndex = 0;
        public PbrMaterial()
        {
            // Setup some default materials (source: https://seblagarde.wordpress.com/2011/08/17/feeding-a-physical-based-lighting-mode/)
            materialDatas.Add(new MaterialData("Gold", new vec3(1.0f, 0.765557f, 0.336057f), 0.1f, 1.0f));
            materialDatas.Add(new MaterialData("Copper", new vec3(0.955008f, 0.637427f, 0.538163f), 0.1f, 1.0f));
            materialDatas.Add(new MaterialData("Chromium", new vec3(0.549585f, 0.556114f, 0.554256f), 0.1f, 1.0f));
            materialDatas.Add(new MaterialData("Nickel", new vec3(0.659777f, 0.608679f, 0.525649f), 0.1f, 1.0f));
            materialDatas.Add(new MaterialData("Titanium", new vec3(0.541931f, 0.496791f, 0.449419f), 0.1f, 1.0f));
            materialDatas.Add(new MaterialData("Cobalt", new vec3(0.662124f, 0.654864f, 0.633732f), 0.1f, 1.0f));
            materialDatas.Add(new MaterialData("Platinum", new vec3(0.672411f, 0.637331f, 0.585456f), 0.1f, 1.0f));
            // Testing materials
            materialDatas.Add(new MaterialData("White", new vec3(1.0f), 0.1f, 1.0f));
            materialDatas.Add(new MaterialData("Red", new vec3(1.0f, 0.0f, 0.0f), 0.1f, 1.0f));
            materialDatas.Add(new MaterialData("Blue", new vec3(0.0f, 0.0f, 1.0f), 0.1f, 1.0f));
            materialDatas.Add(new MaterialData("Black", new vec3(0.0f), 0.1f, 1.0f));
        }

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
                importer.SetVertexComponents(VertexComponent.Position, VertexComponent.Normal);
                var model = importer.Load("Models/sphere.gltf");
                
                const int GRID_DIM = 7;

                for (int y = 0; y < GRID_DIM; y++)
                {
                    for (int x = 0; x < GRID_DIM; x++)
                    {
                        vec3 pos = new vec3((x - (GRID_DIM - 1) / 2.0f) * 2.5f, (y - (GRID_DIM - 1)/ 2.0f) * 2.5f, 0.0f);
                        var node = scene.CreateChild("Model", pos);
                        var staticModel = node.AddComponent<StaticModel>();
                        staticModel.SetModel(model);

                        var mat = new Material(shader);
                        materialDatas[materialIndex].Apply(mat);

                        mat.SetShaderParameter("metallic", glm.clamp(x / (float)(GRID_DIM - 1), 0.1f, 1.0f));
                        mat.SetShaderParameter("roughness", glm.clamp(y / (float)(GRID_DIM - 1), 0.05f, 1.0f));

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

        void SetActiveMaterial(MaterialData materialData)
        {
            foreach(var mat in materials)
            {
                materialData.Apply(mat);
            }

        }


        public override void OnGUI()
        {
            if (ImGui.Begin("HUD"))
            {
                if(ImGui.BeginCombo("Material", materialDatas[materialIndex].name))
                {
                    foreach (var mat in materialDatas)
                    {
                        bool selected = false;
                        if (ImGui.Selectable(mat.name, ref selected))
                        {
                            SetActiveMaterial(mat);
                        }
                    }

                    ImGui.EndCombo();
                }



                if (ImGui.Button("Randomize"))
                {
                    foreach(var mat in materials)
                    {
                        SetRandomMaterial(mat);
                    }
                }

            }

            ImGui.End();

        }

    }
}
