using System;
using System.Collections.Generic;
using System.Text;
using static SharpGame.ObjFile;

namespace SharpGame
{
    public class ObjModelReader : ResourceReader<Texture>
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


            DeviceBuffer[] ibs = new DeviceBuffer[objFile.MeshGroups.Length];
            List<VertexPosNormTex> vertices = new List<VertexPosNormTex>();
            int index = 0;
            foreach(MeshGroup group in objFile.MeshGroups)
            {
                int indexCount = group.Faces.Length * 3;
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

                    ibs[index] = DeviceBuffer.Create(BufferUsage.IndexBuffer, indices, false);
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

                    ibs[index] = DeviceBuffer.Create(BufferUsage.IndexBuffer, indices, false);
                }



                index++;

            }

            DeviceBuffer vb = DeviceBuffer.Create(BufferUsage.VertexBuffer, vertices.ToArray(), false);

            Model model = new Model
            {
                VertexBuffers = new[] { vb },
                IndexBuffers = ibs          
            };

            model.Geometries = new Geometry[objFile.MeshGroups.Length][];
            for(int i = 0; i < objFile.MeshGroups.Length; i++)
            {
                var geom = new Geometry
                {
                    VertexBuffers = new DeviceBuffer[] { vb },
                    IndexBuffer = ibs[i]
                };

                geom.SetDrawRange(PrimitiveTopology.TriangleList, 0, ibs[i].Size);

                model.Geometries[i] = new Geometry[] { geom };

            }
            


            return model;
        }

    }


}
