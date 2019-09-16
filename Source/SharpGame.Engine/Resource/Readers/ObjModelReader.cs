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

        public override Resource LoadResource(string name)
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
            FastList<VertexPosTexNorm> vertices = new FastList<VertexPosTexNorm>();
            FastList<VertexPosTexNormTangent> tangentVertices = new FastList<VertexPosTexNormTangent>();
            List<uint[]> trianglesList = new List<uint[]>();

            foreach (MeshGroup group in objFile.MeshGroups)
            {
                int indexCount = group.Faces.Length * 3;
                if(indexCount == 0)
                {
                    continue;
                }

                meshGroups.Add(group);
                uint[] triangles;
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
                    triangles = indices;
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

                    triangles = new uint[indices.Length];
                    for(int i = 0; i < indices.Length; i++)
                    {
                        triangles[i] = indices[i];
                    }
                }

                tangentVertices.Resize(vertices.Count);

                trianglesList.Add(triangles);
            }

            CalculateMeshTangents(vertices, tangentVertices, trianglesList);
            //DeviceBuffer vb = DeviceBuffer.Create(BufferUsageFlags.VertexBuffer, vertices.ToArray(), false);
            DeviceBuffer vb = DeviceBuffer.Create(BufferUsageFlags.VertexBuffer, tangentVertices.Items, false);

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
                geom.VertexLayout = VertexPosTexNormTangent.Layout;
                model.Geometries[i] = new Geometry[] { geom };
                model.GeometryCenters.Add(vec3.Zero);
            }

            var shader = Resources.Instance.Load<Shader>("Shaders/LitSolid.shader");

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
  
        public static void CalculateMeshTangents(FastList<VertexPosTexNorm> vertices,
            FastList<VertexPosTexNormTangent> tangentVertices, List<uint[]> trianglesList)
        {
            //speed up math by copying the mesh arrays
            //variable definitions
            int vertexCount = vertices.Count;

            vec3[] tan1 = new vec3[vertexCount];
            vec3[] tan2 = new vec3[vertexCount];

            vec4[] tangents = new vec4[vertexCount];

            foreach (var triangles in trianglesList)
            {
                int triangleCount = triangles.Length;
                for (long a = 0; a < triangleCount; a += 3)
                {
                    int i1 = (int)triangles[a + 0];
                    int i2 = (int)triangles[a + 1];
                    int i3 = (int)triangles[a + 2];

                    vec3 v1 = vertices[i1].position;
                    vec3 v2 = vertices[i2].position;
                    vec3 v3 = vertices[i3].position;

                    vec2 w1 = vertices[i1].texcoord;
                    vec2 w2 = vertices[i2].texcoord;
                    vec2 w3 = vertices[i3].texcoord;

                    float x1 = v2.x - v1.x;
                    float x2 = v3.x - v1.x;
                    float y1 = v2.y - v1.y;
                    float y2 = v3.y - v1.y;
                    float z1 = v2.z - v1.z;
                    float z2 = v3.z - v1.z;

                    float s1 = w2.x - w1.x;
                    float s2 = w3.x - w1.x;
                    float t1 = w2.y - w1.y;
                    float t2 = w3.y - w1.y;

                    float r = 1.0f / (s1 * t2 - s2 * t1);

                    vec3 sdir = new vec3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                    vec3 tdir = new vec3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                    tan1[i1] += sdir;
                    tan1[i2] += sdir;
                    tan1[i3] += sdir;

                    tan2[i1] += tdir;
                    tan2[i2] += tdir;
                    tan2[i3] += tdir;
                }
            }

            for (int a = 0; a < vertexCount; ++a)
            {
                vec3 n = vertices[a].normal;
                vec3 t = tan1[a];
                if(t == vec3.Zero)
                {
                    continue;
                }

                ref var newVertex = ref tangentVertices.At(a);
                newVertex.position = vertices[a].position;
                newVertex.texcoord = vertices[a].texcoord;
                newVertex.normal = vertices[a].normal;

                vec3 tmp = glm.normalize(t - n * vec3.Dot(n, t));

                var tangent = new vec4(tmp.x, tmp.y, tmp.z, 0);
                tangent.w = -1.0f;// (vec3.Dot(vec3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
                newVertex.tangent = tangent;
            }

        }

        Material ConvertMaterial(string path, MaterialDefinition materialDef, Shader shader)
        {
            Material material = new Material(shader);
            
            if (!string.IsNullOrEmpty(materialDef.DiffuseTexture))
            {
                Texture tex = Resources.Instance.Load<Texture>(path + materialDef.DiffuseTexture);
                material.SetTexture("DiffMap", tex.ResourceRef);                
            }
            else
            {
                material.SetTexture("DiffMap", Texture.White);
            }

            if (!string.IsNullOrEmpty(materialDef.BumpMap))
            {
                Texture tex = Resources.Instance.Load<Texture>(path + materialDef.BumpMap);
                material.SetTexture("NormalMap", tex.ResourceRef);
            }
            else
            {
                material.SetTexture("NormalMap", Texture.Blue);
            }

            if (!string.IsNullOrEmpty(materialDef.SpecularColorTexture))
            {
                Texture tex = Resources.Instance.Load<Texture>(path + materialDef.SpecularColorTexture);
                material.SetTexture("SpecMap", tex.ResourceRef);
            }
            else
            {
                material.SetTexture("SpecMap", Texture.White);
            }
            return material;
        }
    }



}
