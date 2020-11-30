using System.Collections.Generic;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 2)]
    public class Sponza : Sample
    {
        ClusterRenderer clusterRenderer;
        List<Light> lights = new List<Light>();
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

                new Node("sponza")
                {
                    new StaticModel
                    {
                        ModelFile = "Models/crysponza_bubbles/sponza.obj"
                    },
                },
            };
                        
            camera = scene.GetComponent<Camera>(true);
            var staticModel = scene.GetComponent<StaticModel>(true);

            BoundingBox aabb = staticModel.WorldBoundingBox;
            Lighting.SetupLights(scene, aabb, 1024, lights);

            //clusterRenderer = new ClusterForwardRenderer();
            clusterRenderer = new HybridRenderer();
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

    }
}

