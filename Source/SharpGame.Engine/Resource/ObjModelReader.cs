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
         
            List<VertexPosNormTex> vertices = new List<VertexPosNormTex>();

            foreach(MeshGroup group in objFile.MeshGroups)
            {
                ushort[] indices = new ushort[group.Faces.Length * 3];
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
            }

            return null;
        }

    }


}
