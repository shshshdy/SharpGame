//using MessagePack;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame.Samples.StaticScene
{
    public class StaticSceneApp : Application
    {
        private Scene scene_;
        private Node node_;
        private Model model_;
        private Node cameraNode_;
        private Camera camera_;

        private Shader testShader_;
        private Texture _cubeTexture;

        private Geometry geometry_;

        protected override void Init()
        {

            testShader_ = new Shader
            (
                Name = "Test",
                new Pass("Textured.vert.spv", "Textured.frag.spv")
            );

            scene_ = new Scene();

            node_ = new Node
            {
                Position = new Vector3(0, 0, 0)
            };

            scene_.AddChild(node_);

            cameraNode_ = new Node
            {
                Position = new Vector3(0, 0, -3)
            };

            cameraNode_.LookAt(Vector3.Zero);

            camera_ = cameraNode_.AddComponent<Camera>();
            camera_.AspectRatio = (float)graphics_.GameWindow.Width / graphics_.GameWindow.Height;

            model_ = resourceCache_.Load<Model>("Models/Mushroom.mdl").Result;

            var staticModel = node_.AddComponent<StaticModel>();
            staticModel.SetModel(model_);
            geometry_ = model_.GetGeometry(0, 0);

            _cubeTexture = resourceCache_.Load<Texture>("IndustryForgedDark512.ktx").Result;

            var mat = new Material();
            mat.Shader = testShader_;
            mat.SetTexture("sampler_Color", _cubeTexture);
            staticModel.SetMaterial(0, mat);

            renderer_.MainView.Scene = scene_;
            renderer_.MainView.Camera = camera_;
        }

        protected override void Destroy()
        {
            testShader_.Dispose();

            base.Destroy();
        }

        
    }
}
