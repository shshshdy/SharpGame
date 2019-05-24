using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vulkan;
using static Vulkan.VulkanNative;

using ImGuiNET;

namespace SharpGame.Samples
{
    struct UboVS
    {
        public Matrix model;
        public Matrix view;
        public Matrix projection;
        public Vector4 lightPos;
    }

    [SampleDesc(sortOrder = 0)]
    public unsafe class AssimpMesh : Sample
    {
        private const uint VERTEX_BUFFER_BIND_ID = 0;
        bool wireframe = false;

        Texture2D colorMap = new Texture2D();
        VertexLayout vertexLayout;

        // Vertex layout used in this example
        // This must fit input locations of the vertex shader used to render the model
        struct Vertex
        {
            public Vector3 pos;
            public Vector3 normal;
            public Vector2 uv;
            public Vector3 color;
            public const uint PositionOffset = 0;
            public const uint NormalOffset = 12;
            public const uint UvOffset = 24;
            public const uint ColorOffset = 32;
        };

        Geometry geometry;
        GraphicsBuffer uniformBufferScene = new GraphicsBuffer();

        UboVS uboVS = new UboVS() { lightPos = new Vector4(25.0f, 5.0f, 5.0f, 1.0f) };

        Shader shader;
        ResourceLayout resourceLayout;
        ResourceSet resourceSet;

        Pipeline pipelineSolid;
        Pipeline pipelineWireframe;
        float zoom = -5.5f;
        float zoomSpeed = 20.5f;
        float rotationSpeed = 0.5f;
        Vector3 rotation = new Vector3(-0.5f, 112.75f + 180, 0.0f);
        Vector3 cameraPos = new Vector3(0.1f, 1.1f, 0.0f);
        public AssimpMesh()
        {
        }

        public override void Init()
        {
            base.Init();


            var graphics = Graphics.Instance;
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0.1f, 1.1f, -5);
            cameraNode.LookAt(Vector3.Zero);

            camera = cameraNode.CreateComponent<Camera>();
            camera.Fov = MathUtil.DegreesToRadians(60);
            camera.AspectRatio = (float)graphics.Width / graphics.Height;

            vertexLayout = new VertexLayout
            {
                bindings = new[]
                {
                    new VertexInputBinding(0, (uint)sizeof(Vertex), VertexInputRate.Vertex)
                },

                attributes = new[]
                {
                    new VertexInputAttribute(0, 0, Format.R32g32b32Sfloat, Vertex.PositionOffset),
                    new VertexInputAttribute(0, 1, Format.R32g32b32Sfloat, Vertex.NormalOffset),
                    new VertexInputAttribute(0, 2, Format.R32g32Sfloat, Vertex.UvOffset),
                    new VertexInputAttribute(0, 3, Format.R32g32b32Sfloat, Vertex.ColorOffset)
                }

            };

            LoadAssets();
            CreatePipelines();
            CreateUniformBuffers();
            SetupResourceSet();

            this.Subscribe<BeginRenderPass>(Handle);
         
        }


        protected override void Destroy()
        {
            geometry.Dispose();
            colorMap.Dispose();
            uniformBufferScene.Dispose();
            pipelineSolid.Dispose();
            pipelineWireframe.Dispose();

            base.Destroy();
        }

        void CreatePipelines()
        {
            resourceLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex),
                new ResourceLayoutBinding(1, DescriptorType.CombinedImageSampler, ShaderStage.Fragment)
            };

            shader = new Shader
            {
                new Pass("shaders/mesh/mesh.vert.spv", "shaders/mesh/mesh.frag.spv")
            };

            pipelineSolid = new Pipeline
            {
                CullMode = CullMode.Back,
                FrontFace = FrontFace.CounterClockwise,
                DynamicState = new DynamicStateInfo(DynamicState.Viewport, DynamicState.Scissor),
                VertexLayout = vertexLayout,
                ResourceLayout = new[]
                {
                    resourceLayout
                }
            };

            pipelineWireframe = new Pipeline
            {
                FillMode = PolygonMode.Line,
                CullMode = CullMode.Back,
                FrontFace = FrontFace.CounterClockwise,
                DynamicState = new DynamicStateInfo(DynamicState.Viewport, DynamicState.Scissor),
                VertexLayout = vertexLayout
            };
        }

        void LoadAssets()
        {
            LoadModel(Application.DataPath + "models/voyager/voyager.dae");

            if (Device.Features.textureCompressionBC == 1)
            {
                colorMap.LoadFromFile(Application.DataPath + "models/voyager/voyager_bc3_unorm.ktx",
                    VkFormat.Bc3UnormBlock);
            }
            else if (Device.Features.textureCompressionASTC_LDR == 1)
            {
                colorMap.LoadFromFile(Application.DataPath + "models/voyager/voyager_astc_8x8_unorm.ktx", VkFormat.Astc8x8UnormBlock);
            }
            else if (Device.Features.textureCompressionETC2 == 1)
            {
                colorMap.LoadFromFile(Application.DataPath + "models/voyager/voyager_etc2_unorm.ktx", VkFormat.Etc2R8g8b8a8UnormBlock);
            }
            else
            {
                throw new InvalidOperationException("Device does not support any compressed texture format!");
            }
        }

        // Load a model from file using the ASSIMP model loader and generate all resources required to render the model
        void LoadModel(string filename)
        {
            // Load the model from file using ASSIMP

            // Flags for loading the mesh
            Assimp.PostProcessSteps assimpFlags = Assimp.PostProcessSteps.FlipWindingOrder
                | Assimp.PostProcessSteps.Triangulate
                | Assimp.PostProcessSteps.PreTransformVertices;

            var scene = new Assimp.AssimpContext().ImportFile(filename, assimpFlags);

            // Generate vertex buffer from ASSIMP scene data
            float scale = 1.0f;
            NativeList<Vertex> vertexBuffer = new NativeList<Vertex>();

            // Iterate through all meshes in the file and extract the vertex components
            for (int m = 0; m < scene.MeshCount; m++)
            {
                for (int v = 0; v < scene.Meshes[(int)m].VertexCount; v++)
                {
                    Vertex vertex;
                    Assimp.Mesh mesh = scene.Meshes[m];

                    // Use glm make_* functions to convert ASSIMP vectors to glm vectors
                    vertex.pos = new Vector3(mesh.Vertices[v].X, mesh.Vertices[v].Y, mesh.Vertices[v].Z) * scale;
                    vertex.normal = new Vector3(mesh.Normals[v].X, mesh.Normals[v].Y, mesh.Normals[v].Z);
                    // Texture coordinates and colors may have multiple channels, we only use the first [0] one
                    vertex.uv = new Vector2(mesh.TextureCoordinateChannels[0][v].X, mesh.TextureCoordinateChannels[0][v].Y);
                    // Mesh may not have vertex colors
                    if (mesh.HasVertexColors(0))
                    {
                        vertex.color = new Vector3(mesh.VertexColorChannels[0][v].R, mesh.VertexColorChannels[0][v].G, mesh.VertexColorChannels[0][v].B);
                    }
                    else
                    {
                        vertex.color = new Vector3(1f);
                    }

                    // Vulkan uses a right-handed NDC (contrary to OpenGL), so simply flip Y-Axis
                    //vertex.pos.Y *= -1.0f;

                    vertexBuffer.Add(vertex);
                }
            }

            var vb = GraphicsBuffer.Create(BufferUsage.VertexBuffer, false,
                sizeof(Vertex), (int)vertexBuffer.Count, vertexBuffer.Data);

            ulong vertexBufferSize = (ulong)(vertexBuffer.Count * sizeof(Vertex));

            // Generate index buffer from ASSIMP scene data
            NativeList<uint> indexBuffer = new NativeList<uint>();
            for (int m = 0; m < scene.MeshCount; m++)
            {
                uint indexBase = indexBuffer.Count;
                for (int f = 0; f < scene.Meshes[m].FaceCount; f++)
                {
                    // We assume that all faces are triangulated
                    for (int i = 0; i < 3; i++)
                    {
                        indexBuffer.Add((uint)scene.Meshes[m].Faces[f].Indices[i] + indexBase);
                    }
                }
            }

            var ib = GraphicsBuffer.Create(BufferUsage.IndexBuffer, false, sizeof(uint), (int)indexBuffer.Count, indexBuffer.Data);

            geometry = new Geometry
            {
                VertexBuffers = new[] { vb },
                IndexBuffer = ib,
                VertexLayout = vertexLayout
            };

            geometry.SetDrawRange(PrimitiveTopology.TriangleList, 0, ib.Count);
        }

        void SetupResourceSet()
        {
            resourceSet = new ResourceSet(resourceLayout, uniformBufferScene, colorMap);
        }

        // Prepare and initialize uniform buffer containing shader uniforms
        void CreateUniformBuffers()
        {
            uniformBufferScene = GraphicsBuffer.CreateUniformBuffer<UboVS>();
        }
                
        void Handle(BeginRenderPass e)
        {
            var graphics = Graphics.Instance;
            var width = graphics.Width;
            var height = graphics.Height;
            var cmdBuffer = e.renderPass.CmdBuffer;

            rotation.Y += Time.Delta * 10;

            uboVS.model = Matrix.RotationYawPitchRoll(MathUtil.DegreesToRadians(rotation.Y),
                MathUtil.DegreesToRadians(rotation.X), MathUtil.DegreesToRadians(rotation.Z));// * uboVS.model;
        

            uboVS.projection = camera.Projection;
            uboVS.view = camera.View;
            uniformBufferScene.SetData(ref uboVS);
            
            var pipe = wireframe ? pipelineWireframe : pipelineSolid;
            cmdBuffer.DrawGeometry(geometry, pipe, shader.Main, resourceSet);
              
        }

    }
}
