using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 8)]
    public class Raytracing : Sample
    {
        [StructLayout(LayoutKind.Sequential)]
        struct UboCompute
        {
            public vec3 lightPos;
            // Aspect ratio of the viewport
            public float aspectRatio;
            public vec4 fogColor;// = glm::vec4(0.0f);

            public vec3 pos;// = glm::vec3(0.0f, 1.5f, 4.0f);
            public float padding;
            public vec3 lookat;// = glm::vec3(0.0f, 0.5f, 0.0f);
            public float fov;// = 10.0f;
                    
        }

        private RenderPipeline renderer = new RenderPipeline();
        private Texture storageTex;
        private SharedBuffer uniformBuffer;

        private Geometry geometry;
        private Material material;
        private Pass computePipeline;
        private ResourceSet computeResourceSet;

        UboCompute ubo = new UboCompute
        {
            pos = glm.vec3(0.0f, 1.5f, 4.0f),
            lookat = glm.vec3(0.0f, 0.5f, 0.0f),
            fov = 10.0f
        };

        public override void Init()
        {
            base.Init();

            GenerateQuad();

            var shader = Resources.Load<Shader>("shaders/raytracing.shader");

            storageTex = Texture.CreateStorage(2048, 2048, Format.R8g8b8a8Snorm);
            uniformBuffer = new SharedBuffer(BufferUsageFlags.UniformBuffer, (uint)Utilities.SizeOf<UboCompute>());
            
            material = new Material(shader);
            material.SetTexture("samplerColor", storageTex);

            computePipeline = shader.GetPass("Compute");
            computeResourceSet = new ResourceSet(computePipeline.PipelineLayout.ResourceLayout[0], storageTex, uniformBuffer);

            renderer.AddComputePass(Docompute);
            renderer.AddGraphicsPass(DrawQuad);

            MainView.Attach(null, null, renderer);

        }

        void GenerateQuad()
        {
            const float dim = 1.0f;

            VertexPosTex[] vertices =
            {
                new VertexPosTex(dim, dim, 0, 1.0f, 1.0f),
                new VertexPosTex(-dim, dim, 0, 0.0f, 1.0f),
                new VertexPosTex(-dim, -dim, 0, 0.0f, 0.0f),
                new VertexPosTex(dim, -dim, 0, 1.0f, 0.0f),
            };

            int[] indices =
            {
                0, 1, 2, 2, 3, 0,
            };

            geometry = new Geometry
            {
                VertexBuffers = new[] { Buffer.Create(BufferUsageFlags.VertexBuffer, vertices) },
                IndexBuffer = Buffer.Create(BufferUsageFlags.IndexBuffer, indices),
                VertexLayout = VertexPosTex.Layout
            };

            geometry.SetDrawRange(PrimitiveTopology.TriangleList, 0, (uint)indices.Length, 0);         

        }

        public override void Update()
        {
            base.Update();

            ubo.aspectRatio = (float)Graphics.Width / (float)Graphics.Height;
            ubo.lightPos.x = 0.0f + glm.sin(glm.radians(Time.Elapsed * 90.0f)) * 2.0f;
            ubo.lightPos.y = 5.0f;
            ubo.lightPos.z = 1.0f;
            ubo.lightPos.z = 0.0f + glm.cos(glm.radians(Time.Elapsed * 90.0f)) * 2.0f;
       
            uniformBuffer.SetData(ref ubo);
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
