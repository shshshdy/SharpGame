using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    public struct Particle
    {
        public Vector2 pos;                              // Particle position
        public Vector2 vel;                              // Particle velocity
        public Vector4 gradientPos;                      // Texture coordiantes for the gradient ramp map
    };

    [SampleDesc(sortOrder = 8)]
    public class ComputeShader : Sample
    {
        const int PARTICLE_COUNT = 256 * 1024;

        private FrameGraph frameGraph = new FrameGraph();
        private DeviceBuffer storageBuffer;
        private DeviceBuffer uniformBuffer;

        struct UBO
        {
            public float deltaT;
            public float destX;
            public float destY;
            public int particleCount;
        };

        private Geometry geometry;
        private Material material;

        private UBO ubo = new UBO();
        private Pass computePipeline;
        private ResourceSet computeResourceSet;

        private float timer = 0.0f;
        private float animStart = 20.0f;
        private bool animate = true;


        public override void Init()
        {
            base.Init();

            ubo.particleCount = PARTICLE_COUNT;

            var shader = Resources.Load<Shader>("shaders/Particle.shader");

            Random rand = new Random();
            Particle[] particles = new Particle[PARTICLE_COUNT];
            for (int i = 0; i < PARTICLE_COUNT; i++)
            {
                ref Particle particle = ref particles[i];

                particle.pos = rand.NextVector2(new Vector2(-1, -1), new Vector2(1, 1));
                particle.vel = new Vector2(0.0f);
                particle.gradientPos = new Vector4(particle.pos.X / 2.0f, 0, 0, 0);
            }

            storageBuffer = DeviceBuffer.Create(BufferUsageFlags.VertexBuffer | BufferUsageFlags.StorageBuffer, particles, true);
            uniformBuffer = DeviceBuffer.CreateUniformBuffer<UBO>();

            geometry = new Geometry
            {
                VertexBuffers = new [] { storageBuffer },

                VertexLayout = new VertexLayout
                (
                    new[]
                    {
                        new VertexInputBinding(0, 32, VertexInputRate.Vertex)
                    },
                    new[]
                    {
                        new VertexInputAttribute(0, 0, Format.R32g32Sfloat, 0),
                        //new VertexInputAttribute(0, 1, Format.R32g32Sfloat, 8),
                        new VertexInputAttribute(0, 1, Format.R32g32b32a32Sfloat, 16)
                    }
                )

            };

            geometry.SetDrawRange(PrimitiveTopology.PointList, 0, 0, 0, PARTICLE_COUNT);

            material = new Material(shader);

            KtxTextureReader texReader = new KtxTextureReader
            {
                Format = Format.R8g8b8a8Unorm,
            };

            var tex = texReader.Load("textures/particle01_rgba.ktx");            
            var tex1 = texReader.Load("textures/particle_gradient_rgba.ktx");

            AddDebugImage(tex);
            AddDebugImage(tex1);

            material.ResourceSet[0].Bind(tex, tex1);

            computePipeline = shader.GetPass("compute");
            computeResourceSet = new ResourceSet(computePipeline.PipelineLayout.ResourceLayout[0], storageBuffer, uniformBuffer);

            frameGraph.AddGraphicsPass(DrawQuad);
            frameGraph.AddComputePass(Docompute);


            Renderer.Instance.MainView.Attach(null, null, frameGraph);

        }

        public override void Update()
        {
            base.Update();

            if (animate)
            {
                if (animStart > 0.0f)
                {
                    animStart -= Time.Delta * 5.0f;
                }
                else if (animStart <= 0.0f)
                {
                    timer += Time.Delta * 0.04f;
                    if (timer > 1.0f)
                        timer = 0.0f;
                }
            }

            ubo.deltaT = Time.Delta * 2.5f;

            if (animate)
            {
                ubo.destX = (float)Math.Sin(MathUtil.Radians(timer * 360.0f)) * 0.75f;
                ubo.destY = 0.0f;
            }
            else
            {
                float normalizedMx = (mousePos.X - Graphics.Width / 2.0f) / (Graphics.Width / 2.0f);
                float normalizedMy = (mousePos.Y - Graphics.Height / 2) / (Graphics.Height / 2);
                ubo.destX = normalizedMx;
                ubo.destY = normalizedMy;
            }

            uniformBuffer.SetData(ref ubo);
        }

        private void DrawQuad(GraphicsPass renderPass, RenderView view)
        {
            var cb = renderPass.CmdBuffer;
            var shader = material.Shader;

            cb.DrawGeometry(geometry, shader.Main, material);
        }

        private void Docompute(ComputePass renderPass, RenderView view)
        {
            // Record particle movements.
            var cb = renderPass.CmdBuffer;
            var graphicsToComputeBarrier = new BufferMemoryBarrier(storageBuffer,
                AccessFlags.VertexAttributeRead, AccessFlags.ShaderWrite,
                Graphics.GraphicsQueue.FamilyIndex, Graphics.ComputeQueue.FamilyIndex);

            var computeToGraphicsBarrier = new BufferMemoryBarrier(storageBuffer,
                AccessFlags.ShaderWrite, AccessFlags.VertexAttributeRead,
                Graphics.ComputeQueue.FamilyIndex, Graphics.GraphicsQueue.FamilyIndex);

            cb.Begin();

            // Add memory barrier to ensure that the (graphics) vertex shader has fetched attributes
            // before compute starts to write to the buffer.
            cb.PipelineBarrier(PipelineStageFlags.VertexInput, PipelineStageFlags.ComputeShader, ref graphicsToComputeBarrier);
            cb.BindComputePipeline(computePipeline);
            cb.BindComputeResourceSet(computePipeline.PipelineLayout, 0, computeResourceSet);
            cb.Dispatch((uint)storageBuffer.Count / 256, 1, 1);
            // Add memory barrier to ensure that compute shader has finished writing to the buffer.
            // Without this the (rendering) vertex shader may display incomplete results (partial
            // data from last frame).
            cb.PipelineBarrier(PipelineStageFlags.ComputeShader, PipelineStageFlags.VertexInput, ref computeToGraphicsBarrier);
            cb.End();
        }

    }
}
