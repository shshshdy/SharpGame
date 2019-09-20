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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGame
{
    /// <summary>
    /// Defines a frustum which can be used in frustum culling, zoom to Extents (zoom to fit) operations, 
    /// (matrix, frustum, camera) interchange, and many kind of intersection testing.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct BoundingFrustum : IEquatable<BoundingFrustum>
    {
        private mat4 pMatrix;
        private Plane  pNear;
        private Plane  pFar;
        private Plane  pLeft;
        private Plane  pRight;
        private Plane  pTop;
        private Plane  pBottom;

        /// <summary>
        /// Gets or sets the mat4 that describes this bounding frustum.
        /// </summary>
        public mat4 mat4
        {
            get
            {
                return pMatrix;
            }
            set
            {
                pMatrix = value;
                GetPlanesFromMatrix(in pMatrix, out pNear, out pFar, out pLeft, out pRight, out pTop, out pBottom);
            }
        }
        /// <summary>
        /// Gets the near plane of the BoundingFrustum.
        /// </summary>
        public Plane Near
        {
            get
            {
                return pNear;
            }
        }
        /// <summary>
        /// Gets the far plane of the BoundingFrustum.
        /// </summary>
        public Plane Far
        {
            get
            {
                return pFar;
            }
        }
        /// <summary>
        /// Gets the left plane of the BoundingFrustum.
        /// </summary>
        public Plane Left
        {
            get
            {
                return pLeft;
            }
        }
        /// <summary>
        /// Gets the right plane of the BoundingFrustum.
        /// </summary>
        public Plane Right
        {
            get
            {
                return pRight;
            }
        }
        /// <summary>
        /// Gets the top plane of the BoundingFrustum.
        /// </summary>
        public Plane Top
        {
            get
            {
                return pTop;
            }
        }
        /// <summary>
        /// Gets the bottom plane of the BoundingFrustum.
        /// </summary>
        public Plane Bottom
        {
            get
            {
                return pBottom;
            }
        }

        /// <summary>
        /// Creates a new instance of BoundingFrustum.
        /// </summary>
        /// <param name="matrix">Combined matrix that usually takes view × projection matrix.</param>
        public BoundingFrustum(mat4 matrix)
        {
            pMatrix = matrix;
            GetPlanesFromMatrix(in pMatrix, out pNear, out pFar, out pLeft, out pRight, out pTop, out pBottom);
        }

        public override int GetHashCode()
        {
            return pMatrix.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="BoundingFrustum"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="BoundingFrustum"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="BoundingFrustum"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(in BoundingFrustum other)
        {
            return this.pMatrix == other.pMatrix;
        }

        /// <summary>
        /// Determines whether the specified <see cref="BoundingFrustum"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="BoundingFrustum"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="BoundingFrustum"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(BoundingFrustum other)
        {
            return Equals(in other);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if(!(obj is BoundingFrustum))
                return false;

            var strongValue = (BoundingFrustum)obj;
            return Equals(in strongValue);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(BoundingFrustum left, BoundingFrustum right)
        {
            return left.Equals(in right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator !=(BoundingFrustum left, BoundingFrustum right)
        {
            return !left.Equals(in right);
        }

        /// <summary>
        /// Returns one of the 6 planes related to this frustum.
        /// </summary>
        /// <param name="index">Plane index where 0 fro Left, 1 for Right, 2 for Top, 3 for Bottom, 4 for Near, 5 for Far</param>
        /// <returns></returns>
        public Plane GetPlane(int index)
        {
            switch (index)
            {
                case 0: return pLeft;
                case 1: return pRight;
                case 2: return pTop;
                case 3: return pBottom;
                case 4: return pNear;
                case 5: return pFar;
                default:
                    return new Plane();
            }
        }

        private static void GetPlanesFromMatrix(in mat4 matrix, out Plane near, out Plane far, out Plane left, out Plane right, out Plane top, out Plane bottom)
        {
            //http://www.chadvernon.com/blog/resources/directx9/frustum-culling/

            // Left plane
            left.normal.x = matrix.M14 + matrix.M11;
            left.normal.y = matrix.M24 + matrix.M21;
            left.normal.z = matrix.M34 + matrix.M31;
            left.d = matrix.M44 + matrix.M41;
            left.Normalize();

            // Right plane
            right.normal.x = matrix.M14 - matrix.M11;
            right.normal.y = matrix.M24 - matrix.M21;
            right.normal.z = matrix.M34 - matrix.M31;
            right.d = matrix.M44 - matrix.M41;
            right.Normalize();

            // Top plane
            top.normal.x = matrix.M14 - matrix.M12;
            top.normal.y = matrix.M24 - matrix.M22;
            top.normal.z = matrix.M34 - matrix.M32;
            top.d = matrix.M44 - matrix.M42;
            top.Normalize();

            // Bottom plane
            bottom.normal.x = matrix.M14 + matrix.M12;
            bottom.normal.y = matrix.M24 + matrix.M22;
            bottom.normal.z = matrix.M34 + matrix.M32;
            bottom.d = matrix.M44 + matrix.M42;
            bottom.Normalize();

            // Near plane
            near.normal.x = matrix.M13;
            near.normal.y = matrix.M23;
            near.normal.z = matrix.M33;
            near.d = matrix.M43;
            near.Normalize();

            // Far plane
            far.normal.x = matrix.M14 - matrix.M13;
            far.normal.y = matrix.M24 - matrix.M23;
            far.normal.z = matrix.M34 - matrix.M33;
            far.d = matrix.M44 - matrix.M43;
            far.Normalize();
        }

        private static vec3 Get3PlanesInterPoint(in Plane p1, in Plane p2, in Plane p3)
        {
            //P = -d1 * N2xN3 / N1.N2xN3 - d2 * N3xN1 / N2.N3xN1 - d3 * N1xN2 / N3.N1xN2 
            vec3 v =
                -p1.d * vec3.Cross(p2.normal, p3.normal) / vec3.Dot(p1.normal, vec3.Cross(p2.normal, p3.normal))
                - p2.d * vec3.Cross(p3.normal, p1.normal) / vec3.Dot(p2.normal, vec3.Cross(p3.normal, p1.normal))
                - p3.d * vec3.Cross(p1.normal, p2.normal) / vec3.Dot(p3.normal, vec3.Cross(p1.normal, p2.normal));

            return v;
        }

        /// <summary>
        /// Creates a new frustum relaying on perspective camera parameters
        /// </summary>
        /// <param name="cameraPos">The camera pos.</param>
        /// <param name="lookDir">The look dir.</param>
        /// <param name="upDir">Up dir.</param>
        /// <param name="fov">The fov.</param>
        /// <param name="znear">The znear.</param>
        /// <param name="zfar">The zfar.</param>
        /// <param name="aspect">The aspect.</param>
        /// <returns>The bounding frustum calculated from perspective camera</returns>
        public static BoundingFrustum FromCamera(vec3 cameraPos, vec3 lookDir, vec3 upDir, float fov, float znear, float zfar, float aspect)
        {
            //http://knol.google.com/k/view-frustum

            lookDir = glm.normalize(lookDir);
            upDir = glm.normalize(upDir);

            vec3 nearCenter = cameraPos + lookDir * znear;
            vec3 farCenter = cameraPos + lookDir * zfar;
            float nearHalfHeight = (float)(znear * Math.Tan(fov / 2f));
            float farHalfHeight = (float)(zfar * Math.Tan(fov / 2f));
            float nearHalfWidth = nearHalfHeight * aspect;
            float farHalfWidth = farHalfHeight * aspect;

            vec3 rightDir = glm.normalize(vec3.Cross(upDir, lookDir));
            vec3 Near1 = nearCenter - nearHalfHeight * upDir + nearHalfWidth * rightDir;
            vec3 Near2 = nearCenter + nearHalfHeight * upDir + nearHalfWidth * rightDir;
            vec3 Near3 = nearCenter + nearHalfHeight * upDir - nearHalfWidth * rightDir;
            vec3 Near4 = nearCenter - nearHalfHeight * upDir - nearHalfWidth * rightDir;
            vec3 Far1 = farCenter - farHalfHeight * upDir + farHalfWidth * rightDir;
            vec3 Far2 = farCenter + farHalfHeight * upDir + farHalfWidth * rightDir;
            vec3 Far3 = farCenter + farHalfHeight * upDir - farHalfWidth * rightDir;
            vec3 Far4 = farCenter - farHalfHeight * upDir - farHalfWidth * rightDir;

            var result = new BoundingFrustum();
            result.pNear = new Plane(Near1, Near2, Near3);
            result.pFar = new Plane(Far3, Far2, Far1);
            result.pLeft = new Plane(Near4, Near3, Far3);
            result.pRight = new Plane(Far1, Far2, Near2);
            result.pTop = new Plane(Near2, Far2, Far3);
            result.pBottom = new Plane(Far4, Far1, Near1);

            result.pNear.Normalize();
            result.pFar.Normalize();
            result.pLeft.Normalize();
            result.pRight.Normalize();
            result.pTop.Normalize();
            result.pBottom.Normalize();

            result.pMatrix =glm.perspective(fov, aspect, znear, zfar) * glm.lookAt(cameraPos, cameraPos + lookDir * 10, upDir);

            return result;
        }

        /// <summary>
        /// Returns the 8 corners of the frustum, element0 is Near1 (near right down corner)
        /// , element1 is Near2 (near right top corner)
        /// , element2 is Near3 (near Left top corner)
        /// , element3 is Near4 (near Left down corner)
        /// , element4 is Far1 (far right down corner)
        /// , element5 is Far2 (far right top corner)
        /// , element6 is Far3 (far left top corner)
        /// , element7 is Far4 (far left down corner)
        /// </summary>
        /// <returns>The 8 corners of the frustum</returns>
        public vec3[] GetCorners()
        {
            var corners = new vec3[8];
            GetCorners(corners);
            return corners;
        }

        /// <summary>
        /// Returns the 8 corners of the frustum, element0 is Near1 (near right down corner)
        /// , element1 is Near2 (near right top corner)
        /// , element2 is Near3 (near Left top corner)
        /// , element3 is Near4 (near Left down corner)
        /// , element4 is Far1 (far right down corner)
        /// , element5 is Far2 (far right top corner)
        /// , element6 is Far3 (far left top corner)
        /// , element7 is Far4 (far left down corner)
        /// </summary>
        /// <returns>The 8 corners of the frustum</returns>
        public void GetCorners(vec3[] corners)
        {
            corners[0] = Get3PlanesInterPoint(in pNear, in  pBottom, in  pRight);    //Near1
            corners[1] = Get3PlanesInterPoint(in pNear, in  pTop, in  pRight);       //Near2
            corners[2] = Get3PlanesInterPoint(in pNear, in  pTop, in  pLeft);        //Near3
            corners[3] = Get3PlanesInterPoint(in pNear, in  pBottom, in  pLeft);     //Near3
            corners[4] = Get3PlanesInterPoint(in pFar, in  pBottom, in  pRight);    //Far1
            corners[5] = Get3PlanesInterPoint(in pFar, in  pTop, in  pRight);       //Far2
            corners[6] = Get3PlanesInterPoint(in pFar, in  pTop, in  pLeft);        //Far3
            corners[7] = Get3PlanesInterPoint(in pFar, in  pBottom, in  pLeft);     //Far3
        }

        /// <summary>
        /// Checks whether a point lay inside, intersects or lay outside the frustum.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Type of the containment</returns>
        public Intersection Contains(in vec3 point)
        {
            var result = PlaneIntersectionType.Front;
            var planeResult = PlaneIntersectionType.Front;
            for (int i = 0; i < 6; i++)
            {
                switch (i)
                {
                    case 0: planeResult = pNear.Intersects(in point); break;
                    case 1: planeResult = pFar.Intersects(in point); break;
                    case 2: planeResult = pLeft.Intersects(in point); break;
                    case 3: planeResult = pRight.Intersects(in point); break;
                    case 4: planeResult = pTop.Intersects(in point); break;
                    case 5: planeResult = pBottom.Intersects(in point); break;
                }
                switch (planeResult)
                {
                    case PlaneIntersectionType.Back:
                        return Intersection.OutSide;
                    case PlaneIntersectionType.Intersecting:
                        result = PlaneIntersectionType.Intersecting;
                        break;
                }
            }
            switch (result)
            {
                case PlaneIntersectionType.Intersecting: return Intersection.Intersects;
                default: return Intersection.InSide;
            }
        }

        /// <summary>
        /// Checks whether a point lay inside, intersects or lay outside the frustum.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Type of the containment</returns>
        public Intersection Contains(vec3 point)
        {
            return Contains(in point);
        }

        /// <summary>
        /// Checks whether a group of points lay totally inside the frustum (Contains), or lay partially inside the frustum (Intersects), or lay outside the frustum (Disjoint).
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>Type of the containment</returns>
        public Intersection Contains(vec3[] points)
        {
            throw new NotImplementedException();
            /* TODO: (PMin) This method is wrong, does not calculate case where only plane from points is intersected
            var containsAny = false;
            var containsAll = true;
            for (int i = 0; i < points.Length; i++)
            {
                switch (Contains(in points[i]))
                {
                    case ContainmentType.Contains:
                    case ContainmentType.Intersects:
                        containsAny = true;
                        break;
                    case ContainmentType.Disjoint:
                        containsAll = false;
                        break;
                }
            }
            if (containsAny)
            {
                if (containsAll)
                    return ContainmentType.Contains;
                else
                    return ContainmentType.Intersects;
            }
            else
                return ContainmentType.Disjoint;  */
        }
        /// <summary>
        /// Checks whether a group of points lay totally inside the frustum (Contains), or lay partially inside the frustum (Intersects), or lay outside the frustum (Disjoint).
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="result">Type of the containment.</param>
        public void Contains(vec3[] points, out Intersection result)
        {
            result = Contains(points);
        }

        private void GetBoxToPlanePVertexNVertex(in BoundingBox box, in vec3 planeNormal, out vec3 p, out vec3 n)
        {
            p = box.Minimum;
            if (planeNormal.X >= 0)
                p.X = box.Maximum.X;
            if (planeNormal.Y >= 0)
                p.Y = box.Maximum.Y;
            if (planeNormal.Z >= 0)
                p.Z = box.Maximum.Z;

            n = box.Maximum;
            if (planeNormal.X >= 0)
                n.X = box.Minimum.X;
            if (planeNormal.Y >= 0)
                n.Y = box.Minimum.Y;
            if (planeNormal.Z >= 0)
                n.Z = box.Minimum.Z;
        }

        /// <summary>
        /// Determines the intersection relationship between the frustum and a bounding box.
        /// </summary>
        /// <param name="box">The box.</param>
        /// <returns>Type of the containment</returns>
        public Intersection Contains(in BoundingBox box)
        {
            vec3 p, n;
            Plane plane;
            var result = Intersection.InSide;
            for (int i = 0; i < 6; i++)
            {
                plane = GetPlane(i);
                GetBoxToPlanePVertexNVertex(in box, in plane.normal, out p, out n);
                if (Collision.PlaneIntersectsPoint(in plane, in p) == PlaneIntersectionType.Back)
                    return Intersection.OutSide;

                if (Collision.PlaneIntersectsPoint(in plane, in n) == PlaneIntersectionType.Back)
                    result = Intersection.Intersects;
            }
            return result;
        }

        /// <summary>
        /// Determines the intersection relationship between the frustum and a bounding box.
        /// </summary>
        /// <param name="box">The box.</param>
        /// <returns>Type of the containment</returns>
        public Intersection Contains(BoundingBox box)
        {
            return Contains(in box);
        }

        /// <summary>
        /// Determines the intersection relationship between the frustum and a bounding box.
        /// </summary>
        /// <param name="box">The box.</param>
        /// <param name="result">Type of the containment.</param>
        public void Contains(in BoundingBox box, out Intersection result)
        {
            result = Contains(in box);
        }

        /// <summary>
        /// Determines the intersection relationship between the frustum and a bounding sphere.
        /// </summary>
        /// <param name="sphere">The sphere.</param>
        /// <returns>Type of the containment</returns>
        public Intersection Contains(in Sphere sphere)
        {
            var result = PlaneIntersectionType.Front;
            var planeResult = PlaneIntersectionType.Front;
            for (int i = 0; i < 6; i++)
            {
                switch (i)
                {
                    case 0: planeResult = pNear.Intersects(in sphere); break;
                    case 1: planeResult = pFar.Intersects(in sphere); break;
                    case 2: planeResult = pLeft.Intersects(in sphere); break;
                    case 3: planeResult = pRight.Intersects(in sphere); break;
                    case 4: planeResult = pTop.Intersects(in sphere); break;
                    case 5: planeResult = pBottom.Intersects(in sphere); break;
                }
                switch (planeResult)
                {
                    case PlaneIntersectionType.Back:
                        return Intersection.OutSide;
                    case PlaneIntersectionType.Intersecting:
                        result = PlaneIntersectionType.Intersecting;
                        break;
                }
            }
            switch (result)
            {
                case PlaneIntersectionType.Intersecting: return Intersection.Intersects;
                default: return Intersection.InSide;
            }
        }

        /// <summary>
        /// Determines the intersection relationship between the frustum and a bounding sphere.
        /// </summary>
        /// <param name="sphere">The sphere.</param>
        /// <returns>Type of the containment</returns>
        public Intersection Contains(Sphere sphere)
        {
            return Contains(in sphere);
        }

        /// <summary>
        /// Determines the intersection relationship between the frustum and a bounding sphere.
        /// </summary>
        /// <param name="sphere">The sphere.</param>
        /// <param name="result">Type of the containment.</param>
        public void Contains(in Sphere sphere, out Intersection result)
        {
            result = Contains(in sphere);
        }

        /// <summary>
        /// Determines the intersection relationship between the frustum and another bounding frustum.
        /// </summary>
        /// <param name="frustum">The frustum.</param>
        /// <returns>Type of the containment</returns>
        public bool Contains(in BoundingFrustum frustum)
        {
            return Contains(frustum.GetCorners()) != Intersection.OutSide;
        }

        /// <summary>
        /// Determines the intersection relationship between the frustum and another bounding frustum.
        /// </summary>
        /// <param name="frustum">The frustum.</param>
        /// <returns>Type of the containment</returns>
        public bool Contains(BoundingFrustum frustum)
        {
            return Contains(in frustum);
        }

        /// <summary>
        /// Determines the intersection relationship between the frustum and another bounding frustum.
        /// </summary>
        /// <param name="frustum">The frustum.</param>
        /// <param name="result">Type of the containment.</param>
        public void Contains(in BoundingFrustum frustum, out bool result)
        {
            result = Contains(frustum.GetCorners()) != Intersection.OutSide;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The sphere.</param>
        /// <returns>Type of the containment</returns>
        public bool Intersects(in Sphere sphere)
        {
            return Contains(in sphere) != Intersection.OutSide;
        }
        /// <summary>
        /// Checks whether the current BoundingFrustum intersects a BoundingSphere.
        /// </summary>
        /// <param name="sphere">The sphere.</param>
        /// <param name="result">Set to <c>true</c> if the current BoundingFrustum intersects a BoundingSphere.</param>
        public void Intersects(in Sphere sphere, out bool result)
        {
            result = Contains(in sphere) != Intersection.OutSide;
        }
        /// <summary>
        /// Checks whether the current BoundingFrustum intersects a BoundingBox.
        /// </summary>
        /// <param name="box">The box.</param>
        /// <returns><c>true</c> if the current BoundingFrustum intersects a BoundingSphere.</returns>
        public bool Intersects(in BoundingBox box)
        {
            return Contains(in box) != Intersection.OutSide;
        }
        /// <summary>
        /// Checks whether the current BoundingFrustum intersects a BoundingBox.
        /// </summary>
        /// <param name="box">The box.</param>
        /// <param name="result"><c>true</c> if the current BoundingFrustum intersects a BoundingSphere.</param>
        public void Intersects(in BoundingBox box, out bool result)
        {
            result = Contains(in box) != Intersection.OutSide;
        }

        private PlaneIntersectionType PlaneIntersectsPoints(in Plane plane, vec3[] points)
        {
            var result = Collision.PlaneIntersectsPoint(in plane, in points[0]);
            for (int i = 1; i < points.Length; i++)
                if (Collision.PlaneIntersectsPoint(in plane, in points[i]) != result)
                    return PlaneIntersectionType.Intersecting;
            return result;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects the specified Plane.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <returns>Plane intersection type.</returns>
        public PlaneIntersectionType Intersects(in Plane plane)
        {
            return PlaneIntersectsPoints(in plane, GetCorners());
        }
        /// <summary>
        /// Checks whether the current BoundingFrustum intersects the specified Plane.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="result">Plane intersection type.</param>
        public void Intersects(in Plane plane, out PlaneIntersectionType result)
        {
            result = PlaneIntersectsPoints(in plane, GetCorners());
        }

        /// <summary>
        /// Get the width of the frustum at specified depth.
        /// </summary>
        /// <param name="depth">the depth at which to calculate frustum width.</param>
        /// <returns>With of the frustum at the specified depth</returns>
        public float GetWidthAtDepth(float depth)
        {
            float hAngle = (float)((Math.PI / 2.0 - Math.Acos(vec3.Dot(pNear.normal, pLeft.normal))));
            return (float)(Math.Tan(hAngle) * depth * 2);
        }

        /// <summary>
        /// Get the height of the frustum at specified depth.
        /// </summary>
        /// <param name="depth">the depth at which to calculate frustum height.</param>
        /// <returns>Height of the frustum at the specified depth</returns>
        public float GetHeightAtDepth(float depth)
        {
            float vAngle = (float)((Math.PI / 2.0 - Math.Acos(vec3.Dot(pNear.normal, pTop.normal))));
            return (float)(Math.Tan(vAngle) * depth * 2);
        }

        private BoundingFrustum GetInsideOutClone()
        {
            var frustum = this;
            frustum.pNear.normal = -frustum.pNear.normal;
            frustum.pFar.normal = -frustum.pFar.normal;
            frustum.pLeft.normal = -frustum.pLeft.normal;
            frustum.pRight.normal = -frustum.pRight.normal;
            frustum.pTop.normal = -frustum.pTop.normal;
            frustum.pBottom.normal = -frustum.pBottom.normal;
            return frustum;
        }

        /// <summary>
        /// Checks whether the current BoundingFrustum intersects the specified Ray.
        /// </summary>
        /// <param name="ray">The ray.</param>
        /// <returns><c>true</c> if the current BoundingFrustum intersects the specified Ray.</returns>
        public bool Intersects(in Ray ray)
        {
            float? inDist, outDist;
            return Intersects(in ray, out inDist, out outDist);
        }
        /// <summary>
        /// Checks whether the current BoundingFrustum intersects the specified Ray.
        /// </summary>
        /// <param name="ray">The Ray to check for intersection with.</param>
        /// <param name="inDistance">The distance at which the ray enters the frustum if there is an intersection and the ray starts outside the frustum.</param>
        /// <param name="outDistance">The distance at which the ray exits the frustum if there is an intersection.</param>
        /// <returns><c>true</c> if the current BoundingFrustum intersects the specified Ray.</returns>
        public bool Intersects(in Ray ray, out float? inDistance, out float? outDistance)
        {
            if (Contains(ray.origin) != Intersection.OutSide)
            {
                float nearstPlaneDistance = float.MaxValue;
                for (int i = 0; i < 6; i++)
                {
                    var plane = GetPlane(i);
                    float distance;
                    if (Collision.RayIntersectsPlane(in ray, in plane, out distance) && distance < nearstPlaneDistance)
                    {
                        nearstPlaneDistance = distance;
                    }
                }

                inDistance = nearstPlaneDistance;
                outDistance = null;
                return true;
            }
            else
            {
                //We will find the two points at which the ray enters and exists the frustum
                //These two points make a line which center inside the frustum if the ray intersects it
                //Or outside the frustum if the ray intersects frustum planes outside it.
                float minDist = float.MaxValue;
                float maxDist = float.MinValue;
                for (int i = 0; i < 6; i++)
                {
                    var plane = GetPlane(i);
                    float distance;
                    if (Collision.RayIntersectsPlane(in ray, in plane, out distance))
                    {
                        minDist = Math.Min(minDist, distance);
                        maxDist = Math.Max(maxDist, distance);
                    }
                }

                vec3 minPoint = ray.origin + ray.direction * minDist;
                vec3 maxPoint = ray.origin + ray.direction * maxDist;
                vec3 center = (minPoint + maxPoint) / 2f;
                if (Contains(in center) != Intersection.OutSide)
                {
                    inDistance = minDist;
                    outDistance = maxDist;
                    return true;
                }
                else
                {
                    inDistance = null;
                    outDistance = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// Get the distance which when added to camera position along the lookat direction will do the effect of zoom to extents (zoom to fit) operation,
        /// so all the passed points will fit in the current view.
        /// if the returned value is positive, the camera will move toward the lookat direction (ZoomIn).
        /// if the returned value is negative, the camera will move in the reverse direction of the lookat direction (ZoomOut).
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>The zoom to fit distance</returns>
        public float GetZoomToExtentsShiftDistance(vec3[] points)
        {
            float vAngle = (float)((Math.PI / 2.0 - Math.Acos(vec3.Dot(pNear.normal, pTop.normal))));
            float vSin = (float)Math.Sin(vAngle);
            float hAngle = (float)((Math.PI / 2.0 - Math.Acos(vec3.Dot(pNear.normal, pLeft.normal))));
            float hSin = (float)Math.Sin(hAngle);
            float horizontalToVerticalMapping = vSin / hSin;

            var ioFrustrum = GetInsideOutClone();

            float maxPointDist = float.MinValue;
            for (int i = 0; i < points.Length; i++)
            {
                float pointDist = Collision.DistancePlanePoint(in ioFrustrum.pTop, in points[i]);
                pointDist = Math.Max(pointDist, Collision.DistancePlanePoint(in ioFrustrum.pBottom, in points[i]));
                pointDist = Math.Max(pointDist, Collision.DistancePlanePoint(in ioFrustrum.pLeft, in points[i]) * horizontalToVerticalMapping);
                pointDist = Math.Max(pointDist, Collision.DistancePlanePoint(in ioFrustrum.pRight, in points[i]) * horizontalToVerticalMapping);

                maxPointDist = Math.Max(maxPointDist, pointDist);
            }
            return -maxPointDist / vSin;
        }

        /// <summary>
        /// Get the distance which when added to camera position along the lookat direction will do the effect of zoom to extents (zoom to fit) operation,
        /// so all the passed points will fit in the current view.
        /// if the returned value is positive, the camera will move toward the lookat direction (ZoomIn).
        /// if the returned value is negative, the camera will move in the reverse direction of the lookat direction (ZoomOut).
        /// </summary>
        /// <param name="boundingBox">The bounding box.</param>
        /// <returns>The zoom to fit distance</returns>
        public float GetZoomToExtentsShiftDistance(in BoundingBox boundingBox)
        {
            return GetZoomToExtentsShiftDistance(boundingBox.GetCorners());
        }

        /// <summary>
        /// Get the vector shift which when added to camera position will do the effect of zoom to extents (zoom to fit) operation,
        /// so all the passed points will fit in the current view.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>The zoom to fit vector</returns>
        public vec3 GetZoomToExtentsShiftVector(vec3[] points)
        {
            return GetZoomToExtentsShiftDistance(points) * pNear.normal;
        }
        /// <summary>
        /// Get the vector shift which when added to camera position will do the effect of zoom to extents (zoom to fit) operation,
        /// so all the passed points will fit in the current view.
        /// </summary>
        /// <param name="boundingBox">The bounding box.</param>
        /// <returns>The zoom to fit vector</returns>
        public vec3 GetZoomToExtentsShiftVector(in BoundingBox boundingBox)
        {
            return GetZoomToExtentsShiftDistance(boundingBox.GetCorners()) * pNear.normal;
        }

        /// <summary>
        /// Indicate whether the current BoundingFrustrum is Orthographic.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the current BoundingFrustrum is Orthographic; otherwise, <c>false</c>.
        /// </value>
        public bool IsOrthographic
        {
            get
            {
                return (pLeft.normal == -pRight.normal) && (pTop.normal == -pBottom.normal);
            }
        }
    }
    
}
