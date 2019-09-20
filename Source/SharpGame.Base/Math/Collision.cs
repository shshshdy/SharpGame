// Copyright (c) 2018-2022 SharpGame
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// -----------------------------------------------------------------------------
// Original code from SlimMath project. http://code.google.com/p/slimmath/
// Greetings to SlimDX Group. Original code published with the following license:
// -----------------------------------------------------------------------------
/*
* Copyright (c) 2007-2011 SlimDX Group
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;

namespace SharpGame
{
    public enum Intersection
    {
        /// <summary>
        /// The two bounding volumes don't intersect at all.
        /// </summary>
        OutSide,
        /// <summary>
        /// The two bounding volumes overlap.
        /// </summary>
        Intersects,
        /// <summary>
        /// One bounding volume completely contains another.
        /// </summary>
        InSide,
    };

    /// <summary>
    /// Describes the result of an intersection with a plane in three dimensions.
    /// </summary>
    public enum PlaneIntersectionType
    {
        /// <summary>
        /// The object is behind the plane.
        /// </summary>
        Back,

        /// <summary>
        /// The object is in front of the plane.
        /// </summary>
        Front,

        /// <summary>
        /// The object is intersecting the plane.
        /// </summary>
        Intersecting
    };

    /*
     * This class is organized so that the least complex objects come first so that the least
     * complex objects will have the most methods in most cases. Note that not all shapes exist
     * at this time and not all shapes have a corresponding struct. Only the objects that have
     * a corresponding struct should come first in naming and in parameter order. The order of
     * complexity is as follows:
     * 
     * 1. Point
     * 2. Ray
     * 3. Segment
     * 4. Plane
     * 5. Triangle
     * 6. Polygon
     * 7. Box
     * 8. Sphere
     * 9. Ellipsoid
     * 10. Cylinder
     * 11. Cone
     * 12. Capsule
     * 13. Torus
     * 14. Polyhedron
     * 15. Frustum
    */

    /// <summary>
    /// Contains static methods to help in determining intersections, containment, etc.
    /// </summary>
    public static class Collision
    {
        /// <summary>
        /// Determines the closest point between a point and a triangle.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <param name="vertex1">The first vertex to test.</param>
        /// <param name="vertex2">The second vertex to test.</param>
        /// <param name="vertex3">The third vertex to test.</param>
        /// <param name="result">When the method completes, contains the closest point between the two objects.</param>
        public static void ClosestPointPointTriangle(in vec3 point, in vec3 vertex1, in vec3 vertex2, in vec3 vertex3, out vec3 result)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 136

            //Check if P in vertex region outside A
            vec3 ab = vertex2 - vertex1;
            vec3 ac = vertex3 - vertex1;
            vec3 ap = point - vertex1;

            float d1 = vec3.Dot(ab, ap);
            float d2 = vec3.Dot(ac, ap);
            if (d1 <= 0.0f && d2 <= 0.0f)
            {
                result = vertex1; //Barycentric coordinates (1,0,0)
                return;
            }

            //Check if P in vertex region outside B
            vec3 bp = point - vertex2;
            float d3 = vec3.Dot(ab, bp);
            float d4 = vec3.Dot(ac, bp);
            if (d3 >= 0.0f && d4 <= d3)
            {
                result = vertex2; // Barycentric coordinates (0,1,0)
                return;
            }

            //Check if P in edge region of AB, if so return projection of P onto AB
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                float v = d1 / (d1 - d3);
                result = vertex1 + v * ab; //Barycentric coordinates (1-v,v,0)
                return;
            }

            //Check if P in vertex region outside C
            vec3 cp = point - vertex3;
            float d5 = vec3.Dot(ab, cp);
            float d6 = vec3.Dot(ac, cp);
            if (d6 >= 0.0f && d5 <= d6)
            {
                result = vertex3; //Barycentric coordinates (0,0,1)
                return;
            }

            //Check if P in edge region of AC, if so return projection of P onto AC
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                float w = d2 / (d2 - d6);
                result = vertex1 + w * ac; //Barycentric coordinates (1-w,0,w)
                return;
            }

            //Check if P in edge region of BC, if so return projection of P onto BC
            float va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                result = vertex2 + w * (vertex3 - vertex2); //Barycentric coordinates (0,1-w,w)
                return;
            }

            //P inside face region. Compute Q through its Barycentric coordinates (u,v,w)
            float denom = 1.0f / (va + vb + vc);
            float v2 = vb * denom;
            float w2 = vc * denom;
            result = vertex1 + ab * v2 + ac * w2; //= u*vertex1 + v*vertex2 + w*vertex3, u = va * denom = 1.0f - v - w
        }

        /// <summary>
        /// Determines the closest point between a <see cref="Plane"/> and a point.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <param name="point">The point to test.</param>
        /// <param name="result">When the method completes, contains the closest point between the two objects.</param>
        public static void ClosestPointPlanePoint(in Plane plane, in vec3 point, out vec3 result)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 126

            float dot;
            vec3.Dot(in plane.normal, in point, out dot);
            float t = dot - plane.d;

            result = point - (t * plane.normal);
        }

        /// <summary>
        /// Determines the closest point between a <see cref="BoundingBox"/> and a point.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <param name="point">The point to test.</param>
        /// <param name="result">When the method completes, contains the closest point between the two objects.</param>
        public static void ClosestPointBoxPoint(in BoundingBox box, in vec3 point, out vec3 result)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 130

            vec3 temp;
            glm.max(in point, in box.Minimum, out temp);
            glm.min(in temp, in box.Maximum, out result);
        }

        /// <summary>
        /// Determines the closest point between a <see cref="Sphere"/> and a point.
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="point">The point to test.</param>
        /// <param name="result">When the method completes, contains the closest point between the two objects;
        /// or, if the point is directly in the center of the sphere, contains <see cref="vec3.Zero"/>.</param>
        public static void ClosestPointSpherePoint(in Sphere sphere, in vec3 point, out vec3 result)
        {
            //Source: Jorgy343
            //Reference: None

            //Get the unit direction from the sphere's center to the point.
            vec3.Subtract(in point, in sphere.center, out result);
            result.Normalize();

            //Multiply the unit direction by the sphere's radius to get a vector
            //the length of the sphere.
            result *= sphere.radius;

            //Add the sphere's center to the direction to get a point on the sphere.
            result += sphere.center;
        }

        /// <summary>
        /// Determines the closest point between a <see cref="Sphere"/> and a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere1">The first sphere to test.</param>
        /// <param name="sphere2">The second sphere to test.</param>
        /// <param name="result">When the method completes, contains the closest point between the two objects;
        /// or, if the point is directly in the center of the sphere, contains <see cref="vec3.Zero"/>.</param>
        /// <remarks>
        /// If the two spheres are overlapping, but not directly on top of each other, the closest point
        /// is the 'closest' point of intersection. This can also be considered is the deepest point of
        /// intersection.
        /// </remarks>
        public static void ClosestPointSphereSphere(in Sphere sphere1, in Sphere sphere2, out vec3 result)
        {
            //Source: Jorgy343
            //Reference: None

            //Get the unit direction from the first sphere's center to the second sphere's center.
            vec3.Subtract(in sphere2.center, in sphere1.center, out result);
            result.Normalize();

            //Multiply the unit direction by the first sphere's radius to get a vector
            //the length of the first sphere.
            result *= sphere1.radius;

            //Add the first sphere's center to the direction to get a point on the first sphere.
            result += sphere1.center;
        }

        /// <summary>
        /// Determines the distance between a <see cref="Plane"/> and a point.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>The distance between the two objects.</returns>
        public static float DistancePlanePoint(in Plane plane, in vec3 point)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 127

            float dot;
            vec3.Dot(in plane.normal, in point, out dot);
            return dot - plane.d;
        }

        /// <summary>
        /// Determines the distance between a <see cref="BoundingBox"/> and a point.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>The distance between the two objects.</returns>
        public static float DistanceBoxPoint(in BoundingBox box, in vec3 point)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 131

            float distance = 0f;

            if (point.X < box.Minimum.X)
                distance += (box.Minimum.X - point.X) * (box.Minimum.X - point.X);
            if (point.X > box.Maximum.X)
                distance += (point.X - box.Maximum.X) * (point.X - box.Maximum.X);

            if (point.Y < box.Minimum.Y)
                distance += (box.Minimum.Y - point.Y) * (box.Minimum.Y - point.Y);
            if (point.Y > box.Maximum.Y)
                distance += (point.Y - box.Maximum.Y) * (point.Y - box.Maximum.Y);

            if (point.Z < box.Minimum.Z)
                distance += (box.Minimum.Z - point.Z) * (box.Minimum.Z - point.Z);
            if (point.Z > box.Maximum.Z)
                distance += (point.Z - box.Maximum.Z) * (point.Z - box.Maximum.Z);

            return (float)Math.Sqrt(distance);
        }

        /// <summary>
        /// Determines the distance between a <see cref="BoundingBox"/> and a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box1">The first box to test.</param>
        /// <param name="box2">The second box to test.</param>
        /// <returns>The distance between the two objects.</returns>
        public static float DistanceBoxBox(in BoundingBox box1, in BoundingBox box2)
        {
            //Source:
            //Reference:

            float distance = 0f;

            //Distance for X.
            if (box1.Minimum.X > box2.Maximum.X)
            {
                float delta = box2.Maximum.X - box1.Minimum.X;
                distance += delta * delta;
            }
            else if (box2.Minimum.X > box1.Maximum.X)
            {
                float delta = box1.Maximum.X - box2.Minimum.X;
                distance += delta * delta;
            }

            //Distance for Y.
            if (box1.Minimum.Y > box2.Maximum.Y)
            {
                float delta = box2.Maximum.Y - box1.Minimum.Y;
                distance += delta * delta;
            }
            else if (box2.Minimum.Y > box1.Maximum.Y)
            {
                float delta = box1.Maximum.Y - box2.Minimum.Y;
                distance += delta * delta;
            }

            //Distance for Z.
            if (box1.Minimum.Z > box2.Maximum.Z)
            {
                float delta = box2.Maximum.Z - box1.Minimum.Z;
                distance += delta * delta;
            }
            else if (box2.Minimum.Z > box1.Maximum.Z)
            {
                float delta = box1.Maximum.Z - box2.Minimum.Z;
                distance += delta * delta;
            }

            return (float)Math.Sqrt(distance);
        }

        /// <summary>
        /// Determines the distance between a <see cref="Sphere"/> and a point.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>The distance between the two objects.</returns>
        public static float DistanceSpherePoint(in Sphere sphere, in vec3 point)
        {
            //Source: Jorgy343
            //Reference: None

            float distance;
            vec3.Distance(in sphere.center, in point, out distance);
            distance -= sphere.radius;

            return Math.Max(distance, 0f);
        }

        /// <summary>
        /// Determines the distance between a <see cref="Sphere"/> and a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere1">The first sphere to test.</param>
        /// <param name="sphere2">The second sphere to test.</param>
        /// <returns>The distance between the two objects.</returns>
        public static float DistanceSphereSphere(in Sphere sphere1, in Sphere sphere2)
        {
            //Source: Jorgy343
            //Reference: None

            float distance;
            vec3.Distance(in sphere1.center, in sphere2.center, out distance);
            distance -= sphere1.radius + sphere2.radius;

            return Math.Max(distance, 0f);
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a point.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>Whether the two objects intersect.</returns>
        public static bool RayIntersectsPoint(in Ray ray, in vec3 point)
        {
            //Source: RayIntersectsSphere
            //Reference: None

            vec3 m;
            vec3.Subtract(in ray.origin, in point, out m);

            //Same thing as RayIntersectsSphere except that the radius of the sphere (point)
            //is the epsilon for zero.
            float b = vec3.Dot(m, ray.direction);
            float c = vec3.Dot(m, m) - MathUtil.Epsilon;

            if (c > 0f && b > 0f)
                return false;

            float discriminant = b * b - c;

            if (discriminant < 0f)
                return false;

            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="Ray"/>.
        /// </summary>
        /// <param name="ray1">The first ray to test.</param>
        /// <param name="ray2">The second ray to test.</param>
        /// <param name="point">When the method completes, contains the point of intersection,
        /// or <see cref="vec3.Zero"/> if there was no intersection.</param>
        /// <returns>Whether the two objects intersect.</returns>
        /// <remarks>
        /// This method performs a ray vs ray intersection test based on the following formula
        /// from Goldman.
        /// <code>s = det([o_2 - o_1, d_2, d_1 x d_2]) / ||d_1 x d_2||^2</code>
        /// <code>t = det([o_2 - o_1, d_1, d_1 x d_2]) / ||d_1 x d_2||^2</code>
        /// Where o_1 is the position of the first ray, o_2 is the position of the second ray,
        /// d_1 is the normalized direction of the first ray, d_2 is the normalized direction
        /// of the second ray, det denotes the determinant of a matrix, x denotes the cross
        /// product, [ ] denotes a matrix, and || || denotes the length or magnitude of a vector.
        /// </remarks>
        public static bool RayIntersectsRay(in Ray ray1, in Ray ray2, out vec3 point)
        {
            //Source: float-Time Rendering, Third Edition
            //Reference: Page 780

            vec3 cross;

            vec3.Cross(in ray1.direction, in ray2.direction, out cross);
            float denominator = cross.Length();

            //Lines are parallel.
            if (MathUtil.IsZero(denominator))
            {
                //Lines are parallel and on top of each other.
                if (MathUtil.NearEqual(ray2.origin.X, ray1.origin.X) &&
                    MathUtil.NearEqual(ray2.origin.Y, ray1.origin.Y) &&
                    MathUtil.NearEqual(ray2.origin.Z, ray1.origin.Z))
                {
                    point = vec3.Zero;
                    return true;
                }
            }

            denominator = denominator * denominator;

            //3x3 matrix for the first ray.
            float m11 = ray2.origin.X - ray1.origin.X;
            float m12 = ray2.origin.Y - ray1.origin.Y;
            float m13 = ray2.origin.Z - ray1.origin.Z;
            float m21 = ray2.direction.X;
            float m22 = ray2.direction.Y;
            float m23 = ray2.direction.Z;
            float m31 = cross.X;
            float m32 = cross.Y;
            float m33 = cross.Z;

            //Determinant of first matrix.
            float dets =
                m11 * m22 * m33 +
                m12 * m23 * m31 +
                m13 * m21 * m32 -
                m11 * m23 * m32 -
                m12 * m21 * m33 -
                m13 * m22 * m31;

            //3x3 matrix for the second ray.
            m21 = ray1.direction.X;
            m22 = ray1.direction.Y;
            m23 = ray1.direction.Z;

            //Determinant of the second matrix.
            float dett =
                m11 * m22 * m33 +
                m12 * m23 * m31 +
                m13 * m21 * m32 -
                m11 * m23 * m32 -
                m12 * m21 * m33 -
                m13 * m22 * m31;

            //t values of the point of intersection.
            float s = dets / denominator;
            float t = dett / denominator;

            //The points of intersection.
            vec3 point1 = ray1.origin + (s * ray1.direction);
            vec3 point2 = ray2.origin + (t * ray2.direction);

            //If the points are not equal, no intersection has occurred.
            if (!MathUtil.NearEqual(point2.X, point1.X) ||
                !MathUtil.NearEqual(point2.Y, point1.Y) ||
                !MathUtil.NearEqual(point2.Z, point1.Z))
            {
                point = vec3.Zero;
                return false;
            }

            point = point1;
            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="plane">The plane to test.</param>
        /// <param name="distance">When the method completes, contains the distance of the intersection,
        /// or 0 if there was no intersection.</param>
        /// <returns>Whether the two objects intersect.</returns>
        public static bool RayIntersectsPlane(in Ray ray, in Plane plane, out float distance)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 175

            float direction;
            vec3.Dot(in plane.normal, in ray.direction, out direction);

            if (MathUtil.IsZero(direction))
            {
                distance = 0f;
                return false;
            }

            float position;
            vec3.Dot(in plane.normal, in ray.origin, out position);
            distance = (-plane.d - position) / direction;

            if (distance < 0f)
            {
                distance = 0f;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="plane">The plane to test</param>
        /// <param name="point">When the method completes, contains the point of intersection,
        /// or <see cref="vec3.Zero"/> if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool RayIntersectsPlane(in Ray ray, in Plane plane, out vec3 point)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 175

            float distance;
            if (!RayIntersectsPlane(in ray, in plane, out distance))
            {
                point = vec3.Zero;
                return false;
            }

            point = ray.origin + (ray.direction * distance);
            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a triangle.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <param name="distance">When the method completes, contains the distance of the intersection,
        /// or 0 if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        /// <remarks>
        /// This method tests if the ray intersects either the front or back of the triangle.
        /// If the ray is parallel to the triangle's plane, no intersection is assumed to have
        /// happened. If the intersection of the ray and the triangle is behind the origin of
        /// the ray, no intersection is assumed to have happened. In both cases of assumptions,
        /// this method returns false.
        /// </remarks>
        public static bool RayIntersectsTriangle(in Ray ray, in vec3 vertex1, in vec3 vertex2, in vec3 vertex3, out float distance)
        {
            //Source: Fast Minimum Storage Ray / Triangle Intersection
            //Reference: http://www.cs.virginia.edu/~gfx/Courses/2003/ImageSynthesis/papers/Acceleration/Fast%20MinimumStorage%20RayTriangle%20Intersection.pdf

            //Compute vectors along two edges of the triangle.
            vec3 edge1, edge2;

            //Edge 1
            edge1.x = vertex2.X - vertex1.X;
            edge1.y = vertex2.Y - vertex1.Y;
            edge1.z = vertex2.Z - vertex1.Z;

            //Edge2
            edge2.x = vertex3.X - vertex1.X;
            edge2.y = vertex3.Y - vertex1.Y;
            edge2.z = vertex3.Z - vertex1.Z;

            //Cross product of ray direction and edge2 - first part of determinant.
            vec3 directioncrossedge2;
            directioncrossedge2.x = (ray.direction.Y * edge2.Z) - (ray.direction.Z * edge2.Y);
            directioncrossedge2.y = (ray.direction.Z * edge2.X) - (ray.direction.X * edge2.Z);
            directioncrossedge2.z = (ray.direction.X * edge2.Y) - (ray.direction.Y * edge2.X);

            //Compute the determinant.
            float determinant;
            //Dot product of edge1 and the first part of determinant.
            determinant = (edge1.X * directioncrossedge2.X) + (edge1.Y * directioncrossedge2.Y) + (edge1.Z * directioncrossedge2.Z);

            //If the ray is parallel to the triangle plane, there is no collision.
            //This also means that we are not culling, the ray may hit both the
            //back and the front of the triangle.
            if (MathUtil.IsZero(determinant))
            {
                distance = 0f;
                return false;
            }

            float inversedeterminant = 1.0f / determinant;

            //Calculate the U parameter of the intersection point.
            vec3 distanceVector;
            distanceVector.x = ray.origin.X - vertex1.X;
            distanceVector.y = ray.origin.Y - vertex1.Y;
            distanceVector.z = ray.origin.Z - vertex1.Z;

            float triangleU;
            triangleU = (distanceVector.X * directioncrossedge2.X) + (distanceVector.Y * directioncrossedge2.Y) + (distanceVector.Z * directioncrossedge2.Z);
            triangleU *= inversedeterminant;

            //Make sure it is inside the triangle.
            if (triangleU < 0f || triangleU > 1f)
            {
                distance = 0f;
                return false;
            }

            //Calculate the V parameter of the intersection point.
            vec3 distancecrossedge1;
            distancecrossedge1.x = (distanceVector.Y * edge1.Z) - (distanceVector.Z * edge1.Y);
            distancecrossedge1.y = (distanceVector.Z * edge1.X) - (distanceVector.X * edge1.Z);
            distancecrossedge1.z = (distanceVector.X * edge1.Y) - (distanceVector.Y * edge1.X);

            float triangleV;
            triangleV = ((ray.direction.X * distancecrossedge1.X) + (ray.direction.Y * distancecrossedge1.Y)) + (ray.direction.Z * distancecrossedge1.Z);
            triangleV *= inversedeterminant;

            //Make sure it is inside the triangle.
            if (triangleV < 0f || triangleU + triangleV > 1f)
            {
                distance = 0f;
                return false;
            }

            //Compute the distance along the ray to the triangle.
            float raydistance;
            raydistance = (edge2.X * distancecrossedge1.X) + (edge2.Y * distancecrossedge1.Y) + (edge2.Z * distancecrossedge1.Z);
            raydistance *= inversedeterminant;

            //Is the triangle behind the ray origin?
            if (raydistance < 0f)
            {
                distance = 0f;
                return false;
            }

            distance = raydistance;
            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a triangle.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <param name="point">When the method completes, contains the point of intersection,
        /// or <see cref="vec3.Zero"/> if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool RayIntersectsTriangle(in Ray ray, in vec3 vertex1, in vec3 vertex2, in vec3 vertex3, out vec3 point)
        {
            float distance;
            if (!RayIntersectsTriangle(in ray, in vertex1, in vertex2, in vertex3, out distance))
            {
                point = vec3.Zero;
                return false;
            }

            point = ray.origin + (ray.direction * distance);
            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="box">The box to test.</param>
        /// <param name="distance">When the method completes, contains the distance of the intersection,
        /// or 0 if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool RayIntersectsBox(in Ray ray, in BoundingBox box, out float distance)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 179

            distance = 0f;
            float tmax = float.MaxValue;

            if (MathUtil.IsZero(ray.direction.X))
            {
                if (ray.origin.X < box.Minimum.X || ray.origin.X > box.Maximum.X)
                {
                    distance = 0f;
                    return false;
                }
            }
            else
            {
                float inverse = 1.0f / ray.direction.X;
                float t1 = (box.Minimum.X - ray.origin.X) * inverse;
                float t2 = (box.Maximum.X - ray.origin.X) * inverse;

                if (t1 > t2)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                distance = Math.Max(t1, distance);
                tmax = Math.Min(t2, tmax);

                if (distance > tmax)
                {
                    distance = 0f;
                    return false;
                }
            }

            if (MathUtil.IsZero(ray.direction.Y))
            {
                if (ray.origin.Y < box.Minimum.Y || ray.origin.Y > box.Maximum.Y)
                {
                    distance = 0f;
                    return false;
                }
            }
            else
            {
                float inverse = 1.0f / ray.direction.Y;
                float t1 = (box.Minimum.Y - ray.origin.Y) * inverse;
                float t2 = (box.Maximum.Y - ray.origin.Y) * inverse;

                if (t1 > t2)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                distance = Math.Max(t1, distance);
                tmax = Math.Min(t2, tmax);

                if (distance > tmax)
                {
                    distance = 0f;
                    return false;
                }
            }

            if (MathUtil.IsZero(ray.direction.Z))
            {
                if (ray.origin.Z < box.Minimum.Z || ray.origin.Z > box.Maximum.Z)
                {
                    distance = 0f;
                    return false;
                }
            }
            else
            {
                float inverse = 1.0f / ray.direction.Z;
                float t1 = (box.Minimum.Z - ray.origin.Z) * inverse;
                float t2 = (box.Maximum.Z - ray.origin.Z) * inverse;

                if (t1 > t2)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                distance = Math.Max(t1, distance);
                tmax = Math.Min(t2, tmax);

                if (distance > tmax)
                {
                    distance = 0f;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="box">The box to test.</param>
        /// <param name="point">When the method completes, contains the point of intersection,
        /// or <see cref="vec3.Zero"/> if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool RayIntersectsBox(in Ray ray, in BoundingBox box, out vec3 point)
        {
            float distance;
            if (!RayIntersectsBox(in ray, in box, out distance))
            {
                point = vec3.Zero;
                return false;
            }

            point = ray.origin + (ray.direction * distance);
            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="sphere">The sphere to test.</param>
        /// <param name="distance">When the method completes, contains the distance of the intersection,
        /// or 0 if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool RayIntersectsSphere(in Ray ray, in Sphere sphere, out float distance)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 177

            vec3 m;
            vec3.Subtract(in ray.origin, in sphere.center, out m);

            float b = vec3.Dot(m, ray.direction);
            float c = vec3.Dot(m, m) - (sphere.radius * sphere.radius);

            if (c > 0f && b > 0f)
            {
                distance = 0f;
                return false;
            }

            float discriminant = b * b - c;

            if (discriminant < 0f)
            {
                distance = 0f;
                return false;
            }

            distance = -b - (float)Math.Sqrt(discriminant);

            if (distance < 0f)
                distance = 0f;

            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="Sphere"/>. 
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="sphere">The sphere to test.</param>
        /// <param name="point">When the method completes, contains the point of intersection,
        /// or <see cref="vec3.Zero"/> if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool RayIntersectsSphere(in Ray ray, in Sphere sphere, out vec3 point)
        {
            float distance;
            if (!RayIntersectsSphere(in ray, in sphere, out distance))
            {
                point = vec3.Zero;
                return false;
            }

            point = ray.origin + (ray.direction * distance);
            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Plane"/> and a point.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static PlaneIntersectionType PlaneIntersectsPoint(in Plane plane, in vec3 point)
        {
            float distance;
            vec3.Dot(in plane.normal, in point, out distance);
            distance += plane.d;

            if (distance > 0f)
                return PlaneIntersectionType.Front;

            if (distance < 0f)
                return PlaneIntersectionType.Back;

            return PlaneIntersectionType.Intersecting;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Plane"/> and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane1">The first plane to test.</param>
        /// <param name="plane2">The second plane to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool PlaneIntersectsPlane(in Plane plane1, in Plane plane2)
        {
            vec3 direction;
            vec3.Cross(in plane1.normal, in plane2.normal, out direction);

            //If direction is the zero vector, the planes are parallel and possibly
            //coincident. It is not an intersection. The dot product will tell us.
            float denominator;
            vec3.Dot(in direction, in direction, out denominator);

            if (MathUtil.IsZero(denominator))
                return false;

            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Plane"/> and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane1">The first plane to test.</param>
        /// <param name="plane2">The second plane to test.</param>
        /// <param name="line">When the method completes, contains the line of intersection
        /// as a <see cref="Ray"/>, or a zero ray if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        /// <remarks>
        /// Although a ray is set to have an origin, the ray returned by this method is really
        /// a line in three dimensions which has no real origin. The ray is considered valid when
        /// both the positive direction is used and when the negative direction is used.
        /// </remarks>
        public static bool PlaneIntersectsPlane(in Plane plane1, in Plane plane2, out Ray line)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 207

            vec3 direction;
            vec3.Cross(in plane1.normal, in plane2.normal, out direction);

            //If direction is the zero vector, the planes are parallel and possibly
            //coincident. It is not an intersection. The dot product will tell us.
            float denominator;
            vec3.Dot(in direction, in direction, out denominator);

            //We assume the planes are normalized, therefore the denominator
            //only serves as a parallel and coincident check. Otherwise we need
            //to divide the point by the denominator.
            if (MathUtil.IsZero(denominator))
            {
                line = new Ray();
                return false;
            }

            vec3 point;
            vec3 temp = plane1.d * plane2.normal - plane2.d * plane1.normal;
            vec3.Cross(in temp, in direction, out point);

            line.origin = point;
            line.direction = direction;
            line.direction.Normalize();

            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Plane"/> and a triangle.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static PlaneIntersectionType PlaneIntersectsTriangle(in Plane plane, in vec3 vertex1, in vec3 vertex2, in vec3 vertex3)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 207

            PlaneIntersectionType test1 = PlaneIntersectsPoint(in plane, in vertex1);
            PlaneIntersectionType test2 = PlaneIntersectsPoint(in plane, in vertex2);
            PlaneIntersectionType test3 = PlaneIntersectsPoint(in plane, in vertex3);

            if (test1 == PlaneIntersectionType.Front && test2 == PlaneIntersectionType.Front && test3 == PlaneIntersectionType.Front)
                return PlaneIntersectionType.Front;

            if (test1 == PlaneIntersectionType.Back && test2 == PlaneIntersectionType.Back && test3 == PlaneIntersectionType.Back)
                return PlaneIntersectionType.Back;

            return PlaneIntersectionType.Intersecting;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Plane"/> and a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <param name="box">The box to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static PlaneIntersectionType PlaneIntersectsBox(in Plane plane, in BoundingBox box)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 161

            vec3 min;
            vec3 max;

            max.x = (plane.normal.X >= 0.0f) ? box.Minimum.X : box.Maximum.X;
            max.y = (plane.normal.Y >= 0.0f) ? box.Minimum.Y : box.Maximum.Y;
            max.z = (plane.normal.Z >= 0.0f) ? box.Minimum.Z : box.Maximum.Z;
            min.x = (plane.normal.X >= 0.0f) ? box.Maximum.X : box.Minimum.X;
            min.y = (plane.normal.Y >= 0.0f) ? box.Maximum.Y : box.Minimum.Y;
            min.z = (plane.normal.Z >= 0.0f) ? box.Maximum.Z : box.Minimum.Z;

            float distance;
            vec3.Dot(in plane.normal, in max, out distance);

            if (distance + plane.d > 0.0f)
                return PlaneIntersectionType.Front;

            distance = vec3.Dot(plane.normal, min);

            if (distance + plane.d < 0.0f)
                return PlaneIntersectionType.Back;

            return PlaneIntersectionType.Intersecting;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Plane"/> and a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <param name="sphere">The sphere to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static PlaneIntersectionType PlaneIntersectsSphere(in Plane plane, in Sphere sphere)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 160

            float distance;
            vec3.Dot(in plane.normal, in sphere.center, out distance);
            distance += plane.d;

            if (distance > sphere.radius)
                return PlaneIntersectionType.Front;

            if (distance < -sphere.radius)
                return PlaneIntersectionType.Back;

            return PlaneIntersectionType.Intersecting;
        }

        /* This implementation is wrong
        /// <summary>
        /// Determines whether there is an intersection between a <see cref="SharpGame.BoundingBox"/> and a triangle.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool BoxIntersectsTriangle(in BoundingBox box, in vec3 vertex1, in vec3 vertex2, in vec3 vertex3)
        {
            if (BoxContainsPoint(in box, in vertex1) == ContainmentType.Contains)
                return true;

            if (BoxContainsPoint(in box, in vertex2) == ContainmentType.Contains)
                return true;

            if (BoxContainsPoint(in box, in vertex3) == ContainmentType.Contains)
                return true;

            return false;
        }
        */

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="BoundingBox"/> and a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box1">The first box to test.</param>
        /// <param name="box2">The second box to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool BoxIntersectsBox(in BoundingBox box1, in BoundingBox box2)
        {
            if (box1.Minimum.X > box2.Maximum.X || box2.Minimum.X > box1.Maximum.X)
                return false;

            if (box1.Minimum.Y > box2.Maximum.Y || box2.Minimum.Y > box1.Maximum.Y)
                return false;

            if (box1.Minimum.Z > box2.Maximum.Z || box2.Minimum.Z > box1.Maximum.Z)
                return false;

            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="BoundingBox"/> and a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <param name="sphere">The sphere to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool BoxIntersectsSphere(in BoundingBox box, in Sphere sphere)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 166

            vec3 vector;
            glm.clamp(in sphere.center, in box.Minimum, in box.Maximum, out vector);
            float distance = vec3.DistanceSquared(sphere.center, vector);

            return distance <= sphere.radius * sphere.radius;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Sphere"/> and a triangle.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool SphereIntersectsTriangle(in Sphere sphere, in vec3 vertex1, in vec3 vertex2, in vec3 vertex3)
        {
            //Source: float-Time Collision Detection by Christer Ericson
            //Reference: Page 167

            vec3 point;
            ClosestPointPointTriangle(in sphere.center, in vertex1, in vertex2, in vertex3, out point);
            vec3 v = point - sphere.center;

            float dot;
            vec3.Dot(in v, in v, out dot);

            return dot <= sphere.radius * sphere.radius;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Sphere"/> and a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere1">First sphere to test.</param>
        /// <param name="sphere2">Second sphere to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool SphereIntersectsSphere(in Sphere sphere1, in Sphere sphere2)
        {
            float radiisum = sphere1.radius + sphere2.radius;
            return vec3.DistanceSquared(sphere1.center, sphere2.center) <= radiisum * radiisum;
        }

        /// <summary>
        /// Determines whether a <see cref="BoundingBox"/> contains a point.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public static Intersection BoxContainsPoint(in BoundingBox box, in vec3 point)
        {
            if (box.Minimum.X <= point.X && box.Maximum.X >= point.X &&
                box.Minimum.Y <= point.Y && box.Maximum.Y >= point.Y &&
                box.Minimum.Z <= point.Z && box.Maximum.Z >= point.Z)
            {
                return Intersection.InSide;
            }

            return Intersection.OutSide;
        }

        /* This implementation is wrong
        /// <summary>
        /// Determines whether a <see cref="SharpGame.BoundingBox"/> contains a triangle.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public static ContainmentType BoxContainsTriangle(in BoundingBox box, in vec3 vertex1, in vec3 vertex2, in vec3 vertex3)
        {
            ContainmentType test1 = BoxContainsPoint(in box, in vertex1);
            ContainmentType test2 = BoxContainsPoint(in box, in vertex2);
            ContainmentType test3 = BoxContainsPoint(in box, in vertex3);

            if (test1 == ContainmentType.Contains && test2 == ContainmentType.Contains && test3 == ContainmentType.Contains)
                return ContainmentType.Contains;

            if (test1 == ContainmentType.Contains || test2 == ContainmentType.Contains || test3 == ContainmentType.Contains)
                return ContainmentType.Intersects;

            return ContainmentType.Disjoint;
        }
        */

        /// <summary>
        /// Determines whether a <see cref="BoundingBox"/> contains a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box1">The first box to test.</param>
        /// <param name="box2">The second box to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public static Intersection BoxContainsBox(in BoundingBox box1, in BoundingBox box2)
        {
            if (box1.Maximum.X < box2.Minimum.X || box1.Minimum.X > box2.Maximum.X)
                return Intersection.OutSide;

            if (box1.Maximum.Y < box2.Minimum.Y || box1.Minimum.Y > box2.Maximum.Y)
                return Intersection.OutSide;

            if (box1.Maximum.Z < box2.Minimum.Z || box1.Minimum.Z > box2.Maximum.Z)
                return Intersection.OutSide;

            if (box1.Minimum.X <= box2.Minimum.X && (box2.Maximum.X <= box1.Maximum.X &&
                box1.Minimum.Y <= box2.Minimum.Y && box2.Maximum.Y <= box1.Maximum.Y) &&
                box1.Minimum.Z <= box2.Minimum.Z && box2.Maximum.Z <= box1.Maximum.Z)
            {
                return Intersection.InSide;
            }

            return Intersection.Intersects;
        }

        /// <summary>
        /// Determines whether a <see cref="BoundingBox"/> contains a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <param name="sphere">The sphere to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public static Intersection BoxContainsSphere(in BoundingBox box, in Sphere sphere)
        {
            vec3 vector;
            glm.clamp(in sphere.center, in box.Minimum, in box.Maximum, out vector);
            float distance = vec3.DistanceSquared(sphere.center, vector);

            if (distance > sphere.radius * sphere.radius)
                return Intersection.OutSide;

            if ((((box.Minimum.X + sphere.radius <= sphere.center.X) && (sphere.center.X <= box.Maximum.X - sphere.radius)) && ((box.Maximum.X - box.Minimum.X > sphere.radius) &&
                (box.Minimum.Y + sphere.radius <= sphere.center.Y))) && (((sphere.center.Y <= box.Maximum.Y - sphere.radius) && (box.Maximum.Y - box.Minimum.Y > sphere.radius)) &&
                (((box.Minimum.Z + sphere.radius <= sphere.center.Z) && (sphere.center.Z <= box.Maximum.Z - sphere.radius)) && (box.Maximum.Z - box.Minimum.Z > sphere.radius))))
            {
                return Intersection.InSide;
            }

            return Intersection.Intersects;
        }

        /// <summary>
        /// Determines whether a <see cref="Sphere"/> contains a point.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public static Intersection SphereContainsPoint(in Sphere sphere, in vec3 point)
        {
            if (vec3.DistanceSquared(point, sphere.center) <= sphere.radius * sphere.radius)
                return Intersection.InSide;

            return Intersection.OutSide;
        }

        /// <summary>
        /// Determines whether a <see cref="Sphere"/> contains a triangle.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public static Intersection SphereContainsTriangle(in Sphere sphere, in vec3 vertex1, in vec3 vertex2, in vec3 vertex3)
        {
            //Source: Jorgy343
            //Reference: None

            Intersection test1 = SphereContainsPoint(in sphere, in vertex1);
            Intersection test2 = SphereContainsPoint(in sphere, in vertex2);
            Intersection test3 = SphereContainsPoint(in sphere, in vertex3);

            if (test1 == Intersection.InSide && test2 == Intersection.InSide && test3 == Intersection.InSide)
                return Intersection.InSide;

            if (SphereIntersectsTriangle(in sphere, in vertex1, in vertex2, in vertex3))
                return Intersection.Intersects;

            return Intersection.OutSide;
        }

        /// <summary>
        /// Determines whether a <see cref="Sphere"/> contains a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <param name="box">The box to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public static Intersection SphereContainsBox(in Sphere sphere, in BoundingBox box)
        {
            vec3 vector;

            if (!BoxIntersectsSphere(in box, in sphere))
                return Intersection.OutSide;

            float radiussquared = sphere.radius * sphere.radius;
            vector.x = sphere.center.X - box.Minimum.X;
            vector.y = sphere.center.Y - box.Maximum.Y;
            vector.z = sphere.center.Z - box.Maximum.Z;

            if (vector.LengthSquared() > radiussquared)
                return Intersection.Intersects;

            vector.x = sphere.center.X - box.Maximum.X;
            vector.y = sphere.center.Y - box.Maximum.Y;
            vector.z = sphere.center.Z - box.Maximum.Z;

            if (vector.LengthSquared() > radiussquared)
                return Intersection.Intersects;

            vector.X = sphere.center.X - box.Maximum.X;
            vector.Y = sphere.center.Y - box.Minimum.Y;
            vector.Z = sphere.center.Z - box.Maximum.Z;

            if (vector.LengthSquared() > radiussquared)
                return Intersection.Intersects;

            vector.X = sphere.center.X - box.Minimum.X;
            vector.Y = sphere.center.Y - box.Minimum.Y;
            vector.Z = sphere.center.Z - box.Maximum.Z;

            if (vector.LengthSquared() > radiussquared)
                return Intersection.Intersects;

            vector.X = sphere.center.X - box.Minimum.X;
            vector.Y = sphere.center.Y - box.Maximum.Y;
            vector.Z = sphere.center.Z - box.Minimum.Z;

            if (vector.LengthSquared() > radiussquared)
                return Intersection.Intersects;

            vector.X = sphere.center.X - box.Maximum.X;
            vector.Y = sphere.center.Y - box.Maximum.Y;
            vector.Z = sphere.center.Z - box.Minimum.Z;

            if (vector.LengthSquared() > radiussquared)
                return Intersection.Intersects;

            vector.X = sphere.center.X - box.Maximum.X;
            vector.Y = sphere.center.Y - box.Minimum.Y;
            vector.Z = sphere.center.Z - box.Minimum.Z;

            if (vector.LengthSquared() > radiussquared)
                return Intersection.Intersects;

            vector.X = sphere.center.X - box.Minimum.X;
            vector.Y = sphere.center.Y - box.Minimum.Y;
            vector.Z = sphere.center.Z - box.Minimum.Z;

            if (vector.LengthSquared() > radiussquared)
                return Intersection.Intersects;

            return Intersection.InSide;
        }

        /// <summary>
        /// Determines whether a <see cref="Sphere"/> contains a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere1">The first sphere to test.</param>
        /// <param name="sphere2">The second sphere to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public static Intersection SphereContainsSphere(in Sphere sphere1, in Sphere sphere2)
        {
            float distance = vec3.Distance(sphere1.center, sphere2.center);

            if (sphere1.radius + sphere2.radius < distance)
                return Intersection.OutSide;

            if (sphere1.radius - sphere2.radius < distance)
                return Intersection.Intersects;

            return Intersection.InSide;
        }

    }
}
