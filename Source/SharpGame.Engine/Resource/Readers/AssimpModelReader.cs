using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class AssimpModelReader : ResourceReader<Model>
    {
        public AssimpModelReader() : base("")
        {
        }

        protected unsafe override bool OnLoad(Model model, File stream)
        {
            Assimp.PostProcessSteps assimpFlags = Assimp.PostProcessSteps.FlipWindingOrder
           | Assimp.PostProcessSteps.Triangulate
           | Assimp.PostProcessSteps.PreTransformVertices;

            var ctx = new Assimp.AssimpContext();
            string ext = FileUtil.GetExtension(loadingFile);
            if(!ctx.IsImportFormatSupported(ext))
            {
                ctx.Dispose();
                return false;
            }

            //Assimp.Scene scene = ctx.ImportFileFromStream(stream, assimpFlags, ext);
            Assimp.Scene scene = ctx.ImportFile(stream.Name, assimpFlags);

            // Generate vertex buffer from ASSIMP scene data
            float scale = 1.0f;

            model.VertexBuffers = new DeviceBuffer[scene.MeshCount];
            model.IndexBuffers = new DeviceBuffer[scene.MeshCount];
            model.SetNumGeometry(scene.MeshCount);

            PrimitiveTopology[] primitiveTopology =
            {
                PrimitiveTopology.PointList,
                PrimitiveTopology.PointList,
                PrimitiveTopology.LineList,
                PrimitiveTopology.LineList,
                PrimitiveTopology.TriangleList,                
            };

            BoundingBox boundingBox = new BoundingBox();

            var resourceLayout = new ResourceLayout(0)
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
            };

            var resourceLayoutTex = new ResourceLayout(1)
            {
                new ResourceLayoutBinding(0, DescriptorType.CombinedImageSampler, ShaderStage.Fragment, 1)
            };


            var shader = new Shader
            {
                new Pass("shaders/Textured.vert.spv", "shaders/Textured.frag.spv")
                {
                    CullMode = CullMode.Back,
                    FrontFace = FrontFace.CounterClockwise,
                    ResourceLayout = new[] { resourceLayout, resourceLayoutTex },
                    PushConstant = new[]
                    {
                        new PushConstantRange(ShaderStage.Vertex, 0, Utilities.SizeOf<Matrix>())
                    }
                }
            };
            
            string path = FileUtil.GetPath(loadingFile);

            // Iterate through all meshes in the file and extract the vertex components
            for (int m = 0; m < scene.MeshCount; m++)
            {
                Assimp.Mesh mesh = scene.Meshes[m];

                ConvertGeom(scale, mesh, out BoundingBox meshBoundingBox, out DeviceBuffer vb, out DeviceBuffer ib);
                model.VertexBuffers[m] = vb;
                model.IndexBuffers[m] = ib;

                var geometry = new Geometry
                {
                    Name = mesh.Name,
                    VertexBuffers = new[] { vb },
                    IndexBuffer = ib,
                    VertexLayout = VertexPosNormTex.Layout
                };

                geometry.SetDrawRange(primitiveTopology[(int)mesh.PrimitiveType], 0, (uint)ib.Count);
                model.Geometries[m] = new Geometry[] { geometry };
                model.GeometryCenters[m] = meshBoundingBox.Center;


                boundingBox.Merge(ref meshBoundingBox);

                if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex < scene.MaterialCount)
                {
                    Material mat = ConvertMaterial(path, scene.Materials[mesh.MaterialIndex], shader);
                    model.Materials.Add(mat);
                }
                else
                {
                    Log.Error("No material : " + mesh.Name);
                }

            }

            model.BoundingBox = boundingBox;

            ctx.Dispose();
            return true;
        }

        private static unsafe void ConvertGeom(float scale, Assimp.Mesh mesh, out BoundingBox meshBoundingBox, out DeviceBuffer vb, out DeviceBuffer ib)
        {
            NativeList<VertexPosNormTex> vertexBuffer = new NativeList<VertexPosNormTex>();
            NativeList<uint> indexBuffer = new NativeList<uint>();

            meshBoundingBox = new BoundingBox();
            for (int v = 0; v < mesh.VertexCount; v++)
            {
                VertexPosNormTex vertex;

                vertex.position = new Vector3(mesh.Vertices[v].X, mesh.Vertices[v].Y, mesh.Vertices[v].Z) * scale;
                vertex.normal = new Vector3(mesh.Normals[v].X, mesh.Normals[v].Y, mesh.Normals[v].Z);
                // Texture coordinates and colors may have multiple channels, we only use the first [0] one
                if(mesh.HasTextureCoords(0))
                {
                    vertex.texcoord = new Vector2(mesh.TextureCoordinateChannels[0][v].X, mesh.TextureCoordinateChannels[0][v].Y);
                }
                else
                {
                    vertex.texcoord = Vector2.Zero;
                }
                /*
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
                }*/

                vertexBuffer.Add(vertex);

                meshBoundingBox.Merge(ref vertex.position);
            }

            for (int f = 0; f < mesh.FaceCount; f++)
            {
                // We assume that all faces are triangulated
                for (int i = 0; i < 3; i++)
                {
                    indexBuffer.Add((uint)mesh.Faces[f].Indices[i]);
                }
            }


            vb = DeviceBuffer.Create(BufferUsageFlags.VertexBuffer, false, (uint)sizeof(VertexPosNormTex), vertexBuffer.Count, vertexBuffer.Data);
            ib = DeviceBuffer.Create(BufferUsageFlags.IndexBuffer, false, sizeof(uint), indexBuffer.Count, indexBuffer.Data);


            vertexBuffer.Dispose();
            indexBuffer.Dispose();
        }

        Material ConvertMaterial(string path, Assimp.Material aiMaterial, Shader shader)
        {
            Material material = new Material(shader);

            if (aiMaterial.HasTextureDiffuse)
            {
                Texture tex = Resources.Instance.Load<Texture>(path + aiMaterial.TextureDiffuse.FilePath);
                material.SetTexture("DiffMap", tex.ResourceRef);
                //test
                material.ResourceSet.Bind(0, tex).UpdateSets();
            }
            else
            {
                material.ResourceSet.Bind(0, Texture.White).UpdateSets();
            }
            return material;
        }

    }
}
