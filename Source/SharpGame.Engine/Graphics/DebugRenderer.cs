﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    /// Debug rendering line.
    public struct DebugLine
    {
        /// Start position.
        public Vector3 start_;
        /// End position.
        public Vector3 end_;
        /// Color.
        public int color_;

        public DebugLine(Vector3 start, Vector3 end, int color)
        {
            start_ = start;
            end_ = end;
            color_ = color;
        }
    };

    /// Debug render triangle.
    public struct DebugTriangle
    {
        /// Vertex a.
        public Vector3 v1_;
        /// Vertex b.
        public Vector3 v2_;
        /// Vertex c.
        public Vector3 v3_;
        /// Color.
        public int color_;
        public DebugTriangle(Vector3 v1, Vector3 v2, Vector3 v3, int color)
        {
            v1_ = v1;
            v2_ = v2;
            v3_ = v3;
            color_ = color;
        }
    };

    public class DebugRenderer : Component
    {
        // Cap the amount of lines to prevent crash when eg. debug rendering large heightfields
        const int MAX_LINES = 1000000;
        // Cap the amount of triangles to prevent crash.
        const int MAX_TRIANGLES = 100000;

        /// Lines rendered with depth test.
        List<DebugLine> lines_ = new List<DebugLine>();
        /// Lines rendered without depth test.
        List<DebugLine> noDepthLines_ = new List<DebugLine>();
        /// Triangles rendered with depth test.
        List<DebugTriangle> triangles_ = new List<DebugTriangle>();
        /// Triangles rendered without depth test.
        List<DebugTriangle> noDepthTriangles_ = new List<DebugTriangle>();
        /// View transform.
        Matrix view_;
        /// Projection transform.
        Matrix projection_;
        /// Projection transform in API-specific format.
        Matrix gpuProjection_;
        /// View frustum.
        BoundingFrustum frustum_;
        /// Line antialiasing flag.
        bool lineAntiAlias_;

        Shader uiShader_;

        GraphicsPipeline renderState_ ;
        public DebugRenderer()
        {
            //   uiShader_ = cache.GetResource<Shader>("shaders/default.shader");         
            //this.SubscribeToEvent<EndFrame>(HandleEndFrame);
        }

        public void SetView(Camera camera)
        {
            if(!camera)
                return;

            view_ = camera.View;
            projection_ = camera.Projection;
            //gpuProjection_ = camera->GetGPUProjection();
            frustum_ = camera.Frustum;
        }

        public void AddLine(Vector3 start, Vector3 end, Color color, bool depthTest = true)
        {
            AddLine(start, end, color.ToRgba(), depthTest);
        }

        public void AddLine(Vector3 start, Vector3 end, int color, bool depthTest = true)
        {
            if(lines_.Count + noDepthLines_.Count >= MAX_LINES)
                return;

            if(depthTest)
                lines_.Add(new DebugLine(start, end, color));
            else
                noDepthLines_.Add(new DebugLine(start, end, color));
        }

        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color color, bool depthTest = true)
        {
            AddTriangle(v1, v2, v3, color.ToRgba(), depthTest);
        }

        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, int color, bool depthTest = true)
        {
            if(triangles_.Count + noDepthTriangles_.Count >= MAX_TRIANGLES)
                return;

            if(depthTest)
                triangles_.Add(new DebugTriangle(v1, v2, v3, color));
            else
                noDepthTriangles_.Add(new DebugTriangle(v1, v2, v3, color));
        }

        public void AddPolygon(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color color, bool depthTest = true)
        {
            AddTriangle(v1, v2, v3, color, depthTest);
            AddTriangle(v3, v4, v1, color, depthTest);
        }

        public void AddPolygon(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, int color, bool depthTest = true)
        {
            AddTriangle(v1, v2, v3, color, depthTest);
            AddTriangle(v3, v4, v1, color, depthTest);
        }

        public void AddNode(Node node, float scale, bool depthTest)
        {
            if(!node)
                return;

            Vector3 start = node.WorldPosition;
            Quaternion rotation = node.WorldRotation;

            AddLine(start, start + Vector3.Transform(scale * Vector3.Right, rotation), Color.Red.ToRgba(), depthTest);
            AddLine(start, start + Vector3.Transform(scale * Vector3.Up, rotation), Color.Green.ToRgba(), depthTest);
            AddLine(start, start + Vector3.Transform(scale * Vector3.ForwardLH, rotation), Color.Blue.ToRgba(), depthTest);
        }

        public void AddBoundingBox(ref BoundingBox box, Color color, bool depthTest = true, bool solid = false)
        {
            Vector3 min = box.Minimum;
            Vector3 max = box.Maximum;

            Vector3 v1 = new Vector3(max.X, min.Y, min.Z);
            Vector3 v2 = new Vector3(max.X, max.Y, min.Z);
            Vector3 v3 = new Vector3(min.X, max.Y, min.Z);
            Vector3 v4 = new Vector3(min.X, min.Y, max.Z);
            Vector3 v5 = new Vector3(max.X, min.Y, max.Z);
            Vector3 v6 = new Vector3(min.X, max.Y, max.Z);

            int uintColor = color.ToRgba();

            if(!solid)
            {
                AddLine(min, v1, uintColor, depthTest);
                AddLine(v1, v2, uintColor, depthTest);
                AddLine(v2, v3, uintColor, depthTest);
                AddLine(v3, min, uintColor, depthTest);
                AddLine(v4, v5, uintColor, depthTest);
                AddLine(v5, max, uintColor, depthTest);
                AddLine(max, v6, uintColor, depthTest);
                AddLine(v6, v4, uintColor, depthTest);
                AddLine(min, v4, uintColor, depthTest);
                AddLine(v1, v5, uintColor, depthTest);
                AddLine(v2, max, uintColor, depthTest);
                AddLine(v3, v6, uintColor, depthTest);
            }
            else
            {
                AddPolygon(min, v1, v2, v3, uintColor, depthTest);
                AddPolygon(v4, v5, max, v6, uintColor, depthTest);
                AddPolygon(min, v4, v6, v3, uintColor, depthTest);
                AddPolygon(v1, v5, max, v2, uintColor, depthTest);
                AddPolygon(v3, v2, max, v6, uintColor, depthTest);
                AddPolygon(min, v1, v5, v4, uintColor, depthTest);
            }
        }

        public void AddBoundingBox(ref BoundingBox box, ref Matrix transform, Color color, bool depthTest, bool solid)
        {
            Vector3 min = box.Minimum;
            Vector3 max = box.Maximum;

            Vector3 v0 = Vector3.Transform(min, transform);
            Vector3 v1 = Vector3.Transform(new Vector3(max.X, min.Y, min.Z), transform);
            Vector3 v2 = Vector3.Transform(new Vector3(max.X, max.Y, min.Z), transform);
            Vector3 v3 = Vector3.Transform(new Vector3(min.X, max.Y, min.Z), transform);
            Vector3 v4 = Vector3.Transform(new Vector3(min.X, min.Y, max.Z), transform);
            Vector3 v5 = Vector3.Transform(new Vector3(max.X, min.Y, max.Z), transform);
            Vector3 v6 = Vector3.Transform(new Vector3(min.X, max.Y, max.Z), transform);
            Vector3 v7 = Vector3.Transform(max, transform);

            int uintColor = color.ToRgba();

            if(!solid)
            {
                AddLine(v0, v1, uintColor, depthTest);
                AddLine(v1, v2, uintColor, depthTest);
                AddLine(v2, v3, uintColor, depthTest);
                AddLine(v3, v0, uintColor, depthTest);
                AddLine(v4, v5, uintColor, depthTest);
                AddLine(v5, v7, uintColor, depthTest);
                AddLine(v7, v6, uintColor, depthTest);
                AddLine(v6, v4, uintColor, depthTest);
                AddLine(v0, v4, uintColor, depthTest);
                AddLine(v1, v5, uintColor, depthTest);
                AddLine(v2, v7, uintColor, depthTest);
                AddLine(v3, v6, uintColor, depthTest);
            }
            else
            {
                AddPolygon(v0, v1, v2, v3, uintColor, depthTest);
                AddPolygon(v4, v5, v7, v6, uintColor, depthTest);
                AddPolygon(v0, v4, v6, v3, uintColor, depthTest);
                AddPolygon(v1, v5, v7, v2, uintColor, depthTest);
                AddPolygon(v3, v2, v7, v6, uintColor, depthTest);
                AddPolygon(v0, v1, v5, v4, uintColor, depthTest);
            }
        }
        /*
        void AddFrustum(const Frustum& frustum, Color color, bool depthTest)
        {
            const Vector3* vertices = frustum.vertices_;
            int uintColor = color.ToUInt();

            AddLine(vertices[0], vertices[1], uintColor, depthTest);
            AddLine(vertices[1], vertices[2], uintColor, depthTest);
            AddLine(vertices[2], vertices[3], uintColor, depthTest);
            AddLine(vertices[3], vertices[0], uintColor, depthTest);
            AddLine(vertices[4], vertices[5], uintColor, depthTest);
            AddLine(vertices[5], vertices[6], uintColor, depthTest);
            AddLine(vertices[6], vertices[7], uintColor, depthTest);
            AddLine(vertices[7], vertices[4], uintColor, depthTest);
            AddLine(vertices[0], vertices[4], uintColor, depthTest);
            AddLine(vertices[1], vertices[5], uintColor, depthTest);
            AddLine(vertices[2], vertices[6], uintColor, depthTest);
            AddLine(vertices[3], vertices[7], uintColor, depthTest);
        }

        void AddPolyhedron(const Polyhedron& poly, Color color, bool depthTest)
        {
            int uintColor = color.ToUInt();

            for(int i = 0; i < poly.faces_.Count; ++i)
            {
                const PODVector<Vector3>&face = poly.faces_[i];
                if(face.Count >= 3)
                {
                    for(int j = 0; j < face.Count; ++j)
                        AddLine(face[j], face[(j + 1) % face.Count], uintColor, depthTest);
                }
            }
        }*/

        public void AddSphere(ref BoundingSphere sphere, Color color, bool depthTest = true)
        {
            int uintColor = color.ToRgba();

            for(float j = 0; j < 180; j += 45)
            {
                for(float i = 0; i < 360; i += 45)
                {
                    Vector3 p1 = sphere.GetPoint(i, j);
                    Vector3 p2 = sphere.GetPoint(i + 45, j);
                    Vector3 p3 = sphere.GetPoint(i, j + 45);
                    Vector3 p4 = sphere.GetPoint(i + 45, j + 45);

                    AddLine(p1, p2, uintColor, depthTest);
                    AddLine(p3, p4, uintColor, depthTest);
                    AddLine(p1, p3, uintColor, depthTest);
                    AddLine(p2, p4, uintColor, depthTest);
                }
            }
        }

        public void AddSphereSector(ref BoundingSphere sphere, ref Quaternion rotation, float angle,
            bool drawLines, Color color, bool depthTest = true)
        {
            if(angle <= 0.0f)
                return;
            else if(angle >= 360.0f)
            {
                AddSphere(ref sphere, color, depthTest);
                return;
            }

            const int numCircleSegments = 8;
            const int numLines = 4;
            const float arcStep = 45.0f;

            int uintColor = color.ToRgba();
            float halfAngle = 0.5f * angle;
            int numArcSegments = (int)(Math.Ceiling(halfAngle / arcStep)) + 1;

            // Draw circle
            for(int j = 0; j < numCircleSegments; ++j)
            {
                AddLine(
                    sphere.Center + Vector3.Transform(sphere.GetLocalPoint(j * 360.0f / numCircleSegments, halfAngle), rotation),
                    sphere.Center + Vector3.Transform(sphere.GetLocalPoint((j + 1) * 360.0f / numCircleSegments, halfAngle), rotation),
                    uintColor);
            }

            // Draw arcs
            const int step = numCircleSegments / numLines;
            for(int i = 0; i < numArcSegments - 1; ++i)
            {
                for(int j = 0; j < numCircleSegments; j += step)
                {
                    float nextPhi = i + 1 == numArcSegments - 1 ? halfAngle : (i + 1) * arcStep;
                    AddLine(
                        sphere.Center + Vector3.Transform(sphere.GetLocalPoint(j * 360.0f / numCircleSegments, i * arcStep), rotation),
                        sphere.Center + Vector3.Transform(sphere.GetLocalPoint(j * 360.0f / numCircleSegments, nextPhi), rotation),
                        uintColor);
                }
            }

            // Draw lines
            if(drawLines)
            {
                for(int j = 0; j < numCircleSegments; j += step)
                {
                    AddLine(sphere.Center,
                        sphere.Center + Vector3.Transform(sphere.GetLocalPoint(j * 360.0f / numCircleSegments, halfAngle), rotation),
                        uintColor);
                }
            }
        }

        public void AddCylinder(Vector3 position, float radius, float height, Color color, bool depthTest)
        {
            BoundingSphere sphere = new BoundingSphere(position, radius);
            Vector3 heightVec = new Vector3(0, height, 0);
            Vector3 offsetXVec = new Vector3(radius, 0, 0);
            Vector3 offsetZVec = new Vector3(0, 0, radius);
            for(float i = 0; i < 360; i += 45)
            {
                Vector3 p1 = sphere.GetPoint(i, 90);
                Vector3 p2 = sphere.GetPoint(i + 45, 90);
                AddLine(p1, p2, color, depthTest);
                AddLine(p1 + heightVec, p2 + heightVec, color, depthTest);
            }
            AddLine(position + offsetXVec, position + heightVec + offsetXVec, color, depthTest);
            AddLine(position - offsetXVec, position + heightVec - offsetXVec, color, depthTest);
            AddLine(position + offsetZVec, position + heightVec + offsetZVec, color, depthTest);
            AddLine(position - offsetZVec, position + heightVec - offsetZVec, color, depthTest);
        }

        public void AddSkeleton(Skeleton skeleton, Color color, bool depthTest)
        {
            Bone[] bones = skeleton.Bones;
            if(bones.Length == 0)
                return;

            int uintColor = color.ToRgba();

            for(int i = 0; i < bones.Length; ++i)
            {
                // Skip if bone contains no skinned geometry
                if(bones[i].radius_ < MathUtil.Epsilon && bones[i].boundingBox_.Size.LengthSquared() < MathUtil.Epsilon)
                    continue;

                Node boneNode = bones[i].node_;
                if(!boneNode)
                    continue;

                Vector3 start = boneNode.WorldPosition;
                Vector3 end;

                int j = bones[i].parentIndex_;
                Node parentNode = boneNode.Parent;

                // If bone has a parent defined, and it also skins geometry, draw a line to it. Else draw the bone as a point
                if(parentNode && (bones[j].radius_ >= MathUtil.Epsilon || bones[j].boundingBox_.Size.LengthSquared() >= MathUtil.Epsilon))
                    end = parentNode.WorldPosition;
                else
                    end = start;

                AddLine(start, end, uintColor, depthTest);
            }
        }
        /*
    void AddTriangleMesh(const void* vertexData, int vertexSize, const void* indexData, int indexSize,
        int indexStart, int indexCount, const Matrix3x4& transform, Color color, bool depthTest)
    {
        int uintColor = color.ToUInt();
        const int char* srcData = (const int char*)vertexData;

        // 16-bit indices
        if(indexSize == sizeof(int short))
    {
            const int short* indices = ((const int short*)indexData) +indexStart;
            const int short* indicesEnd = indices + indexCount;

            while(indices < indicesEnd)
            {
                Vector3 v0 = transform * *((const Vector3*)(&srcData[indices[0] * vertexSize]));
                Vector3 v1 = transform * *((const Vector3*)(&srcData[indices[1] * vertexSize]));
                Vector3 v2 = transform * *((const Vector3*)(&srcData[indices[2] * vertexSize]));

                AddLine(v0, v1, uintColor, depthTest);
                AddLine(v1, v2, uintColor, depthTest);
                AddLine(v2, v0, uintColor, depthTest);

                indices += 3;
            }
        }
    else
    {
            const int* indices = ((const int*)indexData) +indexStart;
            const int* indicesEnd = indices + indexCount;

            while(indices < indicesEnd)
            {
                Vector3 v0 = transform * *((const Vector3*)(&srcData[indices[0] * vertexSize]));
                Vector3 v1 = transform * *((const Vector3*)(&srcData[indices[1] * vertexSize]));
                Vector3 v2 = transform * *((const Vector3*)(&srcData[indices[2] * vertexSize]));

                AddLine(v0, v1, uintColor, depthTest);
                AddLine(v1, v2, uintColor, depthTest);
                AddLine(v2, v0, uintColor, depthTest);

                indices += 3;
            }
        }
    }*/
        /*
            void AddCircle(Vector3 center, Vector3 normal, float radius, Color color, int steps, bool depthTest)
            {
                Quaternion orientation = Quaternion.FromRotationTo(Vector3.Up, normal.Normalized());
                Vector3 p = orientation * new Vector3(radius, 0, 0) + center;
                int uintColor = color.ToUInt();

                for(int i = 1; i <= steps; ++i)
                {
                    float angle = (float)i / (float)steps * 360.0f;
                    Vector3 v(radius* Cos(angle), 0, radius* Sin(angle));
                Vector3 c = orientation * v + center;
                AddLine(p, c, uintColor, depthTest);
                p = c;
            }

            p = center + normal* (radius / 4.0f);
        AddLine(center, p, uintColor, depthTest);
        }*/

        void AddCross(Vector3 center, float size, Color color, bool depthTest)
        {
            int uintColor = color.ToRgba();

            float halfSize = size / 2.0f;
            for(int i = 0; i < 3; ++i)
            {
                Vector3 start = new Vector3(center.X, center.Y, center.Z);
                Vector3 end = new Vector3(center.X, center.Y, center.Z);
                start[i] = start[i] - halfSize;
                end[i] = end[i] + halfSize;
                AddLine(start, end, uintColor, depthTest);
            }
        }

        void AddQuad(Vector3 center, float width, float height, Color color, bool depthTest)
        {
            int uintColor = color.ToRgba();

            Vector3 v0 = new Vector3(center.X - width / 2, center.Y, center.Z - height / 2);
            Vector3 v1 = new Vector3(center.X + width / 2, center.Y, center.Z - height / 2);
            Vector3 v2 = new Vector3(center.X + width / 2, center.Y, center.Z + height / 2);
            Vector3 v3 = new Vector3(center.X - width / 2, center.Y, center.Z + height / 2);
            AddLine(v0, v1, uintColor, depthTest);
            AddLine(v1, v2, uintColor, depthTest);
            AddLine(v2, v3, uintColor, depthTest);
            AddLine(v3, v0, uintColor, depthTest);
        }

        public unsafe void Render()
        {
            if(!HasContent())
                return;
            /*
            Graphics graphics = GetSubsystem<Graphics>();
            // Engine does not render when window is closed or device is lost
            System.Diagnostics.Debug.Assert(graphics && graphics.IsInitialized);

            int numVertices = (lines_.Count + noDepthLines_.Count) * 2 + (triangles_.Count + noDepthTriangles_.Count) * 3;

            var decl = PosColorVertex.Layout;

            if(TransientVertexBuffer.GetAvailableSpace(numVertices, decl) < numVertices)
                return;

            TransientVertexBuffer vertex_buffer = new TransientVertexBuffer(numVertices, decl);

            float* dest = (float*)vertex_buffer.Data;

            for(int i = 0; i < lines_.Count; ++i)
            {
                ref DebugLine line = ref lines_.At(i);

                dest[0] = line.start_.X;
                dest[1] = line.start_.Y;
                dest[2] = line.start_.Z;
                *(uint*)(&dest[3]) = (uint)line.color_;

                dest[4] = line.end_.X;
                dest[5] = line.end_.Y;
                dest[6] = line.end_.Z;
                *(uint*)(&dest[7]) = (uint)line.color_;

                dest += 8;
            }


            for(int i = 0; i < noDepthLines_.Count; ++i)
            {
                ref DebugLine line = ref noDepthLines_.At(i);

                dest[0] = line.start_.X;
                dest[1] = line.start_.Y;
                dest[2] = line.start_.Z;
                *(uint*)(&dest[3]) = (uint)line.color_;
                dest[4] = line.end_.X;
                dest[5] = line.end_.Y;
                dest[6] = line.end_.Z;
                *(uint*)(&dest[7]) = (uint)line.color_;

                dest += 8;
            }

            for(int i = 0; i < triangles_.Count; ++i)
            {
                ref DebugTriangle triangle = ref triangles_.At(i);

                dest[0] = triangle.v1_.X;
                dest[1] = triangle.v1_.Y;
                dest[2] = triangle.v1_.Z;
                *(uint*)(&dest[3]) = (uint)triangle.color_;

                dest[4] = triangle.v2_.X;
                dest[5] = triangle.v2_.Y;
                dest[6] = triangle.v2_.Z;
                *(uint*)(&dest[7]) = (uint)triangle.color_;

                dest[8] = triangle.v3_.X;
                dest[9] = triangle.v3_.Y;
                dest[10] = triangle.v3_.Z;
                *(uint*)(&dest[11]) = (uint)triangle.color_;

                dest += 12;
            }

            for(int i = 0; i < noDepthTriangles_.Count; ++i)
            {
                ref DebugTriangle triangle = ref noDepthTriangles_.At(i);

                dest[0] = triangle.v1_.X;
                dest[1] = triangle.v1_.Y;
                dest[2] = triangle.v1_.Z;
                *(uint*)(&dest[3]) = (uint)triangle.color_;

                dest[4] = triangle.v2_.X;
                dest[5] = triangle.v2_.Y;
                dest[6] = triangle.v2_.Z;
                *(uint*)(&dest[7]) = (uint)triangle.color_;

                dest[8] = triangle.v3_.X;
                dest[9] = triangle.v3_.Y;
                dest[10] = triangle.v3_.Z;
                *(uint*)(&dest[11]) = (uint)triangle.color_;

                dest += 12;
            }

            RenderState state = RenderState.ColorWrite |
            RenderState.AlphaWrite |
            RenderState.DepthWrite |
            RenderState.NoCulling |
            (lineAntiAlias_ ? RenderState.LineAA : 0) |
            RenderState.Multisampling;


            int start = 0;
            int count = 0;

            if(lines_.Count > 0)
            {
                count = lines_.Count * 2;
                graphics.DrawTransient(
                    254, 
                    vertex_buffer,
                    Matrix.Identity, 
                    start, 
                    count,
                    state | RenderState.DepthTestLessEqual | RenderState.PrimitiveLines,
                    uiShaderInstance_
                );
                start += count;
            }

            if(noDepthLines_.Count > 0)
            {
                count = noDepthLines_.Count * 2;
                graphics.DrawTransient(
                    254,
                    vertex_buffer,
                    Matrix.Identity,
                    start,
                    count,
                    state | RenderState.DepthTestAlways | RenderState.PrimitiveLines,
                    uiShaderInstance_
                );

                start += count;
            }

            state = RenderState.ColorWrite |
            RenderState.AlphaWrite |
            RenderState.BlendAlpha |
            RenderState.NoCulling |
            (lineAntiAlias_ ? RenderState.LineAA : 0) |
            RenderState.Multisampling;

            if(triangles_.Count > 0)
            {
                count = triangles_.Count * 3;

                graphics.DrawTransient(
                    254,
                    vertex_buffer,
                    Matrix.Identity,
                    start,
                    (int)count,
                    state | RenderState.DepthTestLessEqual | RenderState.PrimitiveTriangles,
                    uiShaderInstance_
                );
                start += count;
            }

            if(noDepthTriangles_.Count > 0)
            {
                count = noDepthTriangles_.Count * 3;
                graphics.DrawTransient(
                    254,
                    vertex_buffer,
                    Matrix.Identity,
                    start,
                    (int)count,
                    state | RenderState.DepthTestAlways | RenderState.PrimitiveTriangles,
                    uiShaderInstance_
                );
            }
            */

        }

        bool IsInside(ref BoundingBox box)
        {
            return frustum_.Contains(ref box) == Intersection.InSide;
        }

        bool HasContent()
        {
            return !(lines_.Count == 0 && noDepthLines_.Count == 0 && triangles_.Count == 0 && noDepthTriangles_.Count == 0);
        }

        void HandleEndFrame(ref EndFrame eventData)
        {
            // When the amount of debug geometry is reduced, release memory
            int linesSize = lines_.Count;
            int noDepthLinesSize = noDepthLines_.Count;
            int trianglesSize = triangles_.Count;
            int noDepthTrianglesSize = noDepthTriangles_.Count;

            lines_.Clear();
            noDepthLines_.Clear();
            triangles_.Clear();
            noDepthTriangles_.Clear();
        }





    }
}
