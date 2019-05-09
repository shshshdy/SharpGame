using System;
using SharpGame;

public class Sample : Object
{
    public Sample()
    {
    }

    public virtual void Init()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void Shutdown()
    {
    }
}

public class SampleApplication : Application
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
        timer_ = CreateSubsystem<Timer>();
        fileSystem_ = CreateSubsystem<FileSystem>(gameWindow_);
        graphics_ = CreateSubsystem<Graphics>(gameWindow_);
        resourceCache_ = CreateSubsystem<ResourceCache>("../Content");
        renderer_ = CreateSubsystem<Renderer>();
        input_ = CreateSubsystem<Input>();

        CreateSubsystem<AssetDatabase>();

        GUISystem guiSys = CreateSubsystem<GUISystem>();

        EditorWindow.GetWindow<MainWindow>();

        this.SubscribeToEvent<BeginFrame>(HandleGUI);
        this.SubscribeToEvent<Update>(HandleUpdate);

    }

    protected override void Init()
    {
        base.Init();

        scene_ = new Scene();
        camera_ = scene_.CreateChild("Camera").CreateComponent<Camera>();
        root_ = scene_.CreateChild("Parent");


        var model = resourceCache_.Load<Model>("Models/Mushroom.mdl").Result;

        var staticModel = root_.AddComponent<StaticModel>();
        staticModel.SetModel(model);

        var shader = new Shader
        (
            Name = "Test",
            new Pass("Textured.vert.spv", "Textured.frag.spv")
            {
                ResourceLayout = new ResourceLayout(
                    new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                    new DescriptorSetLayoutBinding(1, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment)
                )
            }
        );

        var mat = new Material
        {
            Shader = shader
        };

        staticModel.SetMaterial(0, mat);            
        camera_.Node.Position = new Vector3(0, 4.0f, -20.0f);
        camera_.Node.LookAt(Vector3.Zero);

        var renderer = Get<Renderer>();
        // RenderView view = renderer.CreateRenderView(camera_, scene_);

        renderer.MainView.Scene = scene_;
        renderer.MainView.Camera = camera_;
    }

    protected override void Shutdown()
    {
        scene_.Dispose();
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
        Input input = Get<Input>();

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

    private void HandleGUI(ref BeginFrame e)
    {
        EditorWindow.OnGUI();
    }

}
