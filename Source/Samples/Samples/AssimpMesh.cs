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
        public Matrix viewProj;
        public Vector4 lightPos;
        public Vector3 cameraPos;
        float pading1;
    }

    [SampleDesc(sortOrder = 2)]
    public unsafe class AssimpMesh : Sample
    {
        Texture2D colorMap = new Texture2D();

        Geometry geometry;
        DeviceBuffer uniformBufferScene = new DeviceBuffer();

        UboVS uboVS = new UboVS() { lightPos = new Vector4(0.0f, 1.0f, -5.0f, 1.0f) };

        Shader shader;
        ResourceLayout resourceLayout;
        GraphicsPipeline pipelineSolid;

        Vector3 rotation = new Vector3(-0.5f, 112.75f + 180, 0.0f);
     
        public AssimpMesh()
        {
        }

        public override void Init()
        {
            base.Init();

            var graphics = Graphics.Instance;
            scene = new Scene();

            var cameraNode = scene.CreateChild("Camera");
            cameraNode.Position = new Vector3(0.0f, 2.0f, -5);
            cameraNode.LookAt(Vector3.Zero);

            camera = cameraNode.CreateComponent<Camera>();
            camera.Fov = MathUtil.DegreesToRadians(60);
            camera.AspectRatio = (float)graphics.Width / graphics.Height;

            var node = scene.CreateChild("Mesh");
            var drawable = node.AddComponent<Drawable>();
            
            LoadMesh();
            CreatePipelines();
            CreateUniformBuffers();
          
            drawable.SetNumGeometries(1);
            drawable.SetGeometry(0, geometry);

            var mat = new Material
            {
                Shader = shader,
                Pipeline = pipelineSolid,
                ResourceSet = new ResourceSet(resourceLayout, uniformBufferScene, colorMap)
            };

            drawable.SetMaterial(0, mat);

            Renderer.Instance.MainView.Attach(camera, scene);
        }


        protected override void Destroy()
        {
            geometry.Dispose();
            colorMap.Dispose();
            uniformBufferScene.Dispose();
            pipelineSolid.Dispose();

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
                new Pass("shaders/mesh.vert.spv", "shaders/mesh.frag.spv")
            };

            pipelineSolid = new GraphicsPipeline
            {
                CullMode = CullMode.Back,
                FrontFace = FrontFace.CounterClockwise,

                ResourceLayout = new[]
                {
                    new ResourceLayout
                    {
                        new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex),
                    },

                    resourceLayout
                }
            };

        }

        void LoadMesh()
        {
            LoadModel(Application.DataPath + "models/voyager/voyager.dae");

            if (Device.Features.textureCompressionBC == 1)
            {
                colorMap.LoadFromFile("models/voyager/voyager_bc3_unorm.ktx",
                    Format.Bc3UnormBlock);
            }
            else if (Device.Features.textureCompressionASTC_LDR == 1)
            {
                colorMap.LoadFromFile("models/voyager/voyager_astc_8x8_unorm.ktx", Format.Astc8x8UnormBlock);
            }
            else if (Device.Features.textureCompressionETC2 == 1)
            {
                colorMap.LoadFromFile("models/voyager/voyager_etc2_unorm.ktx", Format.Etc2R8g8b8a8UnormBlock);
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
            NativeList<VertexPosNormTexColor> vertexBuffer = new NativeList<VertexPosNormTexColor>();

            // Iterate through all meshes in the file and extract the vertex components
            for (int m = 0; m < scene.MeshCount; m++)
            {
                for (int v = 0; v < scene.Meshes[(int)m].VertexCount; v++)
                {
                    VertexPosNormTexColor vertex;
                    Assimp.Mesh mesh = scene.Meshes[m];

                    // Use glm make_* functions to convert ASSIMP vectors to glm vectors
                    vertex.position = new Vector3(mesh.Vertices[v].X, mesh.Vertices[v].Y, mesh.Vertices[v].Z) * scale;
                    vertex.normal = new Vector3(mesh.Normals[v].X, mesh.Normals[v].Y, mesh.Normals[v].Z);
                    // Texture coordinates and colors may have multiple channels, we only use the first [0] one
                    vertex.texcoord = new Vector2(mesh.TextureCoordinateChannels[0][v].X, mesh.TextureCoordinateChannels[0][v].Y);
                    // Mesh may not have vertex colors
                    if (mesh.HasVertexColors(0))
                    {
                        vertex.color = new Color(mesh.VertexColorChannels[0][v].R,
                            mesh.VertexColorChannels[0][v].G,
                            mesh.VertexColorChannels[0][v].B);
                    }
                    else
                    {
                        vertex.color = new Color(1.0f);
                    }

                    vertexBuffer.Add(vertex);
                }
            }

            var vb = DeviceBuffer.Create(BufferUsage.VertexBuffer, false,
                sizeof(VertexPosNormTexColor), (int)vertexBuffer.Count, vertexBuffer.Data);
            
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

            var ib = DeviceBuffer.Create(BufferUsage.IndexBuffer, false, sizeof(uint), (int)indexBuffer.Count, indexBuffer.Data);
            
            geometry = new Geometry
            {
                VertexBuffers = new[] { vb },
                IndexBuffer = ib,
                VertexLayout = VertexPosNormTexColor.Layout
            };

            geometry.SetDrawRange(PrimitiveTopology.TriangleList, 0, ib.Count);
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
        }

        // Prepare and initialize uniform buffer containing shader uniforms
        void CreateUniformBuffers()
        {
            uniformBufferScene = DeviceBuffer.CreateUniformBuffer<UboVS>();
        }

        public override void Update()
        {
            base.Update();

            rotation.Y += Time.Delta * 10;

            uboVS.model = Matrix.RotationY(MathUtil.DegreesToRadians(rotation.Y));

            uboVS.viewProj = camera.View*camera.Projection;
            uboVS.cameraPos = camera.Node.Position;
            uniformBufferScene.SetData(ref uboVS);
        }


    }
}
