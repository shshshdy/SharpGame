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
            var filePath = FileUtil.StandardlizeFile(stream.Name);
            filePath = FileUtil.GetPath(stream.Name);
            FileSystem.AddResourceDir(filePath + "textures");

            Assimp.PostProcessSteps assimpFlags = 
            Assimp.PostProcessSteps.CalculateTangentSpace |
            Assimp.PostProcessSteps.Triangulate |
            Assimp.PostProcessSteps.SortByPrimitiveType |
            Assimp.PostProcessSteps.PreTransformVertices |
            Assimp.PostProcessSteps.MakeLeftHanded |
            //Assimp.PostProcessSteps.GenerateNormals |
            //Assimp.PostProcessSteps.GenerateUVCoords |
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

            BoundingBox boundingBox = new BoundingBox();
            var shader = Resources.Instance.Load<Shader>("Shaders/Basic.shader");
            var shader1 = Resources.Instance.Load<Shader>("Shaders/Litsolid.shader");

            string path = FileUtil.GetPath(loadingFile);

            // Iterate through all meshes in the file and extract the vertex components
            for (int m = 0; m < scene.MeshCount; m++)
            {
                Assimp.Mesh mesh = scene.Meshes[m];
                ConvertGeometry(mesh, scale, out Geometry geometry, out var meshBoundingBox);

                model.VertexBuffers[m] = geometry.VertexBuffers[0];
                model.IndexBuffers[m] = geometry.IndexBuffer;
                model.Geometries[m] = new Geometry[] { geometry };
                model.GeometryCenters[m] = meshBoundingBox.Center;

                boundingBox.Merge(ref meshBoundingBox);

                if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex < scene.MaterialCount)
                {
                    Material mat = ConvertMaterial(path, scene.Materials[mesh.MaterialIndex], mesh.HasTangentBasis ? shader1 : shader);
                    model.Materials.Add(mat);
                }
                else
                {
                    Log.Error("No material : " + mesh.Name);
                }

            }

            model.BoundingBox = boundingBox;
          
            ctx.Dispose();


            FileSystem.RemoveResourceDir(filePath);
            FileSystem.RemoveResourceDir(filePath + "textures");
            return true;
        }

        public static bool Import(string file, List<Geometry> geoList, List<BoundingBox> bboxList)
        {
            Assimp.PostProcessSteps assimpFlags =
            Assimp.PostProcessSteps.FlipWindingOrder |
            Assimp.PostProcessSteps.CalculateTangentSpace |
            Assimp.PostProcessSteps.Triangulate |
            Assimp.PostProcessSteps.SortByPrimitiveType |
            Assimp.PostProcessSteps.PreTransformVertices |
            //Assimp.PostProcessSteps.GenerateNormals |
            Assimp.PostProcessSteps.GenerateSmoothNormals|
            //Assimp.PostProcessSteps.GenerateUVCoords |
            //Assimp.PostProcessSteps.OptimizeMeshes |
            Assimp.PostProcessSteps.Debone |
            Assimp.PostProcessSteps.ValidateDataStructure;

            var ctx = new Assimp.AssimpContext();
            string ext = FileUtil.GetExtension(file);
            if (!ctx.IsImportFormatSupported(ext))
            {
                ctx.Dispose();
                return false;
            }

            File stream = FileSystem.Instance.GetFile(file);

            Assimp.Scene scene = ctx.ImportFileFromStream(stream, assimpFlags);
            float scale = 1.0f;
            string path = FileUtil.GetPath(file);
            // Iterate through all meshes in the file and extract the vertex components
            for (int m = 0; m < scene.MeshCount; m++)
            {
                Assimp.Mesh mesh = scene.Meshes[m];
                ConvertGeometry(mesh, scale, out Geometry geometry, out var meshBoundingBox);
                geoList.Add(geometry);
                bboxList.Add(meshBoundingBox);
            }

            ctx.Dispose();
            return true;
        }

        private static void ConvertGeometry(Assimp.Mesh mesh, float scale, out Geometry geometry, out BoundingBox meshBoundingBox)
        {
            VertexLayout vertexLayout = null;
            DeviceBuffer vb;
            DeviceBuffer ib;
            PrimitiveTopology[] primitiveTopology =
            {
                PrimitiveTopology.PointList,
                PrimitiveTopology.PointList,
                PrimitiveTopology.LineList,
                PrimitiveTopology.LineList,
                PrimitiveTopology.TriangleList,
            };

            bool hasTangent = mesh.HasTangentBasis;
            if (hasTangent)
            {
                ConvertGeomNTB(scale, mesh, out meshBoundingBox, out vb, out ib, out vertexLayout);
            }
            else
            {
                ConvertGeom(scale, mesh, out meshBoundingBox, out vb, out ib, out vertexLayout);
            }

            geometry = new Geometry
            {
                Name = mesh.Name,
                VertexBuffers = new[] { vb },
                IndexBuffer = ib,
                VertexLayout = vertexLayout
            };

            geometry.SetDrawRange(primitiveTopology[(int)mesh.PrimitiveType], 0, (uint)ib.Count);
            
        }

        private static unsafe void ConvertGeom(float scale, Assimp.Mesh mesh,
            out BoundingBox meshBoundingBox, out DeviceBuffer vb, out DeviceBuffer ib, out VertexLayout vertexLayout)
        {
            NativeList<VertexPosTexNorm> vertexBuffer = new NativeList<VertexPosTexNorm>();
            NativeList<uint> indexBuffer = new NativeList<uint>();
            
            meshBoundingBox = new BoundingBox();

            vertexLayout = VertexPosTexNorm.Layout;

            for (int v = 0; v < mesh.VertexCount; v++)
            {
                VertexPosTexNorm vertex;

                vertex.position = new vec3(mesh.Vertices[v].X, mesh.Vertices[v].Y, mesh.Vertices[v].Z) * scale;
                vertex.normal = new vec3(mesh.Normals[v].X, mesh.Normals[v].Y, mesh.Normals[v].Z);
                // Texture coordinates and colors may have multiple channels, we only use the first [0] one
                if(mesh.HasTextureCoords(0))
                {
                    vertex.texcoord = new vec2(mesh.TextureCoordinateChannels[0][v].X, mesh.TextureCoordinateChannels[0][v].Y);
                }
                else
                {
                    vertex.texcoord = vec2.Zero;
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

            vertexLayout.Print();
                
            vb = DeviceBuffer.Create(BufferUsageFlags.VertexBuffer, false, (uint)sizeof(VertexPosTexNorm), vertexBuffer.Count, vertexBuffer.Data);
            ib = DeviceBuffer.Create(BufferUsageFlags.IndexBuffer, false, sizeof(uint), indexBuffer.Count, indexBuffer.Data);

            vertexBuffer.Dispose();
            indexBuffer.Dispose();
        }

        private static unsafe void ConvertGeomNTB(float scale, Assimp.Mesh mesh,
            out BoundingBox meshBoundingBox, out DeviceBuffer vb, out DeviceBuffer ib, out VertexLayout vertexLayout)
        {
            NativeList<VertexPosTexNTB> vertexBuffer = new NativeList<VertexPosTexNTB>();
            NativeList<uint> indexBuffer = new NativeList<uint>();

            meshBoundingBox = new BoundingBox();

            vertexLayout = VertexPosTexNTB.Layout;

            for (int v = 0; v < mesh.VertexCount; v++)
            {
                VertexPosTexNTB vertex;

                vertex.position = new vec3(mesh.Vertices[v].X, mesh.Vertices[v].Y, mesh.Vertices[v].Z) * scale;
                vertex.normal = new vec3(mesh.Normals[v].X, mesh.Normals[v].Y, mesh.Normals[v].Z);
                vertex.tangent = new vec3(mesh.Tangents[v].X, mesh.Tangents[v].Y, mesh.Tangents[v].Z);//, 1);
                vertex.bitangent = new vec3(mesh.BiTangents[v].X, mesh.BiTangents[v].Y, mesh.BiTangents[v].Z);

                // Texture coordinates and colors may have multiple channels, we only use the first [0] one
                if (mesh.HasTextureCoords(0))
                {
                    vertex.texcoord = new vec2(mesh.TextureCoordinateChannels[0][v].X, mesh.TextureCoordinateChannels[0][v].Y);
                }
                else
                {
                    vertex.texcoord = vec2.Zero;
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

            vertexLayout.Print();

            vb = DeviceBuffer.Create(BufferUsageFlags.VertexBuffer, false, (uint)sizeof(VertexPosTexNTB), vertexBuffer.Count, vertexBuffer.Data);
            ib = DeviceBuffer.Create(BufferUsageFlags.IndexBuffer, false, sizeof(uint), indexBuffer.Count, indexBuffer.Data);

            vertexBuffer.Dispose();
            indexBuffer.Dispose();
        }

        Material ConvertMaterial(string path, Assimp.Material aiMaterial, Shader shader)
        {
            Material material = new Material(shader);

            if (aiMaterial.HasTextureDiffuse)
            {
                string texPath = FileUtil.CombinePath(path, aiMaterial.TextureDiffuse.FilePath);
                Texture tex = Resources.Instance.Load<Texture>(texPath);
                if(tex != null)
                {
                    material.SetTexture("DiffMap", tex.ResourceRef);
                }
                else
                {                   
                    tex = Resources.Instance.Load<Texture>(texPath.Replace(".ktx", "_bc3_unorm.ktx"));
                    if (tex != null)
                    {
                        material.SetTexture("DiffMap", tex.ResourceRef);
                    }
                }
            }
            else
            {
                material.SetTexture("DiffMap", Texture.White);
            }

            if (aiMaterial.HasTextureNormal)
            {
                string texPath = FileUtil.CombinePath(path, aiMaterial.TextureNormal.FilePath);
                Texture tex = Resources.Instance.Load<Texture>(texPath);
                if (tex != null)
                {
                    material.SetTexture("NormalMap", tex.ResourceRef);
                }
                else
                {
                    tex = Resources.Instance.Load<Texture>(texPath.Replace(".ktx", "_bc3_unorm.ktx"));
                    if (tex != null)
                    {
                        material.SetTexture("DiffMap", tex.ResourceRef);
                    }
                }
            }
            else
            {
                material.SetTexture("NormalMap", Texture.Blue);
            }


            return material;
        }

    }
}
