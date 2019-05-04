//using MessagePack;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame.Samples.StaticScene
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WorldViewProjection
    {
        public Matrix World;
        public Matrix View;
        public Matrix ViewInv;
        public Matrix ViewProj;
    }

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

        protected override void OnInit()
        {
            SubscribeToEvent<BeginRenderPass>(Handle);
            

            testShader_ = new Shader
            {
                Name = "Test",
                ["main"] = new Pass("Textured.vert.spv", "Textured.frag.spv")
            };

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
            camera_.AspectRatio = (float)graphics_.Platform.Width / graphics_.Platform.Height;

            model_ = resourceCache_.Load<Model>("Models/Mushroom.mdl").Result;

            var staticModel = node_.AddComponent<StaticModel>();
            staticModel.SetModel(model_);
            geometry_ = model_.GetGeometry(0, 0);

            //_cubeTexture = ResourceCache.Load<Texture>("IndustryForgedDark512.ktx").Result;

            var mat = new Material();
            mat.Shader = testShader_;
            mat.SetTexture("sampler_Color", _cubeTexture);
            staticModel.SetMaterial(0, mat);

            renderer_.MainView.scene = scene_;
            renderer_.MainView.camera = camera_;

            //geometry_ = GeometricPrimitive.CreateCube(1.0f, 1.0f, 1.0f);
            /*
            MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            byte[] bytes = MessagePackSerializer.Serialize(testShader_);

            System.IO.File.WriteAllText("test_shader.json", MessagePackSerializer.ToJson(bytes));
            */
            //Shader newObj = MessagePackSerializer.Deserialize(System.IO.File.ReadAllBytes(path));
            /*
            JsonSerializer.SetDefaultResolver(StandardResolver.ExcludeNullSnakeCase);
            {
                byte[] bytes = Utf8Json.JsonSerializer.Serialize(testShader_);
                var json = Utf8Json.JsonSerializer.PrettyPrint(bytes);

                System.IO.File.WriteAllText("test_shader.json", json);
            }*/

        }

        public override void Dispose()
        {
            testShader_.Dispose();

            base.Dispose();
        }


        protected override void Update(Timer timer)
        {
            /*
            const float twoPi = (float)System.Math.PI * 2.0f;
            const float yawSpeed = twoPi / 4.0f;
            const float pitchSpeed = 0.0f;
            const float rollSpeed = twoPi / 4.0f;
         
            _wvp.World = Matrix.RotationYawPitchRoll(
                timer.TotalTime * yawSpeed % twoPi,
                timer.TotalTime * pitchSpeed % twoPi,
                timer.TotalTime * rollSpeed % twoPi);

           // _wvp.World = Matrix.Identity;

            SetViewProjection();

            UpdateUniformBuffers();*/
        }

        void Handle(BeginRenderPass e)
        {/*
            var cmdBuffer = e.commandBuffer;
            var pipeline = pipeline_.GetGraphicsPipeline(e.renderPass, testShader_, geometry_);
            cmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, pipeline_.pipelineLayout, _descriptorSet);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            geometry_.Draw(cmdBuffer);*/
        }
        
    }
}
