using System;
using System.Collections.Generic;
using System.Text;
using static SharpGame.ObjFile;

namespace SharpGame
{
    public class ObjModelReader : ResourceReader<Model>
    {
        public ObjModelReader() : base(".obj")
        {
        }

        public override Resource Load(string name)
        {
            if (!MatchExtension(name))
            {
                return null;
            }

            File stream = FileSystem.GetFile(name);

            var objParser = new ObjParser();
            ObjFile objFile = objParser.Parse(stream);

            Dictionary<FaceVertex, uint> vertexMap = new Dictionary<FaceVertex, uint>();
            int vertexCount = Math.Max(objFile.Positions.Length, objFile.Normals.Length);
            vertexCount = Math.Max(vertexCount, objFile.TexCoords.Length);

            List<DeviceBuffer> ibs = new List<DeviceBuffer>();
            List<MeshGroup> meshGroups = new List<MeshGroup>();
            List<VertexPosNormTex> vertices = new List<VertexPosNormTex>();
  
            foreach(MeshGroup group in objFile.MeshGroups)
            {
                int indexCount = group.Faces.Length * 3;
                if(indexCount == 0)
                {
                    continue;
                }

                meshGroups.Add(group);

                if (vertexCount > ushort.MaxValue)
                {
                    uint[] indices = new uint[indexCount];
                    for (int i = 0; i < group.Faces.Length; i++)
                    {
                        Face face = group.Faces[i];
                        uint index0 = objFile.GetOrCreate(vertexMap, vertices, face.Vertex0, face.Vertex1, face.Vertex2);
                        uint index1 = objFile.GetOrCreate(vertexMap, vertices, face.Vertex1, face.Vertex2, face.Vertex0);
                        uint index2 = objFile.GetOrCreate(vertexMap, vertices, face.Vertex2, face.Vertex0, face.Vertex1);

                        // Reverse winding order here.
                        indices[(i * 3)] = index0;
                        indices[(i * 3) + 2] = index1;
                        indices[(i * 3) + 1] = index2;
                    }

                    ibs.Add(DeviceBuffer.Create(BufferUsageFlags.IndexBuffer, indices, false));
                }
                else
                {
                    ushort[] indices = new ushort[indexCount];
                    for (int i = 0; i < group.Faces.Length; i++)
                    {
                        Face face = group.Faces[i];
                        uint index0 = objFile.GetOrCreate(vertexMap, vertices, face.Vertex0, face.Vertex1, face.Vertex2);
                        uint index1 = objFile.GetOrCreate(vertexMap, vertices, face.Vertex1, face.Vertex2, face.Vertex0);
                        uint index2 = objFile.GetOrCreate(vertexMap, vertices, face.Vertex2, face.Vertex0, face.Vertex1);

                        // Reverse winding order here.
                        indices[(i * 3)] = (ushort)index0;
                        indices[(i * 3) + 2] = (ushort)index1;
                        indices[(i * 3) + 1] = (ushort)index2;
                    }

                    ibs.Add(DeviceBuffer.Create(BufferUsageFlags.IndexBuffer, indices, false));
                }

            }

            DeviceBuffer vb = DeviceBuffer.Create(BufferUsageFlags.VertexBuffer, vertices.ToArray(), false);
            Model model = new Model
            {
                VertexBuffers = new[] { vb },
                IndexBuffers = ibs.ToArray(),
                BoundingBox = BoundingBox.FromPoints(objFile.Positions)                
            };

            model.Geometries = new Geometry[meshGroups.Count][];
            for (int i = 0; i < meshGroups.Count; i++)
            {
                var geom = new Geometry
                {
                    Name = meshGroups[i].Name,
                    VertexBuffers = new DeviceBuffer[] { vb },
                    IndexBuffer = ibs[i]
                };

                geom.SetDrawRange(PrimitiveTopology.TriangleList, 0, (uint)ibs[i].Count);
                geom.VertexLayout = VertexPosNormTex.Layout;
                model.Geometries[i] = new Geometry[] { geom };
                model.GeometryCenters.Add(Vector3.Zero);
            }

            var resourceLayout = new ResourceLayout(0)
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
            };

            var resourceLayoutTex = new ResourceLayout(1)
            {
                new ResourceLayoutBinding(0, DescriptorType.CombinedImageSampler, ShaderStage.Fragment, 1)
            };

            var shader = Resources.Instance.Load<Shader>("Shaders/Textured.shader");

            if (!string.IsNullOrEmpty(objFile.MaterialLibName))
            {
                string path = FileUtil.GetPath(name);
                File file = FileSystem.GetFile(path + objFile.MaterialLibName);
                MtlParser mtlParser = new MtlParser();
                MtlFile mtlFile = mtlParser.Parse(file);

                for (int i = 0; i < meshGroups.Count; i++)
                {
                    if (!mtlFile.Definitions.TryGetValue(meshGroups[i].Material, out MaterialDefinition materialDefinition))
                    {
                        continue;
                    }

                    Material mat = ConvertMaterial(path, materialDefinition, shader);
                    model.Materials.Add(mat);
                }
            }

            return model;
        }

        Material ConvertMaterial(string path, MaterialDefinition materialDef, Shader shader)
        {
            Material material = new Material(shader);
            
            if (!string.IsNullOrEmpty(materialDef.DiffuseTexture))
            {
                Texture tex = Resources.Instance.Load<Texture>(path + materialDef.DiffuseTexture);
                material.SetTexture("DiffMap", tex.ResourceRef);
                //test
                //material.ResourceSet.Bind(0, tex).UpdateSets();
            }
            else
            {
                //material.ResourceSet.Bind(0, Texture.White).UpdateSets();
            }
            return material;
        }
    }



}
