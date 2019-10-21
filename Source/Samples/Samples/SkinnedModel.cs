using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 3)]
    public class SkinnedModel : Sample
    {
        List<AnimatedModel> animators_ = new List<AnimatedModel>();
        public override void Init()
        {
            scene = new Scene
            {
                new Octree(),
                new DebugRenderer(),

                new Node("Camera", new vec3(0, 20, -30), glm.radians(30, 0, 0) )
                {
                    new Camera
                    {
                    },

                },
            };

            camera = scene.GetComponent<Camera>(true);
            {
                var model = GeometricPrimitive.CreatePlaneModel(100, 100);
                var node = scene.CreateChild("Plane");
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);
                var mat = Resources.Load<Material>("materials/Stone.material");

                staticModel.SetMaterial(mat);
            }

            {
                var colorMap = Resources.Load<Texture>("Models/Mutant/Textures/Mutant_diffuse.jpg");
                var normalMap = Resources.Load<Texture>("Models/Mutant/Textures/Mutant_normal.jpg");
                var mat = new Material("Shaders/Skinned.shader");
                mat.SetTexture("DiffMap", colorMap);
                mat.SetTexture("NormalMap", normalMap);
                var model = Resources.Load<Model>("Models/Mutant/Mutant.mdl");

                Animation walkAnimation = Resources.Load<Animation>("Models/Mutant/Mutant_Walk.ani");

                for (int i = 0; i < 100; i++)
                {
                    var node = scene.CreateChild("Model", new vec3(glm.random(-20, 20), 0, glm.random(-20, 20)));
                    node.Rotation = new quat(0, glm.radians(glm.random(0, 90)), 0);

                    var animMdoel = node.AddComponent<AnimatedModel>();
                    animMdoel.SetModel(model);
                    animMdoel.SetMaterial(mat);
                    //animMdoel.CastShadows = true;

                    AnimationState state = animMdoel.AddAnimationState(walkAnimation);
                    // The state would fail to create (return null) if the animation was not found
                    if (state != null)
                    {
                        // Enable full blending weight and looping
                        state.SetWeight(1.0f);
                        state.SetLooped(true);
                        state.SetTime(glm.random(walkAnimation.Length));
                    }

                    animators_.Add(animMdoel);
                }
            }

            Renderer.MainView.Attach(camera, scene);
        }

        public override void Update()
        {
            base.Update();

            foreach (var it in animators_)
            {
                it.AnimationStates[0].AddTime(Time.Delta);
            }
        }
    }
}
