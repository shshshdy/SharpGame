using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 8)]
    public class Raytracing : Sample
    {
        struct UboCompute
        {
            vec3 lightPos;
            // Aspect ratio of the viewport
            float aspectRatio;
            vec4 fogColor;// = glm::vec4(0.0f);

            vec3 pos;// = glm::vec3(0.0f, 1.5f, 4.0f);
            float padding;
            vec3 lookat;// = glm::vec3(0.0f, 0.5f, 0.0f);
            float fov;// = 10.0f;
        
        }

        private RenderPipeline renderer = new RenderPipeline();
        private Texture storageTex;
        private Buffer uniformBuffer;

        private Geometry geometry;
        private Material material;


        private Pass computePipeline;
        private ResourceSet computeResourceSet;


        public override void Init()
        {
            base.Init();

            GenerateQuad();

            var shader = Resources.Load<Shader>("shaders/Particle.shader");

            storageTex = Texture.CreateStorage(2048, 2048, Format.R8g8b8a8Snorm);
            uniformBuffer = Buffer.CreateUniformBuffer<UboCompute>();
            
            material = new Material(shader);

            material.PipelineResourceSet[0].ResourceSet[0].Bind(storageTex);

            computePipeline = shader.GetPass("compute");
            computeResourceSet = new ResourceSet(computePipeline.PipelineLayout.ResourceLayout[0], storageTex, uniformBuffer);

            renderer.AddComputePass(Docompute);
            renderer.AddGraphicsPass(DrawQuad);

            MainView.Attach(null, null, renderer);

        }
        // Setup vertices for a single uv-mapped quad
        void GenerateQuad()
        {
            const float dim = 1.0f;
            Buffer vertexBuffer = null;
            /*
            = Buffer.Create( { { { dim, dim, 0.0f }, { 1.0f, 1.0f } },
        { { -dim, dim, 0.0f }, { 0.0f, 1.0f } },
        { { -dim, -dim, 0.0f }, { 0.0f, 0.0f } },
        { { dim, -dim, 0.0f }, { 1.0f, 0.0f } } };

            vertices.create(vertexBuffer, vk::BufferUsageFlagBits::eVertexBuffer);
            std::vector<uint32_t> indexBuffer = { 0, 1, 2, 2, 3, 0 };
            indexCount = (uint32_t)indexBuffer.size();
            indices.create(indexBuffer, vk::BufferUsageFlagBits::eIndexBuffer);
            */

            geometry = new Geometry
            {
                VertexBuffers = new[] { vertexBuffer },

                VertexLayout = new VertexLayout
                (
                    new VertexAttribute(0, 0, Format.R32g32Sfloat, 0),
                    //new VertexInputAttribute(0, 1, Format.R32g32Sfloat, 8),
                    new VertexAttribute(0, 1, Format.R32g32b32a32Sfloat, 16)

                )

            };

            geometry.SetDrawRange(PrimitiveTopology.TriangleList, 0, 6);

        }

        public override void Update()
        {
            base.Update();

            //uniformBuffer.SetData(ref ubo);
        }

        private void DrawQuad(GraphicsSubpass renderPass, RenderContext rc, CommandBuffer cmd)
        {
            var shader = material.Shader;

            cmd.DrawGeometry(geometry, shader.Main, 0, material);
            
        }

        private void Docompute(ComputePass renderPass, RenderContext rc, CommandBuffer cb)
        {
            // Record particle movements.
            cb.BindComputePipeline(computePipeline);
            cb.BindComputeResourceSet(computePipeline.PipelineLayout, 0, computeResourceSet);
            cb.Dispatch(storageTex.width / 16, storageTex.height / 16, 1);
        }
    }
}
