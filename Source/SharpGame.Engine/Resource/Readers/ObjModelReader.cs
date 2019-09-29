#define FPFIXES
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

            List<Buffer> ibs = new List<Buffer>();
            List<MeshGroup> meshGroups = new List<MeshGroup>();
            FastList<VertexPosTexNorm> vertices = new FastList<VertexPosTexNorm>();
            FastList<VertexPosTexNTB> tangentVertices = new FastList<VertexPosTexNTB>();
            List<uint[]> trianglesList = new List<uint[]>();

            foreach (MeshGroup group in objFile.MeshGroups)
            {
                int indexCount = group.Faces.Length * 3;
                if (indexCount == 0)
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

                    ibs.Add(Buffer.Create(BufferUsageFlags.IndexBuffer, indices, false));
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

                    ibs.Add(Buffer.Create(BufferUsageFlags.IndexBuffer, indices, false));

                    triangles = new uint[indices.Length];
                    for (int i = 0; i < indices.Length; i++)
                    {
                        triangles[i] = indices[i];
                    }
                }

                tangentVertices.Resize(vertices.Count);

                trianglesList.Add(triangles);
            }

            CreateTangentSpaceTangents(vertices, tangentVertices, trianglesList);
            //DeviceBuffer vb = DeviceBuffer.Create(BufferUsageFlags.VertexBuffer, vertices.ToArray(), false);
            Buffer vb = Buffer.Create(BufferUsageFlags.VertexBuffer, tangentVertices.Items, false);

            Model model = new Model
            {
                VertexBuffers = new List<Buffer> { vb },
                IndexBuffers = ibs,
                BoundingBox = BoundingBox.FromPoints(objFile.Positions)
            };

            model.Geometries = new List<Geometry[]>();
            for (int i = 0; i < meshGroups.Count; i++)
            {
                var geom = new Geometry
                {
                    Name = meshGroups[i].Name,
                    VertexBuffers = new Buffer[] { vb },
                    IndexBuffer = ibs[i]
                };

                geom.SetDrawRange(PrimitiveTopology.TriangleList, 0, (uint)ibs[i].Count);
                geom.VertexLayout = VertexPosTexNTB.Layout;
                model.Geometries.Add(new Geometry[] { geom });
                model.GeometryCenters.Add(vec3.Zero);
            }

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

                    Material mat = ConvertMaterial(path, materialDefinition);
                    model.Materials.Add(mat);
                }
            }

            return model;
        }

        /*
        public static void CalculateMeshTangents(FastList<VertexPosTexNorm> vertices,
            FastList<VertexPosTexNTB> tangentVertices, List<uint[]> trianglesList)
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
                if (t == vec3.Zero)
                {
                    continue;
                }

                ref var newVertex = ref tangentVertices.At(a);
                newVertex.position = vertices[a].position;
                newVertex.texcoord = vertices[a].texcoord;
                newVertex.normal = vertices[a].normal;

                vec3 tmp = glm.normalize(t - n * vec3.Dot(n, t));

                var tangent = new vec4(tmp.x, tmp.y, tmp.z, 0);
                tangent.w = (vec3.Dot(vec3.Cross(t, n), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
                newVertex.tangent = tangent;
            }

        }*/

        void CreateTangentSpaceTangents(FastList<VertexPosTexNorm> vertices,
            FastList<VertexPosTexNTB> tangentVertices, List<uint[]> trianglesList)
        {
            vec3 v0, v1, v2;
            vec3 p0, p1, p2;
            vec3 d1, d2;

            float[,] uv = new float[3, 2];
            float det, u, v, l1, l2;

            int vertexCount = vertices.Count;
            vec3[] sVector = new vec3[vertexCount];
            vec3[] tVector = new vec3[vertexCount];

            foreach (var indices in trianglesList)
            {
                int faceCount = indices.Length;
                for (int k = 0; k < faceCount; k += 3)
                {
                    v0 = vertices[(int)indices[k]].position;
                    v1 = vertices[(int)indices[k + 1]].position - v0;
                    v2 = vertices[(int)indices[k + 2]].position - v0;

                    uv[0, 0] = vertices[(int)indices[k]].texcoord.x;
                    uv[0, 1] = vertices[(int)indices[k]].texcoord.y;
                    uv[1, 0] = vertices[(int)indices[k + 1]].texcoord.x - uv[0, 0];
                    uv[1, 1] = vertices[(int)indices[k + 1]].texcoord.y - uv[0, 1];
                    uv[2, 0] = vertices[(int)indices[k + 2]].texcoord.x - uv[0, 0];
                    uv[2, 1] = vertices[(int)indices[k + 2]].texcoord.y - uv[0, 1];

                    det = (uv[1, 0] * uv[2, 1]) - (uv[2, 0] * uv[1, 1]);

                    if (Math.Abs(det) < 0.000001f)
                    {
                        continue;
                    }

                    u = 0; v = 0;
                    u -= uv[0, 0];
                    v -= uv[0, 1];
                    p0 = v0 + v1 * ((u * uv[2, 1] - uv[2, 0] * v) / det) + v2 * ((uv[1, 0] * v - u * uv[1, 1]) / det);

                    u = 1; v = 0;
                    u -= uv[0, 0];
                    v -= uv[0, 1];

                    p1 = v0 + v1 * ((u * uv[2, 1] - uv[2, 0] * v) / det) + v2 * ((uv[1, 0] * v - u * uv[1, 1]) / det);

                    u = 0; v = 1;
                    u -= uv[0, 0]; v -= uv[0, 1];
                    p2 = v0 + v1 * ((u * uv[2, 1] - uv[2, 0] * v) / det) + v2 * ((uv[1, 0] * v - u * uv[1, 1]) / det);
                    d1 = p2 - p0;
                    d2 = p1 - p0;
                    l1 = glm.length(d1);
                    l2 = glm.length(d2);
                    d1 *= 1.0f / l1;
                    d2 *= 1.0f / l2;
                    int j = (int)indices[k];

                    sVector[j] += d1;
                    tVector[j].x += d2.x; tVector[j].y += d2.y; tVector[j].z += d2.z;

                    j = (int)indices[k + 1];

                    sVector[j].x += d1.x; sVector[j].y += d1.y; sVector[j].z += d1.z;
                    tVector[j].x += d2.x; tVector[j].y += d2.y; tVector[j].z += d2.z;

                    j = (int)indices[k + 2];

                    sVector[j].x += d1.x; sVector[j].y += d1.y; sVector[j].z += d1.z;
                    tVector[j].x += d2.x; tVector[j].y += d2.y; tVector[j].z += d2.z;
                }
            }

            for (int i = 0; i < vertexCount; i++)
            {
                v0 = sVector[i];
                v0 = NormalizeRobust(v0);
                v1 = tVector[i];

                vec3 n = glm.vec3(vertices[i].normal.x, vertices[i].normal.y, vertices[i].normal.z);
                if (glm.length2(v1) < 0.0001f)
                {
                    v1 = glm.cross(v0, n);
                }

                v1 = NormalizeRobust(v1);
                sVector[i] = v0;
                tVector[i].x = v1.x;
                tVector[i].y = v1.y;
                tVector[i].z = v1.z;

                ref var newVertex = ref tangentVertices.At(i);
                newVertex.position = vertices[i].position;
                newVertex.texcoord = vertices[i].texcoord;

                newVertex.normal = n;
                newVertex.tangent = tVector[i];
                newVertex.bitangent = sVector[i];
            }

        }

        Material ConvertMaterial(string path, MaterialDefinition materialDef)
        {
            Shader shader = null;// Resources.Instance.Load<Shader>("Shaders/LitSolid.shader");

            if (!string.IsNullOrEmpty(materialDef.AlphaMap))
            {
                shader = Resources.Instance.Load<Shader>("Shaders/LitAlphaTest.shader");
            }
            else if(string.IsNullOrEmpty(materialDef.DiffuseTexture) && materialDef.Opacity < 1)
            {
                shader = Resources.Instance.Load<Shader>("Shaders/LitParticle.shader");
            }
            else
            {
                shader = Resources.Instance.Load<Shader>("Shaders/LitSolid.shader");
            }

            Material material = new Material(shader);

            if (!string.IsNullOrEmpty(materialDef.DiffuseTexture))
            {
                Texture tex = Resources.Instance.Load<Texture>(path + materialDef.DiffuseTexture);
                material.SetTexture("DiffMap", tex.ResourceRef);
            }
            else
            {
                var color = materialDef.DiffuseReflectivity;
                material.SetTexture("DiffMap", Texture.CreateByColor(
                    new Color(color.X, color.Y, color.Z, materialDef.Opacity)));
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
                material.SetTexture("SpecMap", Texture.Black);
            }

            if (!string.IsNullOrEmpty(materialDef.AlphaMap))
            {
                Texture tex = Resources.Instance.Load<Texture>(path + materialDef.AlphaMap);
                material.SetTexture("AlphaMap", tex.ResourceRef);
            }
            else
            {
                material.SetTexture("AlphaMap", Texture.White);
            }
            return material;
        }

        static vec3 NormalizeRobust(in vec3 a, out float l, out float div)
        {
            float a0, a1, a2, aa0, aa1, aa2;
            a0 = a[0];
            a1 = a[1];
            a2 = a[2];

#if FPFIXES
            if (MathUtil.WithinEpsilon(a0, 0.0F, 0.00001F))
                a0 = aa0 = 0;
            else
#endif
            {
                aa0 = Math.Abs(a0);
            }

#if FPFIXES
            if (MathUtil.WithinEpsilon(a1, 0.0F, 0.00001F))
                a1 = aa1 = 0;
            else
#endif
            {
                aa1 = Math.Abs(a1);
            }

#if FPFIXES
            if (MathUtil.WithinEpsilon(a2, 0.0F, 0.00001F))
                a2 = aa2 = 0;
            else
#endif
            {
                aa2 = Math.Abs(a2);
            }

            if (aa1 > aa0)
            {
                if (aa2 > aa1)
                {
                    a0 /= aa2;
                    a1 /= aa2;
                    l = glm.invSqrt(a0 * a0 + a1 * a1 + 1.0F);
                    div = aa2;
                    return glm.vec3(a0 * l, a1 * l, CopySignf(l, a2));
                }
                else
                {
                    // aa1 is largest
                    a0 /= aa1;
                    a2 /= aa1;
                    l = glm.invSqrt(a0 * a0 + a2 * a2 + 1.0F);
                    div = aa1;
                    return glm.vec3(a0 * l, CopySignf(l, a1), a2 * l);
                }
            }
            else
            {
                if (aa2 > aa0)
                {
                    // aa2 is largest
                    a0 /= aa2;
                    a1 /= aa2;
                    l = glm.invSqrt(a0 * a0 + a1 * a1 + 1.0F);
                    div = aa2;
                    return glm.vec3(a0 * l, a1 * l, CopySignf(l, a2));
                }
                else
                {
                    // aa0 is largest
                    if (aa0 <= 0)
                    {
                        l = 0;
                        div = 1;
                        return glm.vec3(0.0F, 1.0F, 0.0F);
                    }

                    a1 /= aa0;
                    a2 /= aa0;
                    l = glm.invSqrt(a1 * a1 + a2 * a2 + 1.0F);
                    div = aa0;
                    return glm.vec3(CopySignf(l, a0), a1 * l, a2 * l);
                }
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 4)]
        struct FloatUIntUnion
        {
            [FieldOffset(0)]
            public float f;
            [FieldOffset(0)]
            public uint i;
        };

        static float CopySignf(float x, float y)
        {
            FloatUIntUnion u = new FloatUIntUnion(), u0 = new FloatUIntUnion(), u1 = new FloatUIntUnion();
            u0.f = x; u1.f = y;

            uint a = u0.i;
            uint b = u1.i;
            int mask = 1 << 31;
            uint sign = (uint)(b & mask);
            a &= (uint)~mask;
            a |= sign;

            u.i = a;
            return u.f;
        }

        vec3 NormalizeRobust(in vec3 a)
        {
            float l, div;
            return NormalizeRobust(a, out l, out div);
        }

        vec3 NormalizeRobust(in vec3 a, out float invOriginalLength)
        {
            float l, div;
            vec3 n = NormalizeRobust(a, out l, out div);
            invOriginalLength = l / div;
            // guard for NaNs
            Debug.Assert(n == n);
            Debug.Assert(invOriginalLength == invOriginalLength);
            Debug.Assert(IsNormalized(n));
            return n;
        }

        bool IsNormalized(in vec3 vec, float epsilon = glm.epsilon)
        {
            return MathUtil.WithinEpsilon(glm.length2(vec), 1.0F, epsilon);
        }



    }



}
