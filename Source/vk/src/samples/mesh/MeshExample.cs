using Assimp;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vulkan;
using Veldrid.Sdl2;
using static Vulkan.VulkanNative;
using Veldrid;
using ImGuiNET;

namespace SharpGame
{
    struct UboVS
    {
        public System.Numerics.Matrix4x4 projection;
        public System.Numerics.Matrix4x4 model;
        public Vector4 lightPos;
    }

    public unsafe class MeshExample : SampleApp
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
        
        public MeshExample()
        {
            zoom = -5.5f;
            zoomSpeed = 20.5f;
            rotationSpeed = 0.5f;
            rotation = new Vector3(-0.5f, -112.75f, 0.0f);
            cameraPos = new Vector3(0.1f, 1.1f, 0.0f);
            Title = "Vulkan Example - Model rendering";
        }

        public override void Init()
        {
            base.Init();

            LoadAssets();
            CreatePipelines();
            CreateUniformBuffers();
            SetupResourceSet();

            this.SubscribeToEvent<BeginRender>(Handle);

            this.SubscribeToEvent<GUIEvent>(Handle);
            prepared = true;
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

            resourceLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex),
                new ResourceLayoutBinding(1, DescriptorType.CombinedImageSampler, ShaderStage.Fragment)
            };

            shader = new Shader
            {
                new Pass("shaders/mesh/mesh.vert.spv", "shaders/mesh/mesh.frag.spv")
                {
                    ResourceLayout = resourceLayout
                }
            };

            pipelineSolid = new Pipeline
            {
                CullMode = CullMode.Back,
                FrontFace = FrontFace.Clockwise,
                DynamicState = new DynamicStateInfo(DynamicState.Viewport, DynamicState.Scissor),
                VertexLayout = vertexLayout
            };

            pipelineWireframe = new Pipeline
            {
                FillMode = PolygonMode.Line,
                CullMode = CullMode.Back,
                FrontFace = FrontFace.Clockwise,
                DynamicState = new DynamicStateInfo(DynamicState.Viewport, DynamicState.Scissor),
                VertexLayout = vertexLayout
            };
        }

        void LoadAssets()
        {
            LoadModel(DataPath + "models/voyager/voyager.dae");

            if (Device.Features.textureCompressionBC == 1)
            {
                colorMap.loadFromFile(DataPath + "models/voyager/voyager_bc3_unorm.ktx",
                    VkFormat.Bc3UnormBlock, Graphics.queue);
            }
            else if (Device.Features.textureCompressionASTC_LDR == 1)
            {
                colorMap.loadFromFile(DataPath + "models/voyager/voyager_astc_8x8_unorm.ktx", VkFormat.Astc8x8UnormBlock, Graphics.queue);
            }
            else if (Device.Features.textureCompressionETC2 == 1)
            {
                colorMap.loadFromFile(DataPath + "models/voyager/voyager_etc2_unorm.ktx", VkFormat.Etc2R8g8b8a8UnormBlock, Graphics.queue);
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
            PostProcessSteps assimpFlags = PostProcessSteps.FlipWindingOrder | PostProcessSteps.Triangulate | PostProcessSteps.PreTransformVertices;

            var scene = new AssimpContext().ImportFile(filename, assimpFlags);

            // Generate vertex buffer from ASSIMP scene data
            float scale = 1.0f;
            NativeList<Vertex> vertexBuffer = new NativeList<Vertex>();

            // Iterate through all meshes in the file and extract the vertex components
            for (int m = 0; m < scene.MeshCount; m++)
            {
                for (int v = 0; v < scene.Meshes[(int)m].VertexCount; v++)
                {
                    Vertex vertex;
                    Mesh mesh = scene.Meshes[m];

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
                    vertex.pos.Y *= -1.0f;

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
            UpdateUniformBuffers();
        }

        void UpdateUniformBuffers()
        {
            uboVS.projection = System.Numerics.Matrix4x4.CreatePerspectiveFieldOfView(Util.DegreesToRadians(60.0f), (float)width / (float)height, 0.1f, 256.0f);
            System.Numerics.Matrix4x4 viewMatrix = System.Numerics.Matrix4x4.CreateTranslation(0.0f, 0.0f, zoom);

            uboVS.model = viewMatrix * System.Numerics.Matrix4x4.CreateTranslation((System.Numerics.Vector3)cameraPos);
            uboVS.model = System.Numerics.Matrix4x4.CreateRotationX(Util.DegreesToRadians(rotation.X)) * uboVS.model;
            uboVS.model = System.Numerics.Matrix4x4.CreateRotationY(Util.DegreesToRadians(rotation.Y)) * uboVS.model;
            uboVS.model = System.Numerics.Matrix4x4.CreateRotationZ(Util.DegreesToRadians(rotation.Z)) * uboVS.model;

            uniformBufferScene.SetData(ref uboVS);
        }
        
        protected override void Render()
        {
            if (!prepared)
                return;

            UpdateUniformBuffers();

            graphics.BeginRender();

            BuildCommandBuffers();

            graphics.EndRender();
        }

        void Handle(BeginRender e)
        {
            UpdateUniformBuffers();

            var graphics = Graphics.Instance;
            var cmdBuffer = Graphics.Instance.RenderCmdBuffer;

            FixedArray2<VkClearValue> clearValues = new FixedArray2<VkClearValue>();
            clearValues.First.color = defaultClearColor;
            clearValues.Second.depthStencil = new VkClearDepthStencilValue() { depth = 1.0f, stencil = 0 };

            var renderPassBeginInfo = VkRenderPassBeginInfo.New();
            renderPassBeginInfo.renderPass = Graphics.RenderPass;
            renderPassBeginInfo.renderArea.offset.x = 0;
            renderPassBeginInfo.renderArea.offset.y = 0;
            renderPassBeginInfo.renderArea.extent.width = (uint)width;
            renderPassBeginInfo.renderArea.extent.height = (uint)height;
            renderPassBeginInfo.clearValueCount = 2;
            renderPassBeginInfo.pClearValues = &clearValues.First;

            // Set target frame buffer
            renderPassBeginInfo.framebuffer = Graphics.FrameBuffers[graphics.currentBuffer];

            cmdBuffer.BeginRenderPass(ref renderPassBeginInfo, VkSubpassContents.Inline);

            cmdBuffer.SetViewport(new Viewport(0, 0, width, height, 0.0f, 1.0f));
            cmdBuffer.SetScissor(new Rect2D(0, 0, width, height));

            var pipe = wireframe ? pipelineWireframe : pipelineSolid;
            cmdBuffer.DrawGeometry(geometry, pipe, shader.Main, resourceSet);

            cmdBuffer.EndRenderPass();
              
        }

        void Handle(GUIEvent e)
        {
            ImGui.ShowDemoWindow();
        }

        protected override void KeyPressed(Key keyCode)
        {
            switch (keyCode)
            {
                case  Key.W:
                    if (Device.Features.fillModeNonSolid == 1)
                    {
                        wireframe = !wireframe;
                    }
                    break;
            }
        }

        public static void Main() => new MeshExample().Run();
    }
}
