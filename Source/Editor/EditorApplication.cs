using Unique;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UniqueEditor
{
    public class EditorApplication : Application
    {
        Scene scene_;
        Node root_;
        Camera camera_;
        DebugRenderer debugRenderer_;
        Model model_;
        Material material_;
        Texture diffTex_;

        List<AnimatedModel> animators_ = new List<AnimatedModel>();
        protected override void Setup()
        {
            base.Setup();

            RegisterSubsystem<AssetDatabase>();

            GUISystem guiSys = RegisterSubsystem<GUISystem>();

            EditorWindow.GetWindow<MainWindow>();

            SubscribeToEvent<GUIEvent>(HandleGUI);
            SubscribeToEvent<Update>(HandleUpdate);
            SubscribeToEvent<PostRenderUpdate>(HandlePostRenderUpdate);

        }

        protected override void Init()
        {
            base.Init();

            scene_ = new Scene();
            camera_ = scene_.CreateChild("Camera").CreateComponent<Camera>();
            root_ = scene_.CreateChild("Parent");

            debugRenderer_ = scene_.CreateComponent<DebugRenderer>();

            ResourceCache cache = ResourceCache.Instance;
            model_ = cache.GetResource<Model>("models/Kachujin/Kachujin.mdl");
            diffTex_ = cache.GetResource<Texture>("models/Kachujin/Textures/Kachujin_diffuse.png");
            // diffTex_ = ResourceCache.Instance.GetResource<Texture>("textures/bark1.dds");
            const int ROWS = 4;
            const int COLS = 4;
            const float GRID_SIZE = 4.0f;

            float offsetY = -ROWS * GRID_SIZE / 2;
            for (int r = 0; r < ROWS; r++)
            {
                float offsetX = -COLS * GRID_SIZE / 2;
                for (int c = 0; c < COLS; c++)
                {
                    Node node = root_.CreateChild($"Child_1_{r}_{c}");
                 
                    AnimatedModel modelObject = node.CreateComponent<AnimatedModel>();
                    modelObject.SetModel(model_);

                    Animation walkAnimation = cache.GetResource<Animation>("Models/Kachujin/Kachujin_Walk.ani");

                    AnimationState state = modelObject.AddAnimationState(walkAnimation);
                    // The state would fail to create (return null) if the animation was not found
                    if (state != null)
                    {
                        // Enable full blending weight and looping
                        state.SetWeight(1.0f);
                        state.SetLooped(true);
                        state.SetTime(MathUtil.Random(walkAnimation.Length));
                    }

                    animators_.Add(modelObject);

                    material_ = new Material
                    {
                        Shader = "shaders/solid.shader",
                        ShaderDefines = "SKINNED"
                    }
                    .SetUniform("MatDiffColor", Color.White)
                    .SetUniform("UVOffset", new Vector4(1, 1, 0, 0))
                    .SetTexture("DiffMap", diffTex_);

                    modelObject.SetMaterial(0, material_);

                    node.Position = new Vector3(offsetX, 0, offsetY);
                    node.Scaling = new Vector3(1, 1, 1);
                    offsetX += GRID_SIZE;
                }

                offsetY += GRID_SIZE;
            }

            camera_.Node.Position = new Vector3(0, 4.0f, -20.0f);
            camera_.Node.LookAt(Vector3.Zero);

            Renderer renderer = GetSubsystem<Renderer>();
            View view = renderer.CreateView(camera_, scene_);

            //IOTest.Test(); 
            //System.IO.File.WriteAllText("test_scene.json", JsonConvert.SerializeObject(scene_, Formatting.Indented));
            //MessagePack.MessagePackSerializer.Serialize(scene_);
            //SerializeUtil.SaveJson("test_scene.json", scene_);
        }

        protected override void Shutdown()
        {
            scene_.Release();
        }

        Vector2 mousePos_ = Vector2.Zero;
        float yaw_;
        float pitch_;
        float rotSpeed_ = 0.5f;
        float wheelSpeed_ = 150.0f;
        float moveSpeed_ = 15.0f;
        Vector3 offset_;

        private void HandleUpdate(ref Update e)
        {
            Input input = GetSubsystem<Input>();

            if (mousePos_ == Vector2.Zero)
                mousePos_ = input.MousePosition;

            offset_ = Vector3.Zero;
            if (input.IsMouseDown(MouseButton.Right))
            {
                Vector2 delta = (input.MousePosition - mousePos_) * Time.Delta * rotSpeed_ * camera_.AspectRatio;

                yaw_ += delta.X;
                pitch_ += delta.Y;

                camera_.Node.Rotation = Quaternion.RotationYawPitchRoll(yaw_, pitch_, 0);

                if (input.IsKeyPressed(Key.W))
                {
                    offset_.Z += 1.0f;
                }

                if (input.IsKeyPressed(Key.S))
                {
                    offset_.Z -= 1.0f;
                }

                if (input.IsKeyPressed(Key.A))
                {
                    offset_.X -= 1.0f;
                }

                if (input.IsKeyPressed(Key.D))
                {
                    offset_.X += 1.0f;
                }
            }

            if (input.IsMouseDown(MouseButton.Middle))
            {
                Vector2 delta = input.MousePosition - mousePos_;
                offset_.X = -delta.X;
                offset_.Y = delta.Y;
            }

            camera_.Node.Translate(offset_ * Time.Delta * moveSpeed_ + new Vector3(0, 0, input.WheelDelta * Time.Delta * wheelSpeed_), TransformSpace.LOCAL);

            mousePos_ = input.MousePosition;

            foreach (var it in animators_)
            {
                it.AnimationStates[0].AddTime(Time.Delta);
            }

        }

        private void HandleGUI(ref GUIEvent e)
        {
            EditorWindow.OnGUI();
        }

        private void HandlePostRenderUpdate(ref PostRenderUpdate e)
        {
        //    Renderer.Instance.DrawDebugGeometry(false);
        }
    }
}
