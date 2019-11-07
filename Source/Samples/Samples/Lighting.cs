
using ImGuiNET;
using SharpGame;
using System;
using System.Collections.Generic;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 0)]
    public class Lighting : Sample
    {
        RenderPipeline frameGraph;
        List<Light> lights = new List<Light>();
        FastList<Spherical> sphericals = new FastList<Spherical>();
        ClusterRenderer clusterRenderer;

        public override void Init()
        {
            base.Init();

            scene = new Scene
            {
                new Environment
                {
                    SunlightDir = glm.normalize(new vec3(-1.0f, -1.0f, 0.0f))
                },

                new DebugRenderer(),

                new Node("Camera", new vec3(-8.0f, -5.0f, 0), glm.radians(0, 90, 0))
                {
                    new Camera
                    {
                        Fov = glm.radians(60)
                    },
                },
            };

            camera = scene.GetComponent<Camera>(true);
       
            var node = scene.CreateChild("Mesh");
            var staticModel = node.AddComponent<StaticModel>();
            staticModel.SetModel("models/sibenik/sibenik_bubble.fbx");// "models/voyager/voyager.dae");

            BoundingBox aabb = staticModel.WorldBoundingBox;
            SetupLights(scene, aabb, 1024);

            scene.GetComponents(lights, true);
          
            clusterRenderer = new ClusterForwardRenderer();

            MainView.Attach(camera, scene, clusterRenderer);
        }

        vec3 center = vec3.Zero;
        public override void Update()
        {
            base.Update();

            Spherical s = new Spherical();
            foreach (var l in lights)
            {
                s.Set(l.Node.Position - center);
                s.el.z += 0.001f;
                s.Restrict();
                var v = s.GetVec();
                l.Node.Position = center + v;
            }
        }

        public override void OnGUI()
        {
            base.OnGUI();

            if (ImGui.Begin("HUD"))
            {
                ref var queryData = ref clusterRenderer.QueryData;

                ImGui.Text("query data (in ms)");
                ImGui.Text("subpass clustering :" + queryData.Clustering / 1000000.0f);
                ImGui.Text("calc light grids :" + queryData.CalcLightGrids / 1000000.0f);
                ImGui.Text("calc grid offsets :" + queryData.CalcGridOffsets / 1000000.0f);
                ImGui.Text("calc light list :" + queryData.CalcLightList / 1000000.0f);
                ImGui.Text("subpass scene :" + queryData.SceneRender / 1000000.0f);
                ImGui.Text("clear :" + queryData.ClearBuffer / 1000000.0f);
            }
        }

        public static void SetupLights(Scene scene, BoundingBox aabb, int num_lights)
        {
            float light_vol = aabb.Volume / (float)(num_lights);
            float base_range = (float)Math.Pow(light_vol, 1.0f / 3.0f);
            float max_range = base_range * 3.0f;
            float min_range = base_range / 1.5f;
            vec3 half_size = aabb.HalfSize;
            float pos_radius = glm.max(half_size.x, glm.max(half_size.y, half_size.z));
            vec3 fcol = new vec3();

            void Hue2Rgb(ref vec3 ret, float hue)
            {
                float s = hue * 6.0f;
                float r0 = glm.clamp(s - 4.0f, 0.0f, 1.0f);
                float g0 = glm.clamp(s - 0.0f, 0.0f, 1.0f);
                float b0 = glm.clamp(s - 2.0f, 0.0f, 1.0f);

                float r1 = glm.clamp(2.0f - s, 0.0f, 1.0f);
                float g1 = glm.clamp(4.0f - s, 0.0f, 1.0f);
                float b1 = glm.clamp(6.0f - s, 0.0f, 1.0f);

                ret[0] = r0 + r1;
                ret[1] = g0 * g1;
                ret[2] = b0 * b1;
            }

            for (int i = 0; i < num_lights; ++i)
            {
                float range = glm.random(min_range, max_range);
                Hue2Rgb(ref fcol, glm.random(0.0f, 1.0f));

                fcol *= 1.3f;
                fcol -= 0.15f;

                vec3 pos = new vec3(glm.random(-pos_radius, pos_radius),
                    glm.random(-pos_radius, pos_radius),
                    glm.random(-pos_radius, pos_radius));

                var lightNode = scene.CreateChild("light" + i, pos);
                var light = lightNode.AddComponent<Light>();
                light.LightType = LightType.Point;
                light.Range = range;
                light.Color = (Color4)fcol;
            }

        }
    }

    public struct Spherical
    {
        public vec3 el;

        public void Set(vec3 v)
        {
            el.x = glm.length(v); // r
            if (v.x == 0.0f)
            {
                el.y = 0.0f;
                el.z = 0.0f;
            }
            else
            {
                el.y = glm.acos(glm.clamp(-v.y / el.x, -1.0f, 1.0f)); // phi
                el.z = glm.atan(-v.z, v.x); // theta
            }
        }

        public void Restrict()
        {
            // restrict phi to the range of EPS ~ PI - EPS
            el.y = glm.max(glm.epsilon, glm.min(glm.pi - glm.epsilon, el.y));
        }

        public vec3 GetVec()
        {
            float x = el.x * glm.sin(el.y) * glm.cos(el.z);
            float y = -el.x * glm.cos(el.y);
            float z = -el.x * glm.sin(el.y) * glm.sin(el.z);
            return glm.vec3(x, y, z);
        }
    }
}
