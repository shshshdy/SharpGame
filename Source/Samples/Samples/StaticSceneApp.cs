using VulkanCore;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 0)]
    public class StaticSceneApp : Sample
    {
        private Model model_;
        private Shader texturedShader;
        private Texture _cubeTexture;


        public override void Init()
        {

            texturedShader = new Shader
            (
                "Test",
                new Pass("Textured.vert.spv", "Textured.frag.spv")
                {
                    ResourceLayout = new ResourceLayout(
                        new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                        new DescriptorSetLayoutBinding(1, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment)
                    )
                }
            );

            scene_ = new Scene();
            var cameraNode = scene_.CreateChild("Camera");
            cameraNode.Position = new Vector3(0, 0, -3);           
            cameraNode.LookAt(Vector3.Zero);

            camera_ = cameraNode.AddComponent<Camera>();
            camera_.AspectRatio = (float)Graphics.Width / Graphics.Height;

            model_ = ResourceCache.Load<Model>("Models/Mushroom.mdl").Result;

            _cubeTexture = ResourceCache.Load<Texture>("IndustryForgedDark512.ktx").Result;

            var mat = new Material();
            mat.Shader = texturedShader;
            mat.SetTexture("sampler_Color", _cubeTexture);
      
            for(int i = 0; i < 10; i++)
            {
                for(int j = 0; j < 10; j++)
                {
                    var node = scene_.CreateChild($"Model_{i}_{j}");
                    node.Position = new Vector3(i*5 - 5*5, 0, j * 5 - 5 * 5);
                    var staticModel = node.AddComponent<StaticModel>();
                    staticModel.SetModel(model_);
                    staticModel.SetMaterial(0, mat);
                }
            }



            Renderer.MainView.Scene = scene_;
            Renderer.MainView.Camera = camera_;
        }

        public override void Shutdown()
        {
            texturedShader.Dispose();

            base.Shutdown();
        }

        
    }
}
