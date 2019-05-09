//using MessagePack;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 0)]
    public class StaticSceneApp : Sample
    {
        private Node node_;
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

            node_ = new Node
            {
                Position = new Vector3(0, 0, 0)
            };

            scene_.AddChild(node_);

            var cameraNode = scene_.CreateChild("Camera");
            cameraNode.Position = new Vector3(0, 0, -3);           
            cameraNode.LookAt(Vector3.Zero);

            camera_ = cameraNode.AddComponent<Camera>();
            camera_.AspectRatio = (float)Graphics.Width / Graphics.Height;

            model_ = ResourceCache.Load<Model>("Models/Mushroom.mdl").Result;

            var staticModel = node_.AddComponent<StaticModel>();
            staticModel.SetModel(model_);


            _cubeTexture = ResourceCache.Load<Texture>("IndustryForgedDark512.ktx").Result;

            var mat = new Material();
            mat.Shader = texturedShader;
            mat.SetTexture("sampler_Color", _cubeTexture);
            staticModel.SetMaterial(0, mat);

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
