﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class AssimpModelReader : ResourceReader<Model>
    {
        public Assimp.PostProcessSteps assimpFlags =
        Assimp.PostProcessSteps.CalculateTangentSpace |
        Assimp.PostProcessSteps.Triangulate |
        Assimp.PostProcessSteps.SortByPrimitiveType |
        Assimp.PostProcessSteps.PreTransformVertices |
        Assimp.PostProcessSteps.MakeLeftHanded |
        //Assimp.PostProcessSteps.GenerateNormals |
        Assimp.PostProcessSteps.GenerateSmoothNormals |
        Assimp.PostProcessSteps.GenerateUVCoords |
        Assimp.PostProcessSteps.OptimizeMeshes |
        Assimp.PostProcessSteps.Debone |
        Assimp.PostProcessSteps.ValidateDataStructure;

        // Generate vertex buffer from ASSIMP scene data
        public float scale = 1.0f;
        public bool combineVB = true;

        public VertexComponent[] vertexComponents = new[] {VertexComponent.Position, VertexComponent.Texcoord,
            VertexComponent.Normal, VertexComponent.Tangent, VertexComponent.Bitangent };

        static NativeList<float> vertexBuffer = new NativeList<float>(1024 * 1024);  
        static NativeList<uint> indexBuffer = new NativeList<uint>(1024 * 1024);     
        static int vertexOffset = 0;

        public AssimpModelReader() : base("")
        {
        }

        protected unsafe override bool OnLoad(Model model, File stream)
        {
            var filePath = FileUtil.StandardlizeFile(stream.Name);
            filePath = FileUtil.GetPath(stream.Name);
            FileSystem.AddResourceDir(filePath);
            FileSystem.AddResourceDir(filePath + "textures");

            var ctx = new Assimp.AssimpContext();
            string ext = FileUtil.GetExtension(loadingFile);
            if(!ctx.IsImportFormatSupported(ext))
            {
                ctx.Dispose();
                return false;
            }

            //Assimp.Scene scene = ctx.ImportFileFromStream(stream, assimpFlags, ext);
            Assimp.Scene scene = ctx.ImportFile(stream.Name, assimpFlags);

            BoundingBox boundingBox = new BoundingBox();

            string path = FileUtil.GetPath(loadingFile);

            model.VertexBuffers = new List<Buffer>();
            model.IndexBuffers = new List<Buffer>();
            
            vertexBuffer.Clear();
            indexBuffer.Clear(); 
            vertexOffset = 0;

            List<Geometry> geoList = new List<Geometry>();

            // Iterate through all meshes in the file and extract the vertex components
            for (int m = 0; m < scene.MeshCount; m++)
            {
                Assimp.Mesh mesh = scene.Meshes[m];
                if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex < scene.MaterialCount)
                {
                    Material mat = ConvertMaterial(path, scene.Materials[mesh.MaterialIndex], mesh.HasTangentBasis);
                    if(mat == null)
                    {
                        continue;
                    }

                    model.Materials.Add(mat);
                }
                else
                {
                    Log.Error("No material : " + mesh.Name);
                }

                var geometry = ConvertGeometry(mesh, scale, vertexComponents, combineVB, out var meshBoundingBox);
                geoList.Add(geometry);

                if (geometry.VertexBuffers != null)
                {
                    model.VertexBuffers.Add(geometry.VertexBuffers[0]);
                }

                model.IndexBuffers.Add(geometry.IndexBuffer);
                model.Geometries.Add(new [] { geometry });
                model.GeometryCenters.Add(meshBoundingBox.Center);

                boundingBox.Merge(meshBoundingBox);

            }

            if(combineVB)
            {
                var vb = Buffer.Create(BufferUsageFlags.VertexBuffer, false, sizeof(float), vertexBuffer.Count, vertexBuffer.Data);
                model.VertexBuffers.Add(vb);

                foreach (var geo in geoList)
                {
                    geo.VertexBuffers = new[] { vb };
                }
            }

            model.BoundingBox = boundingBox;
          
            ctx.Dispose();

            FileSystem.RemoveResourceDir(filePath);
            FileSystem.RemoveResourceDir(filePath + "textures");
            return true;
        }

        public static bool Import(string file, List<Geometry> geoList, List<BoundingBox> bboxList, VertexComponent[] vertexComponents = null, bool combineVB = false)
        {
            Assimp.PostProcessSteps assimpFlags =
            Assimp.PostProcessSteps.FlipWindingOrder |
            Assimp.PostProcessSteps.CalculateTangentSpace |
            Assimp.PostProcessSteps.Triangulate |
            Assimp.PostProcessSteps.SortByPrimitiveType |
            Assimp.PostProcessSteps.PreTransformVertices |
            //Assimp.PostProcessSteps.GenerateNormals |
            Assimp.PostProcessSteps.GenerateSmoothNormals|
            Assimp.PostProcessSteps.GenerateUVCoords |
            Assimp.PostProcessSteps.OptimizeMeshes |
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

            vertexBuffer.Clear();
            indexBuffer.Clear();
            vertexOffset = 0;
            // Iterate through all meshes in the file and extract the vertex components
            for (int m = 0; m < scene.MeshCount; m++)
            {
                Assimp.Mesh mesh = scene.Meshes[m];
                var geometry = ConvertGeometry(mesh, scale, vertexComponents, combineVB, out var meshBoundingBox);
                geoList.Add(geometry);
                bboxList.Add(meshBoundingBox);
            }

            if (combineVB)
            {
                var vb = Buffer.Create(BufferUsageFlags.VertexBuffer, false, sizeof(float), vertexBuffer.Count, vertexBuffer.Data);
                foreach (var geo in geoList)
                {
                    geo.VertexBuffers = new[] { vb };
                }
            }

            ctx.Dispose();
            return true;
        }

        private static Geometry ConvertGeometry(Assimp.Mesh mesh, float scale, VertexComponent[] vertexComponents, bool combineVB, out BoundingBox meshBoundingBox)
        {
            Buffer vb = null;
            Buffer ib = null;
            PrimitiveTopology[] primitiveTopology =
            {
                PrimitiveTopology.PointList,
                PrimitiveTopology.PointList,
                PrimitiveTopology.LineList,
                PrimitiveTopology.LineList,
                PrimitiveTopology.TriangleList,
            };
            
            indexBuffer.Clear();

            ConvertGeom(scale, mesh, out meshBoundingBox, vertexComponents);

            if(!combineVB)
            {
                vb = Buffer.Create(BufferUsageFlags.VertexBuffer, false, sizeof(float), vertexBuffer.Count, vertexBuffer.Data);               
            }

            ib = Buffer.Create(BufferUsageFlags.IndexBuffer, false, sizeof(uint), indexBuffer.Count, indexBuffer.Data);

            var geometry = new Geometry
            {
                Name = mesh.Name,
                IndexBuffer = ib,
                VertexLayout = new VertexLayout(vertexComponents)
            };

            if (!combineVB)
            {
                geometry.VertexBuffers = new[] { vb };
            }

            geometry.SetDrawRange(primitiveTopology[(int)mesh.PrimitiveType], 0, (uint)ib.Count, vertexOffset);

            if (combineVB)
            {
                vertexOffset += mesh.VertexCount;
            }
            return geometry;
        }

        static unsafe void ConvertGeom(float scale, Assimp.Mesh mesh, out BoundingBox meshBoundingBox, VertexComponent[] vertexComponents)
        {
            meshBoundingBox = new BoundingBox();

            for (int v = 0; v < mesh.VertexCount; v++)
            {
                foreach(var vc in vertexComponents)
                {
                    switch(vc)
                    {
                        case VertexComponent.Position:
                            var position = new vec3(mesh.Vertices[v].X, mesh.Vertices[v].Y, mesh.Vertices[v].Z) * scale;
                            vertexBuffer.Add(ref position.x, 3);
                            meshBoundingBox.Merge(position);
                            break;
                        case VertexComponent.Normal:
                            var normal = new vec3(mesh.Normals[v].X, mesh.Normals[v].Y, mesh.Normals[v].Z);
                            vertexBuffer.Add(ref normal.x, 3);
                            break;
                        case VertexComponent.Tangent:
                            var tan = new vec3(mesh.Tangents[v].X, mesh.Tangents[v].Y, mesh.Tangents[v].Z);
                            vertexBuffer.Add(ref tan.x, 3);
                            break;
                        case VertexComponent.Bitangent:
                            var bitan = new vec3(mesh.BiTangents[v].X, mesh.BiTangents[v].Y, mesh.BiTangents[v].Z);
                            vertexBuffer.Add(ref bitan.x, 3);
                            break;
                        case VertexComponent.Texcoord:
                            vec2 texcoord = vec2.Zero;
                            // Texture coordinates and colors may have multiple channels, we only use the first [0] one
                            if (mesh.HasTextureCoords(0))
                            {
                                texcoord = new vec2(mesh.TextureCoordinateChannels[0][v].X, mesh.TextureCoordinateChannels[0][v].Y);
                            }
                            vertexBuffer.Add(ref texcoord.x, 2);
                            break;
                        case VertexComponent.Color:
                            uint color = 0xffffffff;
                            if (mesh.VertexColorChannelCount > 0)
                            {
                                var c = mesh.VertexColorChannels[0][v];
                                color = (uint)(c.A*255) << 24 | (uint)(c.B * 255) << 16 | (uint)(c.G * 255) << 8 | (uint)(c.R * 255);
                            }
                            vertexBuffer.Add(Unsafe.AsPointer(ref color), 1);

                            break;
                    }

                }
            }

            for (int f = 0; f < mesh.FaceCount; f++)
            {
                for (int i = 0; i < 3; i++)
                {
                    indexBuffer.Add((uint)mesh.Faces[f].Indices[i]);
                }
            }


        }

        Material ConvertMaterial(string path, Assimp.Material aiMaterial, bool hasTangent)
        {
            Shader shader = null;
            BlendFlags blendType = BlendFlags.Solid;
            if (hasTangent)
            {
                if (aiMaterial.HasTextureOpacity)
                {
                    blendType = BlendFlags.AlphaTest;
                    shader = Resources.Instance.Load<Shader>("Shaders/LitAlphaTest.shader");
                }
                else if (aiMaterial.Opacity < 1)
                {
                    blendType = BlendFlags.AlphaBlend;
                    shader = Resources.Instance.Load<Shader>("Shaders/LitParticle.shader");
                }
                else
                {
                    shader = Resources.Instance.Load<Shader>("Shaders/LitSolid.shader");
                }

            }
            else
            {
                shader = Resources.Instance.Load<Shader>("Shaders/Basic.shader");
            }

            Material material = new Material(shader);
            material.BlendType = blendType;
            if (aiMaterial.HasTextureDiffuse)
            {
                string texPath = FileUtil.CombinePath(path, aiMaterial.TextureDiffuse.FilePath);
                Texture tex = Resources.Instance.Load<Texture>(texPath);
                if(tex != null)
                {
                    material.SetTexture("DiffMap", tex);
                }
                else
                {
                    int idx = texPath.LastIndexOfAny(new[] { '\\', '/' });
                    if(idx != -1)
                    {
                        texPath = texPath.Substring(idx + 1);
                    }

                    tex = Resources.Instance.Load<Texture>(texPath.Replace(".ktx", "_bc3_unorm.ktx"));
                    if (tex != null)
                    {
                        material.SetTexture("DiffMap", tex);
                    }
                }
            }
            else
            {
                if(aiMaterial.HasColorDiffuse)
                {
                    Color c = new Color(aiMaterial.ColorDiffuse[0], aiMaterial.ColorDiffuse[1], 
                        aiMaterial.ColorDiffuse[2], aiMaterial.ColorDiffuse[3]* aiMaterial.Opacity);
                    material.SetTexture("DiffMap", Texture.CreateByColor(c));
                    material.SetShaderParameter("diffuse", c);
                }
                else
                {
                    material.SetTexture("DiffMap", Texture.White);
                }

            }

            if (aiMaterial.HasTextureNormal)
            {
                string texPath = FileUtil.CombinePath(path, aiMaterial.TextureNormal.FilePath);
                Texture tex = Resources.Instance.Load<Texture>(texPath);
                if (tex != null)
                {
                    material.SetTexture("NormalMap", tex);
                }
                else
                {
                    tex = Resources.Instance.Load<Texture>(texPath.Replace(".ktx", "_bc3_unorm.ktx"));
                    if (tex != null)
                    {
                        material.SetTexture("NormalMap", tex);
                    }
                }
            }
            else
            {
                material.SetTexture("NormalMap", Texture.Blue);
            }

            if (aiMaterial.HasTextureSpecular)
            {
                string texPath = FileUtil.CombinePath(path, aiMaterial.TextureSpecular.FilePath);
                Texture tex = Resources.Instance.Load<Texture>(texPath);
                if (tex != null)
                {
                    material.SetTexture("SpecMap", tex);
                }
                else
                {
                    tex = Resources.Instance.Load<Texture>(texPath.Replace(".ktx", "_bc3_unorm.ktx"));
                    if (tex != null)
                    {
                        material.SetTexture("SpecMap", tex);
                    }
                }
            }
            else
            {
                if (aiMaterial.HasColorSpecular)
                {
                    Color c = new Color(aiMaterial.ColorSpecular[0], aiMaterial.ColorSpecular[1],
                        aiMaterial.ColorSpecular[2], aiMaterial.Shininess/ 255.0f);
                    material.SetTexture("SpecMap", Texture.CreateByColor(c));

                    material.SetShaderParameter("specular", c);
                }
                else
                {
                    material.SetTexture("SpecMap", Texture.Black);
                }

            }

            return material;
        }

    }
}
