
using SharpGame;
using System;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = -2)]
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

                new DebugRenderer()
            };

            var cameraNode = scene.CreateChild("Camera", new vec3(-8.0f, -5.0f, 0));
            cameraNode.EulerAngles = glm.radians(0, 90, 0);

            camera = cameraNode.CreateComponent<Camera>();

            var node = scene.CreateChild("Mesh");

            var staticModel = node.AddComponent<StaticModel>();
            staticModel.SetModel("models/sibenik/sibenik_bubble.fbx");// "models/voyager/voyager.dae");


            BoundingBox aabb = staticModel.WorldBoundingBox;

            int num_lights = 100;

            float light_vol = aabb.Volume / (float)(num_lights);
            float base_range = (float)Math.Pow(light_vol, 1.0f / 3.0f);
            float max_range = base_range * 3.0f;
            float min_range = base_range / 1.5f;
            vec3 half_size = aabb.HalfSize;
            float pos_radius = glm.max(half_size.x, glm.max(half_size.y, half_size.z));
            vec3 fcol = new vec3();


            void hue_to_rgb(ref vec3 ret, float hue)
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
                float range = MathUtil.Random(min_range, max_range);
                hue_to_rgb(ref fcol, MathUtil.Random(0.0f, 1.0f));

                fcol *= 1.3f;
                fcol -= 0.15f;

                vec3 pos = new vec3(MathUtil.Random(-pos_radius, pos_radius),
                    MathUtil.Random(-pos_radius, pos_radius),
                    MathUtil.Random(-pos_radius, pos_radius));


                var lightNode = scene.CreateChild("light" + i, pos);
                var light = lightNode.AddComponent<Light>();
                light.LightType = LightType.Point;
                light.Range = range;
                light.Color = (Color4) fcol;

            }
   

            frameGraph = new FrameGraph
            {
                new ShadowPass(),

                new ClusterLighting()

            };

            Renderer.MainView.Attach(camera, scene, frameGraph);
        }


    }
}
