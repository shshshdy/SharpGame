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

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGame
{
    /// <summary>
    /// OrientedBoundingBox (OBB) is a rectangular block, much like an AABB (BoundingBox) but with an arbitrary orientation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct OrientedBoundingBox : IEquatable<OrientedBoundingBox>, IFormattable
    {
        /// <summary>
        /// Half lengths of the box along each axis.
        /// </summary>
        public vec3 Extents;

        /// <summary>
        /// The matrix which aligns and scales the box, and its translation vector represents the center of the box.
        /// </summary>
        public mat4 Transformation;

        /// <summary>
        /// Creates an <see cref="OrientedBoundingBox"/> from a BoundingBox.
        /// </summary>
        /// <param name="bb">The BoundingBox to create from.</param>
        /// <remarks>
        /// Initially, the OBB is axis-aligned box, but it can be rotated and transformed later.
        /// </remarks>
        public OrientedBoundingBox(BoundingBox bb)
        {
            var Center = bb.Minimum + (bb.Maximum - bb.Minimum) / 2f;
            Extents = bb.Maximum - Center;
            Transformation = glm.translate(Center);
        }

        /// <summary>
        /// Creates an <see cref="OrientedBoundingBox"/> which contained between two minimum and maximum points.
        /// </summary>
        /// <param name="minimum">The minimum vertex of the bounding box.</param>
        /// <param name="maximum">The maximum vertex of the bounding box.</param>
        /// <remarks>
        /// Initially, the OrientedBoundingBox is axis-aligned box, but it can be rotated and transformed later.
        /// </remarks>
        public OrientedBoundingBox(vec3 minimum, vec3 maximum)
        {
            var Center = minimum + (maximum - minimum) / 2f;
            Extents = maximum - Center;
            Transformation = glm.translate(Center);
        }

        /// <summary>
        /// Creates an <see cref="OrientedBoundingBox"/> that fully contains the given points.
        /// </summary>
        /// <param name="points">The points that will be contained by the box.</param>
        /// <remarks>
        /// This method is not for computing the best tight-fitting OrientedBoundingBox.
        /// And initially, the OrientedBoundingBox is axis-aligned box, but it can be rotated and transformed later.
        /// </remarks>
        public OrientedBoundingBox(vec3[] points)
        {
            if (points == null || points.Length == 0)
                throw new ArgumentNullException("points");

            vec3 minimum = new vec3(float.MaxValue);
            vec3 maximum = new vec3(float.MinValue);

            for (int i = 0; i < points.Length; ++i)
            {
                glm.min(in minimum, in points[i], out minimum);
                glm.max(in maximum, in points[i], out maximum);
            }

            var Center = minimum + (maximum - minimum) / 2f;
            Extents = maximum - Center;
            Transformation = glm.translate(Center);
        }

        /// <summary>
        /// Retrieves the eight corners of the bounding box.
        /// </summary>
        /// <returns>An array of points representing the eight corners of the bounding box.</returns>
        public vec3[] GetCorners()
        {
            var xv = new vec3(Extents.X, 0, 0);
            var yv = new vec3(0, Extents.Y, 0);
            var zv = new vec3(0, 0, Extents.Z);
            vec3.TransformNormal(in xv, in Transformation, out xv);
            vec3.TransformNormal(in yv, in Transformation, out yv);
            vec3.TransformNormal(in zv, in Transformation, out zv);

            var center = Transformation.TranslationVector;

            var corners = new vec3[8];
            corners[0] = center + xv + yv + zv;
            corners[1] = center + xv + yv - zv;
            corners[2] = center - xv + yv - zv;
            corners[3] = center - xv + yv + zv;
            corners[4] = center + xv - yv + zv;
            corners[5] = center + xv - yv - zv;
            corners[6] = center - xv - yv - zv;
            corners[7] = center - xv - yv + zv;

            return corners;
        }

        /// <summary>
        /// Transforms this box using a transformation matrix.
        /// </summary>
        /// <param name="mat">The transformation matrix.</param>
        /// <remarks>
        /// While any kind of transformation can be applied, it is recommended to apply scaling using scale method instead, which
        /// scales the Extents and keeps the Transformation matrix for rotation only, and that preserves collision detection accuracy.
        /// </remarks>
        public void Transform(in mat4 mat)
        {
            Transformation = mat * Transformation;
        }

        /// <summary>
        /// Transforms this box using a transformation matrix.
        /// </summary>
        /// <param name="mat">The transformation matrix.</param>
        /// <remarks>
        /// While any kind of transformation can be applied, it is recommended to apply scaling using scale method instead, which
        /// scales the Extents and keeps the Transformation matrix for rotation only, and that preserves collision detection accuracy.
        /// </remarks>
        public void Transform(mat4 mat)
        {
            Transformation = mat * Transformation;
        }

        /// <summary>
        /// Scales the <see cref="OrientedBoundingBox"/> by scaling its Extents without affecting the Transformation matrix,
        /// By keeping Transformation matrix scaling-free, the collision detection methods will be more accurate.
        /// </summary>
        /// <param name="scaling"></param>
        public void Scale(in vec3 scaling)
        {
            Extents *= scaling;
        }

        /// <summary>
        /// Scales the <see cref="OrientedBoundingBox"/> by scaling its Extents without affecting the Transformation matrix,
        /// By keeping Transformation matrix scaling-free, the collision detection methods will be more accurate.
        /// </summary>
        /// <param name="scaling"></param>
        public void Scale(vec3 scaling)
        {
            Extents *= scaling;
        }

        /// <summary>
        /// Scales the <see cref="OrientedBoundingBox"/> by scaling its Extents without affecting the Transformation matrix,
        /// By keeping Transformation matrix scaling-free, the collision detection methods will be more accurate.
        /// </summary>
        /// <param name="scaling"></param>
        public void Scale(float scaling)
        {
            Extents *= scaling;
        }

        /// <summary>
        /// Translates the <see cref="OrientedBoundingBox"/> to a new position using a translation vector;
        /// </summary>
        /// <param name="translation">the translation vector.</param>
        public void Translate(in vec3 translation)
        {
            Transformation.TranslationVector += translation;
        }

        /// <summary>
        /// Translates the <see cref="OrientedBoundingBox"/> to a new position using a translation vector;
        /// </summary>
        /// <param name="translation">the translation vector.</param>
        public void Translate(vec3 translation)
        {
            Transformation.TranslationVector += translation;
        }

        /// <summary>
        /// The size of the <see cref="OrientedBoundingBox"/> if no scaling is applied to the transformation matrix.
        /// </summary>
        /// <remarks>
        /// The property will return the actual size even if the scaling is applied using Scale method, 
        /// but if the scaling is applied to transformation matrix, use GetSize Function instead.
        /// </remarks>
        public vec3 Size
        {
            get
            {
                return Extents * 2;
            }
        }

        /// <summary>
        /// Returns the size of the <see cref="OrientedBoundingBox"/> taking into consideration the scaling applied to the transformation matrix.
        /// </summary>
        /// <returns>The size of the consideration</returns>
        /// <remarks>
        /// This method is computationally expensive, so if no scale is applied to the transformation matrix
        /// use <see cref="OrientedBoundingBox.Size"/> property instead.
        /// </remarks>
        public vec3 GetSize()
        {
            var xv = new vec3(Extents.X * 2, 0, 0);
            var yv = new vec3(0, Extents.Y * 2, 0);
            var zv = new vec3(0, 0, Extents.Z * 2);
            vec3.TransformNormal(in xv, in Transformation, out xv);
            vec3.TransformNormal(in yv, in Transformation, out yv);
            vec3.TransformNormal(in zv, in Transformation, out zv);

            return new vec3(xv.Length(), yv.Length(), zv.Length());
        }

        /// <summary>
        /// Returns the square size of the <see cref="OrientedBoundingBox"/> taking into consideration the scaling applied to the transformation matrix.
        /// </summary>
        /// <returns>The size of the consideration</returns>
        public vec3 GetSizeSquared()
        {
            var xv = new vec3(Extents.X * 2, 0, 0);
            var yv = new vec3(0, Extents.Y * 2, 0);
            var zv = new vec3(0, 0, Extents.Z * 2);
            vec3.TransformNormal(in xv, in Transformation, out xv);
            vec3.TransformNormal(in yv, in Transformation, out yv);
            vec3.TransformNormal(in zv, in Transformation, out zv);

            return new vec3(xv.LengthSquared(), yv.LengthSquared(), zv.LengthSquared());
        }

        /// <summary>
        /// Returns the center of the <see cref="OrientedBoundingBox"/>.
        /// </summary>
        public vec3 Center
        {
            get
            {
                return Transformation.TranslationVector;
            }
        }

        /// <summary>
        /// Determines whether a <see cref="OrientedBoundingBox"/> contains a point. 
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public Intersection Contains(in vec3 point)
        {
            // Transform the point into the obb coordinates
            mat4 invTrans;
            glm.inverse(in Transformation, out invTrans);

            vec3 locPoint;
            vec3.TransformCoordinate(in point, in invTrans, out locPoint);

            locPoint.X = Math.Abs(locPoint.X);
            locPoint.Y = Math.Abs(locPoint.Y);
            locPoint.Z = Math.Abs(locPoint.Z);

            //Simple axes-aligned BB check
            if (MathUtil.NearEqual(locPoint.X, Extents.X) && MathUtil.NearEqual(locPoint.Y, Extents.Y) && MathUtil.NearEqual(locPoint.Z, Extents.Z))
                return Intersection.Intersects;
            if (locPoint.X < Extents.X && locPoint.Y < Extents.Y && locPoint.Z < Extents.Z)
                return Intersection.InSide;
            else
                return Intersection.OutSide;
        }

        /// <summary>
        /// Determines whether a <see cref="OrientedBoundingBox"/> contains a point. 
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public Intersection Contains(vec3 point)
        {
            return Contains(in point);
        }

        /// <summary>
        /// Determines whether a <see cref="OrientedBoundingBox"/> contains an array of points>.
        /// </summary>
        /// <param name="points">The points array to test.</param>
        /// <returns>The type of containment.</returns>
        public Intersection Contains(vec3[] points)
        {
            mat4 invTrans;
            glm.inverse(in Transformation, out invTrans);

            var containsAll = true;
            var containsAny = false;

            for (int i = 0; i < points.Length; i++)
            {
                vec3 locPoint;
                vec3.TransformCoordinate(in points[i], in invTrans, out locPoint);

                locPoint.X = Math.Abs(locPoint.X);
                locPoint.Y = Math.Abs(locPoint.Y);
                locPoint.Z = Math.Abs(locPoint.Z);

                //Simple axes-aligned BB check
                if (MathUtil.NearEqual(locPoint.X, Extents.X) &&
                    MathUtil.NearEqual(locPoint.Y, Extents.Y) &&
                    MathUtil.NearEqual(locPoint.Z, Extents.Z))
                    containsAny = true;
                if (locPoint.X < Extents.X && locPoint.Y < Extents.Y && locPoint.Z < Extents.Z)
                    containsAny = true;
                else
                    containsAll = false;
            }

            if (containsAll)
                return Intersection.InSide;
            else if (containsAny)
                return Intersection.Intersects;
            else
                return Intersection.OutSide;
        }

        /// <summary>
        /// Determines whether a <see cref="OrientedBoundingBox"/> contains a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <param name="IgnoreScale">Optimize the check operation by assuming that <see cref="OrientedBoundingBox"/> has no scaling applied</param>
        /// <returns>The type of containment the two objects have.</returns>
        /// <remarks>
        /// This method is not designed for <see cref="OrientedBoundingBox"/> which has a non-uniform scaling applied to its transformation matrix.
        /// But any type of scaling applied using Scale method will keep this method accurate.
        /// </remarks>
        public Intersection Contains(Sphere sphere, bool IgnoreScale = false)
        {
            mat4 invTrans;
            glm.inverse(in Transformation, out invTrans);

            // Transform sphere center into the obb coordinates
            vec3 locCenter;
            vec3.TransformCoordinate(in sphere.center, in invTrans, out locCenter);

            float locRadius;
            if (IgnoreScale)
                locRadius = sphere.radius;
            else
            {
                // Transform sphere radius into the obb coordinates
                vec3 vRadius = vec3.UnitX * sphere.radius;
                vec3.TransformNormal(in vRadius, in invTrans, out vRadius);
                locRadius = vRadius.Length();
            }

            //Perform regular BoundingBox to BoundingSphere containment check
            vec3 minusExtens = -Extents;
            vec3 vector;
            glm.clamp(in locCenter, in minusExtens, in Extents, out vector);
            float distance = vec3.DistanceSquared(locCenter, vector);

            if (distance > locRadius * locRadius)
                return Intersection.OutSide;

            if ((((minusExtens.X + locRadius <= locCenter.X) && (locCenter.X <= Extents.X - locRadius)) && ((Extents.X - minusExtens.X > locRadius) &&
                (minusExtens.Y + locRadius <= locCenter.Y))) && (((locCenter.Y <= Extents.Y - locRadius) && (Extents.Y - minusExtens.Y > locRadius)) &&
                (((minusExtens.Z + locRadius <= locCenter.Z) && (locCenter.Z <= Extents.Z - locRadius)) && (Extents.Z - minusExtens.Z > locRadius))))
            {
                return Intersection.InSide;
            }

            return Intersection.Intersects;
        }

        private static vec3[] GetRows(in mat4 mat)
        {
            return new vec3[] {
                new vec3(mat.M11,mat.M12,mat.M13),
                new vec3(mat.M21,mat.M22,mat.M23),
                new vec3(mat.M31,mat.M32,mat.M33)
            };
        }

        /// <summary>
        /// Check the intersection between two <see cref="OrientedBoundingBox"/>
        /// </summary>
        /// <param name="obb">The OrientedBoundingBoxs to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        /// <remarks>
        /// For accuracy, The transformation matrix for both <see cref="OrientedBoundingBox"/> must not have any scaling applied to it.
        /// Anyway, scaling using Scale method will keep this method accurate.
        /// </remarks>
        public Intersection Contains(in OrientedBoundingBox obb)
        {
            var cornersCheck = Contains(obb.GetCorners());
            if (cornersCheck != Intersection.OutSide)
                return cornersCheck;

            //http://www.3dkingdoms.com/weekly/bbox.cpp
            var SizeA = Extents;
            var SizeB = obb.Extents;
            var RotA = GetRows(in Transformation);
            var RotB = GetRows(in obb.Transformation);

            var R = new mat4();       // Rotation from B to A
            var AR = new mat4();      // absolute values of R matrix, to use with box extents

            float ExtentA, ExtentB, Separation;
            int i, k;

            // Calculate B to A rotation matrix
            for (i = 0; i < 3; i++)
                for (k = 0; k < 3; k++)
                {
                    R[i, k] = vec3.Dot(RotA[i], RotB[k]);
                    AR[i, k] = Math.Abs(R[i, k]);
                }


            // Vector separating the centers of Box B and of Box A	
            var vSepWS = obb.Center - Center;
            // Rotated into Box A's coordinates
            var vSepA = new vec3(vec3.Dot(vSepWS, RotA[0]), vec3.Dot(vSepWS, RotA[1]), vec3.Dot(vSepWS, RotA[2]));

            // Test if any of A's basis vectors separate the box
            for (i = 0; i < 3; i++)
            {
                ExtentA = SizeA[i];
                ExtentB = vec3.Dot(SizeB, new vec3(AR[i, 0], AR[i, 1], AR[i, 2]));
                Separation = Math.Abs(vSepA[i]);

                if (Separation > ExtentA + ExtentB)
                    return Intersection.OutSide;
            }

            // Test if any of B's basis vectors separate the box
            for (k = 0; k < 3; k++)
            {
                ExtentA = vec3.Dot(SizeA, new vec3(AR[0, k], AR[1, k], AR[2, k]));
                ExtentB = SizeB[k];
                Separation = Math.Abs(vec3.Dot(vSepA, new vec3(R[0, k], R[1, k], R[2, k])));

                if (Separation > ExtentA + ExtentB)
                    return Intersection.OutSide;
            }

            // Now test Cross Products of each basis vector combination ( A[i], B[k] )
            for (i = 0; i < 3; i++)
                for (k = 0; k < 3; k++)
                {
                    int i1 = (i + 1) % 3, i2 = (i + 2) % 3;
                    int k1 = (k + 1) % 3, k2 = (k + 2) % 3;
                    ExtentA = SizeA[i1] * AR[i2, k] + SizeA[i2] * AR[i1, k];
                    ExtentB = SizeB[k1] * AR[i, k2] + SizeB[k2] * AR[i, k1];
                    Separation = Math.Abs(vSepA[i2] * R[i1, k] - vSepA[i1] * R[i2, k]);
                    if (Separation > ExtentA + ExtentB)
                        return Intersection.OutSide;
                }

            // No separating axis found, the boxes overlap	
            return Intersection.Intersects;
        }

        /// <summary>
        /// Check the intersection between an <see cref="OrientedBoundingBox"/> and a line defined by two points
        /// </summary>
        /// <param name="L1">The first point in the line.</param>
        /// <param name="L2">The second point in the line.</param>
        /// <returns>The type of containment the two objects have.</returns>
        /// <remarks>
        /// For accuracy, The transformation matrix for the <see cref="OrientedBoundingBox"/> must not have any scaling applied to it.
        /// Anyway, scaling using Scale method will keep this method accurate.
        /// </remarks>
        public Intersection ContainsLine(in vec3 L1, in vec3 L2)
        {
            var cornersCheck = Contains(new vec3[] { L1, L2 });
            if (cornersCheck != Intersection.OutSide)
                return cornersCheck;

            //http://www.3dkingdoms.com/weekly/bbox.cpp
            // Put line in box space
            mat4 invTrans;
            glm.inverse(in Transformation, out invTrans);

            vec3 LB1;
            vec3.TransformCoordinate(in L1, in invTrans, out LB1);
            vec3 LB2;
            vec3.TransformCoordinate(in L1, in invTrans, out LB2);

            // Get line midpoint and extent
            var LMid = (LB1 + LB2) * 0.5f;
            var L = (LB1 - LMid);
            var LExt = new vec3(Math.Abs(L.X), Math.Abs(L.Y), Math.Abs(L.Z));

            // Use Separating Axis Test
            // Separation vector from box center to line center is LMid, since the line is in box space
            if (Math.Abs(LMid.X) > Extents.X + LExt.X) return Intersection.OutSide;
            if (Math.Abs(LMid.Y) > Extents.Y + LExt.Y) return Intersection.OutSide;
            if (Math.Abs(LMid.Z) > Extents.Z + LExt.Z) return Intersection.OutSide;
            // Cross products of line and each axis
            if (Math.Abs(LMid.Y * L.Z - LMid.Z * L.Y) > (Extents.Y * LExt.Z + Extents.Z * LExt.Y)) return Intersection.OutSide;
            if (Math.Abs(LMid.X * L.Z - LMid.Z * L.X) > (Extents.X * LExt.Z + Extents.Z * LExt.X)) return Intersection.OutSide;
            if (Math.Abs(LMid.X * L.Y - LMid.Y * L.X) > (Extents.X * LExt.Y + Extents.Y * LExt.X)) return Intersection.OutSide;
            // No separating axis, the line intersects
            return Intersection.Intersects;
        }

        /// <summary>
        /// Check the intersection between an <see cref="OrientedBoundingBox"/> and <see cref="BoundingBox"/>
        /// </summary>
        /// <param name="box">The BoundingBox to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        /// <remarks>
        /// For accuracy, The transformation matrix for the <see cref="OrientedBoundingBox"/> must not have any scaling applied to it.
        /// Anyway, scaling using Scale method will keep this method accurate.
        /// </remarks>
        public Intersection Contains(in BoundingBox box)
        {
            var cornersCheck = Contains(box.GetCorners());
            if (cornersCheck != Intersection.OutSide)
                return cornersCheck;

            var boxCenter = box.Minimum + (box.Maximum - box.Minimum) / 2f;
            var boxExtents = box.Maximum - boxCenter;

            var SizeA = Extents;
            var SizeB = boxExtents;
            var RotA = GetRows(in Transformation);

            float ExtentA, ExtentB, Separation;
            int i, k;

            mat4 R;                   // Rotation from B to A
            glm.inverse(in Transformation, out R);
            var AR = new mat4();      // absolute values of R matrix, to use with box extents

            for (i = 0; i < 3; i++)
                for (k = 0; k < 3; k++)
                {
                    AR[i, k] = Math.Abs(R[i, k]);
                }


            // Vector separating the centers of Box B and of Box A	
            var vSepWS = boxCenter - Center;
            // Rotated into Box A's coordinates
            var vSepA = new vec3(vec3.Dot(vSepWS, RotA[0]), vec3.Dot(vSepWS, RotA[1]), vec3.Dot(vSepWS, RotA[2]));

            // Test if any of A's basis vectors separate the box
            for (i = 0; i < 3; i++)
            {
                ExtentA = SizeA[i];
                ExtentB = vec3.Dot(SizeB, new vec3(AR[i, 0], AR[i, 1], AR[i, 2]));
                Separation = Math.Abs(vSepA[i]);

                if (Separation > ExtentA + ExtentB)
                    return Intersection.OutSide;
            }

            // Test if any of B's basis vectors separate the box
            for (k = 0; k < 3; k++)
            {
                ExtentA = vec3.Dot(SizeA, new vec3(AR[0, k], AR[1, k], AR[2, k]));
                ExtentB = SizeB[k];
                Separation = Math.Abs(vec3.Dot(vSepA, new vec3(R[0, k], R[1, k], R[2, k])));

                if (Separation > ExtentA + ExtentB)
                    return Intersection.OutSide;
            }

            // Now test Cross Products of each basis vector combination ( A[i], B[k] )
            for (i = 0; i < 3; i++)
                for (k = 0; k < 3; k++)
                {
                    int i1 = (i + 1) % 3, i2 = (i + 2) % 3;
                    int k1 = (k + 1) % 3, k2 = (k + 2) % 3;
                    ExtentA = SizeA[i1] * AR[i2, k] + SizeA[i2] * AR[i1, k];
                    ExtentB = SizeB[k1] * AR[i, k2] + SizeB[k2] * AR[i, k1];
                    Separation = Math.Abs(vSepA[i2] * R[i1, k] - vSepA[i1] * R[i2, k]);
                    if (Separation > ExtentA + ExtentB)
                        return Intersection.OutSide;
                }

            // No separating axis found, the boxes overlap	
            return Intersection.Intersects;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="OrientedBoundingBox"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="point">When the method completes, contains the point of intersection,
        /// or <see cref="vec3.Zero"/> if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in Ray ray, out vec3 point)
        {
            // Put ray in box space
            mat4 invTrans;
            glm.inverse(in Transformation, out invTrans);

            Ray bRay;
            vec3.TransformNormal(in ray.direction, in invTrans, out bRay.direction);
            vec3.TransformCoordinate(in ray.origin, in invTrans, out bRay.origin);

            //Perform a regular ray to BoundingBox check
            var bb = new BoundingBox(-Extents, Extents);
            var intersects = Collision.RayIntersectsBox(in bRay, in bb, out point);

            //Put the result intersection back to world
            if (intersects)
                vec3.TransformCoordinate(in point, in Transformation, out point);

            return intersects;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="OrientedBoundingBox"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in Ray ray)
        {
            vec3 point;
            return Intersects(in ray, out point);
        }

        private vec3[] GetLocalCorners()
        {
            var xv = new vec3(Extents.X, 0, 0);
            var yv = new vec3(0, Extents.Y, 0);
            var zv = new vec3(0, 0, Extents.Z);

            var corners = new vec3[8];
            corners[0] = +xv + yv + zv;
            corners[1] = +xv + yv - zv;
            corners[2] = -xv + yv - zv;
            corners[3] = -xv + yv + zv;
            corners[4] = +xv - yv + zv;
            corners[5] = +xv - yv - zv;
            corners[6] = -xv - yv - zv;
            corners[7] = -xv - yv + zv;

            return corners;
        }

        /// <summary>
        /// Get the axis-aligned <see cref="BoundingBox"/> which contains all <see cref="OrientedBoundingBox"/> corners.
        /// </summary>
        /// <returns>The axis-aligned BoundingBox of this OrientedBoundingBox.</returns>
        public BoundingBox GetBoundingBox()
        {
            return BoundingBox.FromPoints(GetCorners());
        }

        /// <summary>
        /// Determines whether the specified <see cref="Vector4"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="Vector4"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="Vector4"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(in OrientedBoundingBox value)
        {
            return Extents == value.Extents && Transformation == value.Transformation;
        }

        /// <summary>
        /// Determines whether the specified <see cref="Vector4"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="Vector4"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="Vector4"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(OrientedBoundingBox value)
        {
            return Equals(in value);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object value)
        {
            if (!(value is OrientedBoundingBox))
                return false;

            var strongValue = (OrientedBoundingBox)value;
            return Equals(in strongValue);
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(OrientedBoundingBox left, OrientedBoundingBox right)
        {
            return left.Equals(in right);
        }

        /// <summary>
        /// Tests for inequality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator !=(OrientedBoundingBox left, OrientedBoundingBox right)
        {
            return !left.Equals(in right);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return Extents.GetHashCode() + Transformation.GetHashCode();
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Center: {0}, Extents: {1}", Center, Extents);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(string format)
        {
            if (format == null)
                return ToString();

            return string.Format(CultureInfo.CurrentCulture, "Center: {0}, Extents: {1}", Center.ToString(format, CultureInfo.CurrentCulture),
                Extents.ToString(format, CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Center: {0}, Extents: {1}", Center.ToString(), Extents.ToString());
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null)
                return ToString(formatProvider);

            return string.Format(formatProvider, "Center: {0}, Extents: {1}", Center.ToString(format, formatProvider),
                Extents.ToString(format, formatProvider));
        }
    }
}

