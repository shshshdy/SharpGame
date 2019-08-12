﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = -3)]
    public class SkinnedModel : Sample
    {
        List<AnimatedModel> animators_ = new List<AnimatedModel>();
        public override void Init()
        {
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0, 20, -30);
            cameraNode.Rotation = Quaternion.FromEuler(MathUtil.DegreesToRadians(30), 0, 0);

            camera = cameraNode.CreateComponent<Camera>();
            camera.AspectRatio = (float)Graphics.Width / Graphics.Height;
            camera.FarClip = 3000.0f;

            {
                var shader = Resources.Load<Shader>("Shaders/LitSolid.shader");
                var model = Resources.Load<Model>("Models/plane2.dae");
                var node = scene.CreateChild("Plane");
                node.Scaling = new Vector3(3.0f);
                var staticModel = node.AddComponent<StaticModel>();
                staticModel.SetModel(model);
                var colorMap = Resources.Load<Texture2D>("textures/StoneDiffuse.png");
                var mat = new Material(shader);
                mat.SetTexture("DiffMap", colorMap);

                staticModel.SetMaterial(mat);
            }

            {
                var colorMap = Resources.Load<Texture2D>("Models/Mutant/Textures/Mutant_diffuse.jpg");
                var normalMap = Resources.Load<Texture2D>("Models/Mutant/Textures/Mutant_normal.jpg");
                var mat = new Material("Shaders/Skinned.shader");
                mat.SetTexture("DiffMap", colorMap);
                mat.SetTexture("NormalMap", normalMap);
                var model = Resources.Load<Model>("Models/Mutant/Mutant.mdl");

                Animation walkAnimation = Resources.Load<Animation>("Models/Mutant/Mutant_Walk.ani");

                for (int i = 0; i < 100; i++)
                {
                    var node = scene.CreateChild("Model");
                    node.Position = new Vector3(MathUtil.Random(-20, 20), 0, MathUtil.Random(-20, 20));
                    node.Rotation = Quaternion.FromEuler(0, MathUtil.DegreesToRadians(MathUtil.Random(0, 90)), 0);

                    var animMdoel = node.AddComponent<AnimatedModel>();
                    animMdoel.SetModel(model);
                    animMdoel.SetMaterial(mat);


                    AnimationState state = animMdoel.AddAnimationState(walkAnimation);
                    // The state would fail to create (return null) if the animation was not found
                    if (state != null)
                    {
                        // Enable full blending weight and looping
                        state.SetWeight(1.0f);
                        state.SetLooped(true);
                        state.SetTime(MathUtil.Random(walkAnimation.Length));
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
