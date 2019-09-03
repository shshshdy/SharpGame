using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    /// Frustum planes.
    enum FrustumPlane
    {
        PLANE_NEAR = 0,
        PLANE_LEFT,
        PLANE_RIGHT,
        PLANE_UP,
        PLANE_DOWN,
        PLANE_FAR,
    }

    public struct Frustum
    {
        const int NUM_FRUSTUM_PLANES = 6;
        const int NUM_FRUSTUM_VERTICES = 8;

        /// Frustum planes.
        public Plane plane0;
        public Plane plane1;
        public Plane plane2;
        public Plane plane3;
        public Plane plane4;
        public Plane plane5;

        /// Frustum vertices.
        public vec3 vertice0;
        public vec3 vertice1;
        public vec3 vertice2;
        public vec3 vertice3;
        public vec3 vertice4;
        public vec3 vertice5;
        public vec3 vertice6;
        public vec3 vertice7;


        static vec3 ClipEdgeZ(vec3 v0, vec3 v1, float clipZ)
        {
            return new vec3(
                v1.X + (v0.X - v1.X) * ((clipZ - v1.Z) / (v0.Z - v1.Z)),
                v1.Y + (v0.Y - v1.Y) * ((clipZ - v1.Z) / (v0.Z - v1.Z)),
                clipZ
            );
        }

        static void ProjectAndMergeEdge(vec3 v0, vec3 v1, ref RectangleF rect, ref mat4 projection)
        {
            // Check if both vertices behind near plane
            if(v0.Z < MathUtil.MinNearClip && v1.Z < MathUtil.MinNearClip)
                return;

            // Check if need to clip one of the vertices
            if(v1.Z < MathUtil.MinNearClip)
                v1 = ClipEdgeZ(v1, v0, MathUtil.MinNearClip);
            else if(v0.Z < MathUtil.MinNearClip)
                v0 = ClipEdgeZ(v0, v1, MathUtil.MinNearClip);

            // Project, perspective divide and merge
            vec3 tV0 = vec3.Transform(ref v0, ref projection);
            vec3 tV1 = vec3.Transform(ref v1, ref projection);
            rect.Merge(new vec2(tV0.X, tV0.Y));
            rect.Merge(new vec2(tV1.X, tV1.Y));
        }


        public void Define(float fov, float aspectRatio, float zoom, float nearZ, float farZ, ref mat4 transform)
        {
            nearZ = Math.Max(nearZ, 0.0f);
            farZ = Math.Max(farZ, nearZ);
            float halfViewSize = (float)Math.Tan(fov /** M_DEGTORAD_2*/) / zoom;
            vec3 near, far;

            near.z = nearZ;
            near.y = near.z * halfViewSize;
            near.x = near.y * aspectRatio;
            far.z = farZ;
            far.y = far.z * halfViewSize;
            far.x = far.y * aspectRatio;

            Define(ref near, ref far, ref transform);
        }

        public void Define(ref vec3 near, ref vec3 far, ref mat4 transform)
        {
            vertice0 = vec3.Transform(near, transform);
            vertice1 = vec3.Transform(new vec3(near.X, -near.Y, near.Z), transform);
            vertice2 = vec3.Transform(new vec3(-near.X, -near.Y, near.Z), transform);
            vertice3 = vec3.Transform(new vec3(-near.X, near.Y, near.Z), transform);
            vertice4 = vec3.Transform(far, transform);
            vertice5 = vec3.Transform(new vec3(far.X, -far.Y, far.Z), transform);
            vertice6 = vec3.Transform(new vec3(-far.X, -far.Y, far.Z), transform);
            vertice7 = vec3.Transform(new vec3(-far.X, far.Y, far.Z), transform);

            UpdatePlanes();
        }

        public void Define(ref BoundingBox box, ref mat4 transform)
        {
            vertice0 = vec3.Transform(new vec3(box.Maximum.X, box.Maximum.Y, box.Minimum.Z), transform);
            vertice1 = vec3.Transform(new vec3(box.Maximum.X, box.Minimum.Y, box.Minimum.Z), transform);
            vertice2 = vec3.Transform(new vec3(box.Minimum.X, box.Minimum.Y, box.Minimum.Z), transform);
            vertice3 = vec3.Transform(new vec3(box.Minimum.X, box.Maximum.Y, box.Minimum.Z), transform);
            vertice4 = vec3.Transform(new vec3(box.Maximum.X, box.Maximum.Y, box.Maximum.Z), transform);
            vertice5 = vec3.Transform(new vec3(box.Maximum.X, box.Minimum.Y, box.Maximum.Z), transform);
            vertice6 = vec3.Transform(new vec3(box.Minimum.X, box.Minimum.Y, box.Maximum.Z), transform);
            vertice7 = vec3.Transform(new vec3(box.Minimum.X, box.Maximum.Y, box.Maximum.Z), transform);

            UpdatePlanes();
        }

        public void Define(ref mat4 projection)
        {
            mat4 projInverse = glm.inverse(projection);

            vertice0 = vec3.Transform(new vec3(1.0f, 1.0f, 0.0f), projInverse);
            vertice1 = vec3.Transform(new vec3(1.0f, -1.0f, 0.0f), projInverse);
            vertice2 = vec3.Transform(new vec3(-1.0f, -1.0f, 0.0f), projInverse);
            vertice3 = vec3.Transform(new vec3(-1.0f, 1.0f, 0.0f), projInverse);
            vertice4 = vec3.Transform(new vec3(1.0f, 1.0f, 1.0f), projInverse);
            vertice5 = vec3.Transform(new vec3(1.0f, -1.0f, 1.0f), projInverse);
            vertice6 = vec3.Transform(new vec3(-1.0f, -1.0f, 1.0f), projInverse);
            vertice7 = vec3.Transform(new vec3(-1.0f, 1.0f, 1.0f), projInverse);

            UpdatePlanes();
        }

        public void DefineOrtho(float orthoSize, float aspectRatio, float zoom, float nearZ, float farZ, ref mat4 transform)
        {
            nearZ = Math.Max(nearZ, 0.0f);
            farZ = Math.Max(farZ, nearZ);
            float halfViewSize = orthoSize * 0.5f / zoom;
            vec3 near, far;

            near.z = nearZ;
            far.z = farZ;
            far.y = near.y = halfViewSize;
            far.x = near.x = near.y * aspectRatio;

            Define(ref near, ref far, ref transform);
        }

        public void DefineSplit(ref mat4 projection, float near, float far)
        {
            mat4 projInverse = glm.inverse(projection);

            // Figure out depth values for near & far
            vec4 nearTemp = projection*new vec4(0.0f, 0.0f, near, 1.0f);
            vec4 farTemp = projection*new vec4(0.0f, 0.0f, far, 1.0f);
            float nearZ = nearTemp.z / nearTemp.w;
            float farZ = farTemp.z / farTemp.w;

            vertice0 = vec3.Transform(new vec3(1.0f, 1.0f, nearZ), projInverse);
            vertice1 = vec3.Transform(new vec3(1.0f, -1.0f, nearZ), projInverse);
            vertice2 = vec3.Transform(new vec3(-1.0f, -1.0f, nearZ), projInverse);
            vertice3 = vec3.Transform(new vec3(-1.0f, 1.0f, nearZ), projInverse);
            vertice4 = vec3.Transform(new vec3(1.0f, 1.0f, farZ), projInverse);
            vertice5 = vec3.Transform(new vec3(1.0f, -1.0f, farZ), projInverse);
            vertice6 = vec3.Transform(new vec3(-1.0f, -1.0f, farZ), projInverse);
            vertice7 = vec3.Transform(new vec3(-1.0f, 1.0f, farZ), projInverse);

            UpdatePlanes();
        }

        public void Transform(ref mat3 transform)
        {
            vec3.Transform(ref vertice0, ref transform, out vertice0);
            vec3.Transform(ref vertice1, ref transform, out vertice1);
            vec3.Transform(ref vertice2, ref transform, out vertice2);
            vec3.Transform(ref vertice3, ref transform, out vertice3);
            vec3.Transform(ref vertice4, ref transform, out vertice4);
            vec3.Transform(ref vertice5, ref transform, out vertice5);
            vec3.Transform(ref vertice6, ref transform, out vertice6);
            vec3.Transform(ref vertice7, ref transform, out vertice7);
            UpdatePlanes();
        }

        public void Transform(ref mat4 transform)
        {
            vec3.Transform(ref vertice0, ref transform, out vertice0);
            vec3.Transform(ref vertice1, ref transform, out vertice1);
            vec3.Transform(ref vertice2, ref transform, out vertice2);
            vec3.Transform(ref vertice3, ref transform, out vertice3);
            vec3.Transform(ref vertice4, ref transform, out vertice4);
            vec3.Transform(ref vertice5, ref transform, out vertice5);
            vec3.Transform(ref vertice6, ref transform, out vertice6);
            vec3.Transform(ref vertice7, ref transform, out vertice7);
            UpdatePlanes();
        }

        public Frustum Transformed(ref mat3 transform)
        {
            Frustum transformed = new Frustum();
            vec3.Transform(ref vertice0, ref transform, out transformed.vertice0);
            vec3.Transform(ref vertice1, ref transform, out transformed.vertice1);
            vec3.Transform(ref vertice2, ref transform, out transformed.vertice2);
            vec3.Transform(ref vertice3, ref transform, out transformed.vertice3);
            vec3.Transform(ref vertice4, ref transform, out transformed.vertice4);
            vec3.Transform(ref vertice5, ref transform, out transformed.vertice5);
            vec3.Transform(ref vertice6, ref transform, out transformed.vertice6);
            vec3.Transform(ref vertice7, ref transform, out transformed.vertice7);

            transformed.UpdatePlanes();
            return transformed;
        }

        public Frustum Transformed(ref mat4 transform)
        {
            Frustum transformed = new Frustum();
            vec3.Transform(ref vertice0, ref transform, out transformed.vertice0);
            vec3.Transform(ref vertice1, ref transform, out transformed.vertice1);
            vec3.Transform(ref vertice2, ref transform, out transformed.vertice2);
            vec3.Transform(ref vertice3, ref transform, out transformed.vertice3);
            vec3.Transform(ref vertice4, ref transform, out transformed.vertice4);
            vec3.Transform(ref vertice5, ref transform, out transformed.vertice5);
            vec3.Transform(ref vertice6, ref transform, out transformed.vertice6);
            vec3.Transform(ref vertice7, ref transform, out transformed.vertice7);

            transformed.UpdatePlanes();
            return transformed;
        }

        public RectangleF Projected(ref mat4 projection)
        {
            RectangleF rect = RectangleF.Empty;

            ProjectAndMergeEdge(vertice0, vertice4, ref rect, ref projection);
            ProjectAndMergeEdge(vertice1, vertice5, ref rect, ref projection);
            ProjectAndMergeEdge(vertice2, vertice6, ref rect, ref projection);
            ProjectAndMergeEdge(vertice3, vertice7, ref rect, ref projection);
            ProjectAndMergeEdge(vertice4, vertice5, ref rect, ref projection);
            ProjectAndMergeEdge(vertice5, vertice6, ref rect, ref projection);
            ProjectAndMergeEdge(vertice6, vertice7, ref rect, ref projection);
            ProjectAndMergeEdge(vertice7, vertice4, ref rect, ref projection);

            return rect;
        }

        public void UpdatePlanes()
        {
            plane0.Define(ref vertice2, ref vertice1, ref vertice0);
            plane1.Define(ref vertice3, ref vertice7, ref vertice6);
            plane2.Define(ref vertice1, ref vertice5, ref vertice4);
            plane3.Define(ref vertice0, ref vertice4, ref vertice7);
            plane4.Define(ref vertice6, ref vertice5, ref vertice1);
            plane5.Define(ref vertice5, ref vertice6, ref vertice7);

            // Check if we ended up with inverted planes (reflected transform) and flip in that case
            if(plane0.Distance(ref vertice5) < 0.0f)
            {
                plane0.Normal = -plane0.Normal;
                plane0.D = -plane0.D;
                plane1.Normal = -plane1.Normal;
                plane1.D = -plane1.D;
                plane2.Normal = -plane2.Normal;
                plane2.D = -plane2.D;
                plane3.Normal = -plane3.Normal;
                plane3.D = -plane3.D;
                plane4.Normal = -plane4.Normal;
                plane4.D = -plane4.D;
                plane5.Normal = -plane5.Normal;
                plane5.D = -plane5.D;

            }

        }

        /// Test if a point is inside or outside.
        public Intersection IsInside(ref vec3 point)
        {
            if(plane0.Distance(ref point) < 0.0f)
                return Intersection.OutSide;
            if(plane1.Distance(ref point) < 0.0f)
                return Intersection.OutSide;
            if(plane2.Distance(ref point) < 0.0f)
                return Intersection.OutSide;
            if(plane3.Distance(ref point) < 0.0f)
                return Intersection.OutSide;
            if(plane4.Distance(ref point) < 0.0f)
                return Intersection.OutSide;
            if(plane5.Distance(ref point) < 0.0f)
                return Intersection.OutSide;
            return Intersection.InSide;
        }

        /// Test if a sphere is inside, outside or intersects.
        public Intersection IsInside(ref BoundingSphere sphere)
        {
            bool allInside = true;

            {
                float dist = plane0.Distance(ref sphere.Center);
                if(dist < -sphere.Radius)
                    return Intersection.OutSide;
                else if(dist < sphere.Radius)
                    allInside = false;
            }

            {
                float dist = plane1.Distance(ref sphere.Center);
                if(dist < -sphere.Radius)
                    return Intersection.OutSide;
                else if(dist < sphere.Radius)
                    allInside = false;
            }
            {
                float dist = plane2.Distance(ref sphere.Center);
                if(dist < -sphere.Radius)
                    return Intersection.OutSide;
                else if(dist < sphere.Radius)
                    allInside = false;
            }
            {
                float dist = plane3.Distance(ref sphere.Center);
                if(dist < -sphere.Radius)
                    return Intersection.OutSide;
                else if(dist < sphere.Radius)
                    allInside = false;
            }
            {
                float dist = plane4.Distance(ref sphere.Center);
                if(dist < -sphere.Radius)
                    return Intersection.OutSide;
                else if(dist < sphere.Radius)
                    allInside = false;
            }
            {
                float dist = plane5.Distance(ref sphere.Center);
                if(dist < -sphere.Radius)
                    return Intersection.OutSide;
                else if(dist < sphere.Radius)
                    allInside = false;
            }
            return allInside ? Intersection.InSide : Intersection.Intersects;
        }

        /// Test if a sphere if (partially) inside or outside.
        public Intersection IsInsideFast(ref BoundingSphere sphere)
        {
            if(plane0.Distance(ref sphere.Center) < -sphere.Radius)
                return Intersection.OutSide;

            if(plane1.Distance(ref sphere.Center) < -sphere.Radius)
                return Intersection.OutSide;

            if(plane2.Distance(ref sphere.Center) < -sphere.Radius)
                return Intersection.OutSide;

            if(plane3.Distance(ref sphere.Center) < -sphere.Radius)
                return Intersection.OutSide;

            if(plane4.Distance(ref sphere.Center) < -sphere.Radius)
                return Intersection.OutSide;

            if(plane5.Distance(ref sphere.Center) < -sphere.Radius)
                return Intersection.OutSide;

            return Intersection.InSide;
        }

        /// Test if a bounding box is inside, outside or intersects.
        public Intersection IsInside(ref BoundingBox box)
        {
            vec3 center = box.Center;
            vec3 edge = center - box.Minimum;
            bool allInside = true;

            {
                float dist = vec3.Dot(plane0.Normal, center) + plane0.D;
                float absDist = vec3.Dot(plane0.AbsNormal, edge);

                if(dist < -absDist)
                    return Intersection.OutSide;
                else if(dist < absDist)
                    allInside = false;
            }
            {
                float dist = vec3.Dot(plane1.Normal, center) + plane1.D;
                float absDist = vec3.Dot(plane1.AbsNormal, edge);

                if(dist < -absDist)
                    return Intersection.OutSide;
                else if(dist < absDist)
                    allInside = false;
            }
            {
                float dist = vec3.Dot(plane2.Normal, center) + plane2.D;
                float absDist = vec3.Dot(plane2.AbsNormal, edge);

                if(dist < -absDist)
                    return Intersection.OutSide;
                else if(dist < absDist)
                    allInside = false;
            }
            {
                float dist = vec3.Dot(plane3.Normal, center) + plane3.D;
                float absDist = vec3.Dot(plane3.AbsNormal, edge);

                if(dist < -absDist)
                    return Intersection.OutSide;
                else if(dist < absDist)
                    allInside = false;
            }
            {
                float dist = vec3.Dot(plane4.Normal, center) + plane4.D;
                float absDist = vec3.Dot(plane4.AbsNormal, edge);

                if(dist < -absDist)
                    return Intersection.OutSide;
                else if(dist < absDist)
                    allInside = false;
            }
            {
                float dist = vec3.Dot(plane5.Normal, center) + plane5.D;
                float absDist = vec3.Dot(plane5.AbsNormal, edge);

                if(dist < -absDist)
                    return Intersection.OutSide;
                else if(dist < absDist)
                    allInside = false;
            }
            return allInside ? Intersection.InSide : Intersection.Intersects;
        }

        /// Test if a bounding box is (partially) inside or outside.
        public Intersection IsInsideFast(ref BoundingBox box)
        {
            vec3 center = box.Center;
            vec3 edge = center - box.Minimum;
            {
                float dist = vec3.Dot(plane0.Normal, center) + plane0.D;
                float absDist = vec3.Dot(plane0.AbsNormal, edge);

                if(dist < -absDist)
                    return Intersection.OutSide;
            }
            {
                float dist = vec3.Dot(plane1.Normal, center) + plane1.D;
                float absDist = vec3.Dot(plane1.AbsNormal, edge);

                if(dist < -absDist)
                    return Intersection.OutSide;
            }
            {
                float dist = vec3.Dot(plane2.Normal, center) + plane2.D;
                float absDist = vec3.Dot(plane2.AbsNormal, edge);

                if(dist < -absDist)
                    return Intersection.OutSide;
            }
            {
                float dist = vec3.Dot(plane3.Normal, center) + plane3.D;
                float absDist = vec3.Dot(plane3.AbsNormal, edge);

                if(dist < -absDist)
                    return Intersection.OutSide;
            }
            {
                float dist = vec3.Dot(plane4.Normal, center) + plane4.D;
                float absDist = vec3.Dot(plane4.AbsNormal, edge);

                if(dist < -absDist)
                    return Intersection.OutSide;
            }
            {
                float dist = vec3.Dot(plane5.Normal, center) + plane5.D;
                float absDist = vec3.Dot(plane5.AbsNormal, edge);

                if(dist < -absDist)
                    return Intersection.OutSide;
            }

            return Intersection.InSide;
        }

        /// Return distance of a point to the frustum, or 0 if inside.
        public float Distance(ref vec3 point)
        {
            float distance = 0.0f;
            distance = Math.Max(-plane0.Distance(ref point), distance);

            return distance;
        }
    }
}
