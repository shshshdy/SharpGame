using System;

namespace SharpGame.Samples
{
    public struct Particle
    {
        public vec2 pos;                              // Particle position
        public vec2 vel;                              // Particle velocity
        public vec4 gradientPos;                      // Texture coordiantes for the gradient ramp map
    };

    [SampleDesc(sortOrder = -8)]
    public class ComputeShader : Sample
    {
        const int PARTICLE_COUNT = 256 * 1024;

        private RenderPipeline frameGraph = new RenderPipeline();
        private Buffer storageBuffer;
        private Buffer uniformBuffer;

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
                particle.pos = rand.NextVector2(new vec2(-1, -1), new vec2(1, 1));
                particle.vel = new vec2(0.0f);
                particle.gradientPos = new vec4(particle.pos.X / 2.0f, 0, 0, 0);
            }

            storageBuffer = Buffer.Create(BufferUsageFlags.VertexBuffer | BufferUsageFlags.StorageBuffer, particles, true);
            uniformBuffer = Buffer.CreateUniformBuffer<UBO>();

            geometry = new Geometry
            {
                VertexBuffers = new [] { storageBuffer },

                VertexLayout = new VertexLayout
                (
                    new VertexAttribute(0, 0, Format.R32g32Sfloat, 0),
                    //new VertexInputAttribute(0, 1, Format.R32g32Sfloat, 8),
                    new VertexAttribute(0, 1, Format.R32g32b32a32Sfloat, 16)
                    
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

            FrameGraph.AddDebugImage(tex);
            FrameGraph.AddDebugImage(tex1);

            material.PipelineResourceSet[0].ResourceSet[0].Bind(tex, tex1);

            computePipeline = shader.GetPass("compute");
            computeResourceSet = new ResourceSet(computePipeline.PipelineLayout.ResourceLayout[0], storageBuffer, uniformBuffer);

            frameGraph.AddComputePass(Docompute);
            frameGraph.AddGraphicsPass(DrawQuad);

            MainView.Attach(null, null, frameGraph);

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
                ubo.destX = (float)Math.Sin(glm.radians(timer * 360.0f)) * 0.75f;
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

        private void DrawQuad(GraphicsSubpass renderPass, CommandBuffer cmd)
        {
            var shader = material.Shader;

            cmd.DrawGeometry(geometry, shader.Main, 0, material);
        }

        private void Docompute(ComputePass renderPass, CommandBuffer cb)
        {
            // Record particle movements.
            cb.BindComputePipeline(computePipeline);
            cb.BindComputeResourceSet(computePipeline.PipelineLayout, 0, computeResourceSet);
            cb.Dispatch((uint)storageBuffer.Count / 256, 1, 1);        
        }

    }
}
