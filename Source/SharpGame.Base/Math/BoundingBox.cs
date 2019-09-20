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

using Microsoft.Win32.SafeHandles;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace SharpGame
{
    /// <summary>
    /// Represents an axis-aligned bounding box in three dimensional space.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DataContract]
    public struct BoundingBox : IEquatable<BoundingBox>, IFormattable
    {
        public static readonly BoundingBox Empty = new BoundingBox(
            new vec3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity), new vec3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity));
        /// <summary>
        /// The minimum point of the box.
        /// </summary>
        [DataMember(Order = 0)]
        public vec3 Minimum;

        /// <summary>
        /// The maximum point of the box.
        /// </summary>
        [DataMember(Order = 1)]
        public vec3 Maximum;

        /// Return center.
        [IgnoreDataMember]
        public vec3 Center => (Maximum + Minimum) * 0.5f;

        /// Return size.
        [IgnoreDataMember]
        public vec3 Size => Maximum - Minimum;

        [IgnoreDataMember]
        public float Volume { get { vec3 sz = Size; return sz.x * sz.y * sz.z; } }

        /// Return half-size.
        [IgnoreDataMember]
        public vec3 HalfSize => (Maximum - Minimum) * 0.5f;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox"/> struct.
        /// </summary>
        /// <param name="minimum">The minimum vertex of the bounding box.</param>
        /// <param name="maximum">The maximum vertex of the bounding box.</param>
        public BoundingBox(vec3 minimum, vec3 maximum)
        {
            this.Minimum = minimum;
            this.Maximum = maximum;
        }

        public BoundingBox(float min, float max)
        {
            Minimum = new vec3(min, min, min);
            Maximum = new vec3(max, max, max);
        }

        public void Clear()
        {
            Minimum = new vec3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Maximum = new vec3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        }

        public void Define(BoundingBox box)=> Define(in box);

        /// Define from another bounding box.
        public void Define(in BoundingBox box)
        {
            Define(in box.Minimum, in box.Maximum);
        }

        /// Define from minimum and maximum vectors.
        public void Define(in vec3 min, in vec3 max)
        {
            Minimum = min;
            Maximum = max;
        }

        /// Define from minimum and maximum floats (all dimensions same.)
        public void Define(float min, float max)
        {
            Minimum = new vec3(min, min, min);
            Maximum = new vec3(max, max, max);
        }

        /// Define from a point.
        public void Define(in vec3 point)
        {
            Minimum = Maximum = point;
        }

        /// Return true if this bounding box is defined via a previous call to Define() or Merge().
        public bool Defined()
        {
            return Minimum.X != float.PositiveInfinity;
        }

        /// <summary>
        /// Retrieves the eight corners of the bounding box.
        /// </summary>
        /// <returns>An array of points representing the eight corners of the bounding box.</returns>
        public vec3[] GetCorners()
        {
            vec3[] results = new vec3[8];
            GetCorners(results);
            return results;
        }

        /// <summary>
        /// Retrieves the eight corners of the bounding box.
        /// </summary>
        /// <returns>An array of points representing the eight corners of the bounding box.</returns>
        public void GetCorners(vec3[] corners)
        {
            corners[0] = new vec3(Minimum.X, Maximum.Y, Maximum.Z);
            corners[1] = new vec3(Maximum.X, Maximum.Y, Maximum.Z);
            corners[2] = new vec3(Maximum.X, Minimum.Y, Maximum.Z);
            corners[3] = new vec3(Minimum.X, Minimum.Y, Maximum.Z);
            corners[4] = new vec3(Minimum.X, Maximum.Y, Minimum.Z);
            corners[5] = new vec3(Maximum.X, Maximum.Y, Minimum.Z);
            corners[6] = new vec3(Maximum.X, Minimum.Y, Minimum.Z);
            corners[7] = new vec3(Minimum.X, Minimum.Y, Minimum.Z);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Ray"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in Ray ray)
        {
            float distance;
            return Collision.RayIntersectsBox(in ray, in this, out distance);
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
            return Collision.RayIntersectsBox(in ray, in this, out distance);
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
            return Collision.RayIntersectsBox(in ray, in this, out point);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public PlaneIntersectionType Intersects(in Plane plane)
        {
            return Collision.PlaneIntersectsBox(in plane, in this);
        }

        /* This implementation is wrong
        /// <summary>
        /// Determines if there is an intersection between the current object and a triangle.
        /// </summary>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in vec3 vertex1, in vec3 vertex2, in vec3 vertex3)
        {
            return Collision.BoxIntersectsTriangle(in this, in vertex1, in vertex2, in vertex3);
        }
        */

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in BoundingBox box)
        {
            return Collision.BoxIntersectsBox(in this, in box);
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
            return Collision.BoxIntersectsSphere(in this, in sphere);
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

        public Intersection Contains(in vec3 point)
        {
            return Collision.BoxContainsPoint(in this, in point);
        }

        public Intersection Contains(vec3 point)
        {
            return Contains(in point);
        }

        /// <summary>
        /// Determines whether the current objects contains a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public Intersection Contains(in BoundingBox box)
        {
            return Collision.BoxContainsBox(in this, in box);
        }

        /// <summary>
        /// Determines whether the current objects contains a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public Intersection Contains(BoundingBox box)
        {
            return Contains(in box);
        }

        /// <summary>
        /// Determines whether the current objects contains a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public Intersection Contains(in Sphere sphere)
        {
            return Collision.BoxContainsSphere(in this, in sphere);
        }

        /// <summary>
        /// Determines whether the current objects contains a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public Intersection Contains(Sphere sphere)
        {
            return Contains(in sphere);
        }

        /// Return transformed by a 4x4 matrix.
        public BoundingBox Transformed(in mat4 transform)
        {
            vec3 center = Center;
            vec3.Transform(in center, in transform, out vec3 newCenter);
            vec3 oldEdge = Size * 0.5f;
            vec3 newEdge;
            newEdge = RotateExtents(in oldEdge, in transform);
            return new BoundingBox(newCenter - newEdge, newCenter + newEdge);
        }

        vec3 RotateExtents(in vec3 extents, in mat4 rotation)
        {
            vec3 newExtents = new vec3();
            for (int i = 0; i < 3; i++)
                newExtents[i] = Math.Abs(rotation[0, i] * extents.x) + Math.Abs(rotation[1, i] * extents.y) + Math.Abs(rotation[2, i] * extents.z);
            return newExtents;
        }

        /// Merge a point.
        public void Merge(vec3 point) => Merge(in point);
        public void Merge(in vec3 point)
        {
            if (point.X < Minimum.X)
                Minimum.X = point.X;
            if (point.Y < Minimum.Y)
                Minimum.Y = point.Y;
            if (point.Z < Minimum.Z)
                Minimum.Z = point.Z;
            if (point.X > Maximum.X)
                Maximum.X = point.X;
            if (point.Y > Maximum.Y)
                Maximum.Y = point.Y;
            if (point.Z > Maximum.Z)
                Maximum.Z = point.Z;
        }

        /// Merge another bounding box.
        public void Merge(BoundingBox box) => Merge(in box);
        public void Merge(in BoundingBox box)
        {
            glm.min(in Minimum, in box.Minimum, out Minimum);
            glm.max(in Maximum, in box.Maximum, out Maximum);
        }

        /// Merge another bounding box.
        public void Merge(Sphere sphere) => Merge(in sphere);
        public void Merge(in Sphere sphere)
        {
            vec3 center = sphere.center;
            float radius = sphere.radius;

            Merge(center + new vec3(radius, radius, radius));
            Merge(center + new vec3(-radius, -radius, -radius));
        }

        /// <summary>
        /// Constructs a <see cref="BoundingBox"/> that fully contains the given points.
        /// </summary>
        /// <param name="points">The points that will be contained by the box.</param>
        /// <param name="result">When the method completes, contains the newly constructed bounding box.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="points"/> is <c>null</c>.</exception>
        public static void FromPoints(vec3[] points, out BoundingBox result)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            vec3 min = new vec3(float.MaxValue);
            vec3 max = new vec3(float.MinValue);

            for (int i = 0; i < points.Length; ++i)
            {
                glm.min(in min, in points[i], out min);
                glm.max(in max, in points[i], out max);
            }

            result = new BoundingBox(min, max);
        }

        /// <summary>
        /// Constructs a <see cref="BoundingBox"/> that fully contains the given points.
        /// </summary>
        /// <param name="points">The points that will be contained by the box.</param>
        /// <returns>The newly constructed bounding box.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="points"/> is <c>null</c>.</exception>
        public static BoundingBox FromPoints(vec3[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            vec3 min = new vec3(float.MaxValue);
            vec3 max = new vec3(float.MinValue);

            for (int i = 0; i < points.Length; ++i)
            {
                glm.min(in min, in points[i], out min);
                glm.max(in max, in points[i], out max);
            }

            return new BoundingBox(min, max);
        }

        /// <summary>
        /// Constructs a <see cref="BoundingBox"/> from a given sphere.
        /// </summary>
        /// <param name="sphere">The sphere that will designate the extents of the box.</param>
        /// <param name="result">When the method completes, contains the newly constructed bounding box.</param>
        public static void FromSphere(in Sphere sphere, out BoundingBox result)
        {
            result.Minimum = new vec3(sphere.center.X - sphere.radius, sphere.center.Y - sphere.radius, sphere.center.Z - sphere.radius);
            result.Maximum = new vec3(sphere.center.X + sphere.radius, sphere.center.Y + sphere.radius, sphere.center.Z + sphere.radius);
        }

        /// <summary>
        /// Constructs a <see cref="BoundingBox"/> from a given sphere.
        /// </summary>
        /// <param name="sphere">The sphere that will designate the extents of the box.</param>
        /// <returns>The newly constructed bounding box.</returns>
        public static BoundingBox FromSphere(Sphere sphere)
        {
            BoundingBox box;
            box.Minimum = new vec3(sphere.center.X - sphere.radius, sphere.center.Y - sphere.radius, sphere.center.Z - sphere.radius);
            box.Maximum = new vec3(sphere.center.X + sphere.radius, sphere.center.Y + sphere.radius, sphere.center.Z + sphere.radius);
            return box;
        }

        /// <summary>
        /// Constructs a <see cref="BoundingBox"/> that is as large as the total combined area of the two specified boxes.
        /// </summary>
        /// <param name="value1">The first box to merge.</param>
        /// <param name="value2">The second box to merge.</param>
        /// <param name="result">When the method completes, contains the newly constructed bounding box.</param>
        public static void Merge(in BoundingBox value1, in BoundingBox value2, out BoundingBox result)
        {
            glm.min(in value1.Minimum, in value2.Minimum, out result.Minimum);
            glm.max(in value1.Maximum, in value2.Maximum, out result.Maximum);
        }

        /// <summary>
        /// Constructs a <see cref="BoundingBox"/> that is as large as the total combined area of the two specified boxes.
        /// </summary>
        /// <param name="value1">The first box to merge.</param>
        /// <param name="value2">The second box to merge.</param>
        /// <returns>The newly constructed bounding box.</returns>
        public static BoundingBox Merge(BoundingBox value1, BoundingBox value2)
        {
            BoundingBox box;
            glm.min(in value1.Minimum, in value2.Minimum, out box.Minimum);
            glm.max(in value1.Maximum, in value2.Maximum, out box.Maximum);
            return box;
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(BoundingBox left, BoundingBox right)
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
        public static bool operator !=(BoundingBox left, BoundingBox right)
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
            return string.Format(CultureInfo.CurrentCulture, "Minimum:{0} Maximum:{1}", Minimum.ToString(), Maximum.ToString());
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

            return string.Format(CultureInfo.CurrentCulture, "Minimum:{0} Maximum:{1}", Minimum.ToString(format, CultureInfo.CurrentCulture),
                Maximum.ToString(format, CultureInfo.CurrentCulture));
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
            return string.Format(formatProvider, "Minimum:{0} Maximum:{1}", Minimum.ToString(), Maximum.ToString());
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

            return string.Format(formatProvider, "Minimum:{0} Maximum:{1}", Minimum.ToString(format, formatProvider),
                Maximum.ToString(format, formatProvider));
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
                return (Minimum.GetHashCode() * 397) ^ Maximum.GetHashCode();
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
        public bool Equals(in BoundingBox value)
        {
            return Minimum == value.Minimum && Maximum == value.Maximum;
        }

        /// <summary>
        /// Determines whether the specified <see cref="Vector4"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="Vector4"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="Vector4"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(BoundingBox value)
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
            if (!(value is BoundingBox))
                return false;

            var strongValue = (BoundingBox)value;
            return Equals(in strongValue);
        }

    }
}
