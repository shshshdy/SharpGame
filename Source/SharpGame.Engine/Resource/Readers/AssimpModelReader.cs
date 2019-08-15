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
            Assimp.PostProcessSteps assimpFlags = //Assimp.PostProcessSteps.FlipWindingOrder
           //| Assimp.PostProcessSteps.Triangulate
           //| Assimp.PostProcessSteps.PreTransformVertices;
            Assimp.PostProcessSteps.CalculateTangentSpace |
Assimp.PostProcessSteps.Triangulate |
Assimp.PostProcessSteps.SortByPrimitiveType |
Assimp.PostProcessSteps.PreTransformVertices |
Assimp.PostProcessSteps.GenerateNormals |
Assimp.PostProcessSteps.GenerateUVCoords |
Assimp.PostProcessSteps.OptimizeMeshes |
Assimp.PostProcessSteps.Debone |
Assimp.PostProcessSteps.ValidateDataStructure;

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

            var shader = Resources.Instance.Load<Shader>("Shaders/Basic.shader");
            string path = FileUtil.GetPath(loadingFile);

            // Iterate through all meshes in the file and extract the vertex components
            for (int m = 0; m < scene.MeshCount; m++)
            {
                Assimp.Mesh mesh = scene.Meshes[m];
                Geometry geometry;
                BoundingBox meshBoundingBox;
                DeviceBuffer vb;
                DeviceBuffer ib;
                VertexLayout vertexLayout;
                if (mesh.HasTangentBasis)
                {
                    ConvertGeomNTB(scale, mesh, out meshBoundingBox, out vb, out ib, out vertexLayout);
                }
                else
                {
                    ConvertGeom(scale, mesh, out meshBoundingBox, out vb, out ib, out vertexLayout);
                }

                model.VertexBuffers[m] = vb;
                model.IndexBuffers[m] = ib;

                geometry = new Geometry
                {
                    Name = mesh.Name,
                    VertexBuffers = new[] { vb },
                    IndexBuffer = ib,
                    VertexLayout = vertexLayout
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

        private static unsafe void ConvertGeom(float scale, Assimp.Mesh mesh,
            out BoundingBox meshBoundingBox, out DeviceBuffer vb, out DeviceBuffer ib, out VertexLayout vertexLayout)
        {
            NativeList<VertexPosNormTex> vertexBuffer = new NativeList<VertexPosNormTex>();
            NativeList<uint> indexBuffer = new NativeList<uint>();

            Log.Info("Geom type : " + typeof(VertexPosNormTex));

            meshBoundingBox = new BoundingBox();

            vertexLayout = VertexPosNormTex.Layout;

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

                vertexBuffer.Add(vertex);

                meshBoundingBox.Merge(ref vertex.position);
            }

            for (int f = 0; f < mesh.FaceCount; f++)
            {
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

        private static unsafe void ConvertGeomNTB(float scale, Assimp.Mesh mesh,
            out BoundingBox meshBoundingBox, out DeviceBuffer vb, out DeviceBuffer ib, out VertexLayout vertexLayout)
        {
            NativeList<VertexPosTBNTex> vertexBuffer = new NativeList<VertexPosTBNTex>();
            NativeList<uint> indexBuffer = new NativeList<uint>();

            Log.Info("Geom type : " + typeof(VertexPosTBNTex));

            meshBoundingBox = new BoundingBox();

            vertexLayout = VertexPosTBNTex.Layout;

            for (int v = 0; v < mesh.VertexCount; v++)
            {
                VertexPosTBNTex vertex;

                vertex.position = new Vector3(mesh.Vertices[v].X, mesh.Vertices[v].Y, mesh.Vertices[v].Z) * scale;
                vertex.normal = new Vector3(mesh.Normals[v].X, mesh.Normals[v].Y, mesh.Normals[v].Z);
                // Texture coordinates and colors may have multiple channels, we only use the first [0] one
                if (mesh.HasTextureCoords(0))
                {
                    vertex.texcoord = new Vector2(mesh.TextureCoordinateChannels[0][v].X, mesh.TextureCoordinateChannels[0][v].Y);
                }
                else
                {
                    vertex.texcoord = Vector2.Zero;
                }

                vertex.tangent = new Vector3(mesh.Tangents[v].X, mesh.Tangents[v].Y, mesh.Tangents[v].Z);
                vertex.bitangent = new Vector3(mesh.BiTangents[v].X, mesh.BiTangents[v].Y, mesh.BiTangents[v].Z);

                vertexBuffer.Add(vertex);
                meshBoundingBox.Merge(ref vertex.position);
            }

            for (int f = 0; f < mesh.FaceCount; f++)
            {
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
                Texture tex = Resources.Instance.Load<Texture2D>(path + aiMaterial.TextureDiffuse.FilePath);
                if(tex != null)
                {
                    material.SetTexture("DiffMap", tex.ResourceRef);
                }
            }
            else
            {
                material.SetTexture("DiffMap", Texture2D.White);
            }
            return material;
        }

    }
}
