using System.Numerics;
using System.Runtime.InteropServices;
using Vulkan;

namespace SharpGame
{
    public class GeometricPrimitive
    {
        public static Geometry CreatePlane(float width, float height)
        {
            return null;
        }

        public static Geometry CreateCube(float width, float height, float depth)
        {
            float w2 = 0.5f * width;
            float h2 = 0.5f * height;
            float d2 = 0.5f * depth;

            VertexPosNormTex[] vertices =
            {
                // Fill in the front face vertex data.
                new VertexPosNormTex(-w2, +h2, -d2, +0, +0, -1, +0, +0),
                new VertexPosNormTex(-w2, -h2, -d2, +0, +0, -1, +0, +1),
                new VertexPosNormTex(+w2, -h2, -d2, +0, +0, -1, +1, +1),
                new VertexPosNormTex(+w2, +h2, -d2, +0, +0, -1, +1, +0),
                // Fill in the back face vertex data.
                new VertexPosNormTex(-w2, +h2, +d2, +0, +0, +1, +1, +0),
                new VertexPosNormTex(+w2, +h2, +d2, +0, +0, +1, +0, +0),
                new VertexPosNormTex(+w2, -h2, +d2, +0, +0, +1, +0, +1),
                new VertexPosNormTex(-w2, -h2, +d2, +0, +0, +1, +1, +1),
                // Fill in the top face vertex data.
                new VertexPosNormTex(-w2, -h2, -d2, +0, +1, +0, +0, +0),
                new VertexPosNormTex(-w2, -h2, +d2, +0, +1, +0, +0, +1),
                new VertexPosNormTex(+w2, -h2, +d2, +0, +1, +0, +1, +1),
                new VertexPosNormTex(+w2, -h2, -d2, +0, +1, +0, +1, +0),
                // Fill in the bottom face vertex data.
                new VertexPosNormTex(-w2, +h2, -d2, +0, -1, +0, +1, +0),
                new VertexPosNormTex(+w2, +h2, -d2, +0, -1, +0, +0, +0),
                new VertexPosNormTex(+w2, +h2, +d2, +0, -1, +0, +0, +1),
                new VertexPosNormTex(-w2, +h2, +d2, +0, -1, +0, +1, +1),
                // Fill in the left face vertex data.
                new VertexPosNormTex(-w2, +h2, +d2, -1, +0, +0, +0, +0),
                new VertexPosNormTex(-w2, -h2, +d2, -1, +0, +0, +0, +1),
                new VertexPosNormTex(-w2, -h2, -d2, -1, +0, +0, +1, +1),
                new VertexPosNormTex(-w2, +h2, -d2, -1, +0, +0, +1, +0),
                // Fill in the right face vertex data.
                new VertexPosNormTex(+w2, +h2, -d2, +1, +0, +0, +0, +0),
                new VertexPosNormTex(+w2, -h2, -d2, +1, +0, +0, +0, +1),
                new VertexPosNormTex(+w2, -h2, +d2, +1, +0, +0, +1, +1),
                new VertexPosNormTex(+w2, +h2, +d2, +1, +0, +0, +1, +0)
            };

            int[] indices =
            {
                // Fill in the front face index data.
                0, 1, 2, 0, 2, 3,
                // Fill in the back face index data.
                4, 5, 6, 4, 6, 7,
                // Fill in the top face index data.
                8, 9, 10, 8, 10, 11,
                // Fill in the bottom face index data.
                12, 13, 14, 12, 14, 15,
                // Fill in the left face index data
                16, 17, 18, 16, 18, 19,
                // Fill in the right face index data
                20, 21, 22, 20, 22, 23
            };

            var geom = new Geometry
            {
                VertexBuffers = new[] { DeviceBuffer.Create(BufferUsageFlags.VertexBuffer, vertices) },
                IndexBuffer = DeviceBuffer.Create(BufferUsageFlags.IndexBuffer, indices),
                VertexLayout = VertexPosNormTex.Layout
            };

            geom.SetDrawRange(PrimitiveTopology.TriangleList, 0, (uint)indices.Length);
            return geom;
        }


    }
}
