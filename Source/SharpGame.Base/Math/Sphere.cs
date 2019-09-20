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
    /// Represents a bounding sphere in three dimensional space.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Sphere : IEquatable<Sphere>, IFormattable
    {
        /// <summary>
        /// The center of the sphere in three dimensional space.
        /// </summary>
        public vec3 center;

        /// <summary>
        /// The radius of the sphere.
        /// </summary>
        public float radius;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sphere"/> struct.
        /// </summary>
        /// <param name="center">The center of the sphere in three dimensional space.</param>
        /// <param name="radius">The radius of the sphere.</param>
        public Sphere(vec3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
        /// Return distance of a point to the surface, or 0 if inside.
        public float Distance( vec3 point) { return Math.Max((point - center).Length() - radius, 0.0f); }
        /// Return point on the sphere relative to sphere position.
        public vec3 GetLocalPoint(float theta, float phi)
        {
            return new vec3(
                radius * (float)Math.Sin(theta) * (float)Math.Sin(phi),
                radius * (float)Math.Cos(phi),
                radius * (float)Math.Cos(theta) * (float)Math.Sin(phi)
            );
        }

        /// Return point on the sphere.
        public vec3 GetPoint(float theta, float phi)
        {
            return center + GetLocalPoint(theta, phi);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Ray"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in Ray ray)
        {
            float distance;
            return Collision.RayIntersectsSphere(in ray, in this, out distance);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Ray"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="distance">When the method completes, contains the distance of the intersection,
        /// or 0 if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in Ray ray, out float distance)
        {
            return Collision.RayIntersectsSphere(in ray, in this, out distance);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Ray"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="point">When the method completes, contains the point of intersection,
        /// or <see cref="vec3.Zero"/> if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in Ray ray, out vec3 point)
        {
            return Collision.RayIntersectsSphere(in ray, in this, out point);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public PlaneIntersectionType Intersects(in Plane plane)
        {
            return Collision.PlaneIntersectsSphere(in plane, in this);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a triangle.
        /// </summary>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in vec3 vertex1, in vec3 vertex2, in vec3 vertex3)
        {
            return Collision.SphereIntersectsTriangle(in this, in vertex1, in vertex2, in vertex3);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in BoundingBox box)
        {
            return Collision.BoxIntersectsSphere(in box, in this);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(BoundingBox box)
        {
            return Intersects(in box);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in Sphere sphere)
        {
            return Collision.SphereIntersectsSphere(in this, in sphere);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(Sphere sphere)
        {
            return Intersects(in sphere);
        }

        /// <summary>
        /// Determines whether the current objects contains a point.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public Intersection Contains(in vec3 point)
        {
            return Collision.SphereContainsPoint(in this, in point);
        }

        /// <summary>
        /// Determines whether the current objects contains a triangle.
        /// </summary>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public Intersection Contains(in vec3 vertex1, in vec3 vertex2, in vec3 vertex3)
        {
            return Collision.SphereContainsTriangle(in this, in vertex1, in vertex2, in vertex3);
        }

        /// <summary>
        /// Determines whether the current objects contains a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public Intersection Contains(in BoundingBox box)
        {
            return Collision.SphereContainsBox(in this, in box);
        }

        /// <summary>
        /// Determines whether the current objects contains a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public Intersection Contains(in Sphere sphere)
        {
            return Collision.SphereContainsSphere(in this, in sphere);
        }

        /// <summary>
        /// Constructs a <see cref="Sphere" /> that fully contains the given points.
        /// </summary>
        /// <param name="points">The points that will be contained by the sphere.</param>
        /// <param name="start">The start index from points array to start compute the bounding sphere.</param>
        /// <param name="count">The count of points to process to compute the bounding sphere.</param>
        /// <param name="result">When the method completes, contains the newly constructed bounding sphere.</param>
        /// <exception cref="System.ArgumentNullException">points</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// start
        /// or
        /// count
        /// </exception>
        public static void FromPoints(vec3[] points, int start, int count, out Sphere result)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            // Check that start is in the correct range
            if (start < 0 || start >= points.Length)
            {
                throw new ArgumentOutOfRangeException("start", start, string.Format("Must be in the range [0, {0}]", points.Length - 1));
            }

            // Check that count is in the correct range
            if (count < 0 || (start + count) > points.Length)
            {
                throw new ArgumentOutOfRangeException("count", count, string.Format("Must be in the range <= {0}", points.Length));
            }

            var upperEnd = start + count;

            //Find the center of all points.
            vec3 center = vec3.Zero;
            for (int i = start; i < upperEnd; ++i)
            {
                vec3.Add(in points[i], in center, out center);
            }

            //This is the center of our sphere.
            center /= (float)count;

            //Find the radius of the sphere
            float radius = 0f;
            for (int i = start; i < upperEnd; ++i)
            {
                //We are doing a relative distance comparison to find the maximum distance
                //from the center of our sphere.
                float distance;
                vec3.DistanceSquared(in center, in points[i], out distance);

                if (distance > radius)
                    radius = distance;
            }

            //Find the real distance from the DistanceSquared.
            radius = (float)Math.Sqrt(radius);

            //Construct the sphere.
            result.center = center;
            result.radius = radius;
        }

        /// <summary>
        /// Constructs a <see cref="Sphere"/> that fully contains the given points.
        /// </summary>
        /// <param name="points">The points that will be contained by the sphere.</param>
        /// <param name="result">When the method completes, contains the newly constructed bounding sphere.</param>
        public static void FromPoints(vec3[] points, out Sphere result)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            FromPoints(points, 0, points.Length, out result);
        }

        /// <summary>
        /// Constructs a <see cref="Sphere"/> that fully contains the given points.
        /// </summary>
        /// <param name="points">The points that will be contained by the sphere.</param>
        /// <returns>The newly constructed bounding sphere.</returns>
        public static Sphere FromPoints(vec3[] points)
        {
            Sphere result;
            FromPoints(points, out result);
            return result;
        }

        /// <summary>
        /// Constructs a <see cref="Sphere"/> from a given box.
        /// </summary>
        /// <param name="box">The box that will designate the extents of the sphere.</param>
        /// <param name="result">When the method completes, the newly constructed bounding sphere.</param>
        public static void FromBox(in BoundingBox box, out Sphere result)
        {
            glm.lerp(in box.min, in box.max, 0.5f, out result.center);

            float x = box.min.X - box.max.X;
            float y = box.min.Y - box.max.Y;
            float z = box.min.Z - box.max.Z;

            float distance = (float)(Math.Sqrt((x * x) + (y * y) + (z * z)));
            result.radius = distance * 0.5f;
        }

        /// <summary>
        /// Constructs a <see cref="Sphere"/> from a given box.
        /// </summary>
        /// <param name="box">The box that will designate the extents of the sphere.</param>
        /// <returns>The newly constructed bounding sphere.</returns>
        public static Sphere FromBox(BoundingBox box)
        {
            Sphere result;
            FromBox(in box, out result);
            return result;
        }

        /// <summary>
        /// Constructs a <see cref="Sphere"/> that is the as large as the total combined area of the two specified spheres.
        /// </summary>
        /// <param name="value1">The first sphere to merge.</param>
        /// <param name="value2">The second sphere to merge.</param>
        /// <param name="result">When the method completes, contains the newly constructed bounding sphere.</param>
        public static void Merge(in Sphere value1, in Sphere value2, out Sphere result)
        {
            vec3 difference = value2.center - value1.center;

            float length = difference.Length();
            float radius = value1.radius;
            float radius2 = value2.radius;

            if (radius + radius2 >= length)
            {
                if (radius - radius2 >= length)
                {
                    result = value1;
                    return;
                }

                if (radius2 - radius >= length)
                {
                    result = value2;
                    return;
                }
            }

            vec3 vector = difference * (1.0f / length);
            float min = Math.Min(-radius, length - radius2);
            float max = (Math.Max(radius, length + radius2) - min) * 0.5f;

            result.center = value1.center + vector * (max + min);
            result.radius = max;
        }

        /// <summary>
        /// Constructs a <see cref="Sphere"/> that is the as large as the total combined area of the two specified spheres.
        /// </summary>
        /// <param name="value1">The first sphere to merge.</param>
        /// <param name="value2">The second sphere to merge.</param>
        /// <returns>The newly constructed bounding sphere.</returns>
        public static Sphere Merge(Sphere value1, Sphere value2)
        {
            Sphere result;
            Merge(in value1, in value2, out result);
            return result;
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(Sphere left, Sphere right)
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
        public static bool operator !=(Sphere left, Sphere right)
        {
            return !left.Equals(in right);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Center:{0} Radius:{1}", center.ToString(), radius.ToString());
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

            return string.Format(CultureInfo.CurrentCulture, "Center:{0} Radius:{1}", center.ToString(format, CultureInfo.CurrentCulture),
                radius.ToString(format, CultureInfo.CurrentCulture));
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
            return string.Format(formatProvider, "Center:{0} Radius:{1}", center.ToString(), radius.ToString());
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

            return string.Format(formatProvider, "Center:{0} Radius:{1}", center.ToString(format, formatProvider),
                radius.ToString(format, formatProvider));
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (center.GetHashCode() * 397) ^ radius.GetHashCode();
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="Vector4"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="Vector4"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="Vector4"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(in Sphere value)
        {
            return center == value.center && radius == value.radius;
        }

        /// <summary>
        /// Determines whether the specified <see cref="Vector4"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="Vector4"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="Vector4"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(Sphere value)
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
            if (!(value is Sphere))
                return false;

            var strongValue = (Sphere)value;
            return Equals(in strongValue);
        }
    }
}
