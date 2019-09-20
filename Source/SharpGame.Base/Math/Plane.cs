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
    /// Represents a plane in three dimensional space.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Plane : IEquatable<Plane>, IFormattable
    {
        /// <summary>
        /// The normal vector of the plane.
        /// </summary>
        public vec3 normal;

        /// <summary>
        /// The distance of the plane along its normal from the origin.
        /// </summary>
        public float d;

        public vec3 AbsNormal { get { return new vec3(Math.Abs(normal.X), Math.Abs(normal.Y), Math.Abs(normal.Z)); } }
        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> struct.
        /// </summary>
        /// <param name="value">The value that will be assigned to all components.</param>
        public Plane(float value)
        {
            normal.x = normal.y = normal.z = d = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> struct.
        /// </summary>
        /// <param name="a">The X component of the normal.</param>
        /// <param name="b">The Y component of the normal.</param>
        /// <param name="c">The Z component of the normal.</param>
        /// <param name="d">The distance of the plane along its normal from the origin.</param>
        public Plane(float a, float b, float c, float d)
        {
            normal.x = a;
            normal.y = b;
            normal.z = c;
            this.d = d;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SharpGame.Plane" /> class.
        /// </summary>
        /// <param name="point">Any point that lies along the plane.</param>
        /// <param name="normal">The normal vector to the plane.</param>
        public Plane(vec3 point, vec3 normal)
        {
            this.normal = normal;
            this.d = -vec3.Dot(normal, point);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> struct.
        /// </summary>
        /// <param name="value">The normal of the plane.</param>
        /// <param name="d">The distance of the plane along its normal from the origin</param>
        public Plane(vec3 value, float d)
        {
            normal = value;
            this.d = d;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> struct.
        /// </summary>
        /// <param name="point1">First point of a triangle defining the plane.</param>
        /// <param name="point2">Second point of a triangle defining the plane.</param>
        /// <param name="point3">Third point of a triangle defining the plane.</param>
        public Plane(vec3 point1, vec3 point2, vec3 point3)
        {
            float x1 = point2.X - point1.X;
            float y1 = point2.Y - point1.Y;
            float z1 = point2.Z - point1.Z;
            float x2 = point3.X - point1.X;
            float y2 = point3.Y - point1.Y;
            float z2 = point3.Z - point1.Z;
            float yz = (y1 * z2) - (z1 * y2);
            float xz = (z1 * x2) - (x1 * z2);
            float xy = (x1 * y2) - (y1 * x2);
            float invPyth = 1.0f / (float)(Math.Sqrt((yz * yz) + (xz * xz) + (xy * xy)));

            normal.x = yz * invPyth;
            normal.y = xz * invPyth;
            normal.z = xy * invPyth;
            d = -((normal.X * point1.X) + (normal.Y * point1.Y) + (normal.Z * point1.Z));
        }

        public void Define(in vec3 point1, in vec3 point2, in vec3 point3)
        {
            float x1 = point2.X - point1.X;
            float y1 = point2.Y - point1.Y;
            float z1 = point2.Z - point1.Z;
            float x2 = point3.X - point1.X;
            float y2 = point3.Y - point1.Y;
            float z2 = point3.Z - point1.Z;
            float yz = (y1 * z2) - (z1 * y2);
            float xz = (z1 * x2) - (x1 * z2);
            float xy = (x1 * y2) - (y1 * x2);
            float invPyth = 1.0f / (float)(Math.Sqrt((yz * yz) + (xz * xz) + (xy * xy)));

            normal.X = yz * invPyth;
            normal.Y = xz * invPyth;
            normal.Z = xy * invPyth;
            d = -((normal.X * point1.X) + (normal.Y * point1.Y) + (normal.Z * point1.Z));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> struct.
        /// </summary>
        /// <param name="values">The values to assign to the A, B, C, and D components of the plane. This must be an array with four elements.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than four elements.</exception>
        public Plane(float[] values)
        {
            if(values == null)
                throw new ArgumentNullException("values");
            if(values.Length != 4)
                throw new ArgumentOutOfRangeException("values", "There must be four and only four input values for Plane.");

            normal.x = values[0];
            normal.y = values[1];
            normal.z = values[2];
            d = values[3];
        }

        /// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        /// <value>The value of the A, B, C, or D component, depending on the index.</value>
        /// <param name="index">The index of the component to access. Use 0 for the A component, 1 for the B component, 2 for the C component, and 3 for the D component.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 3].</exception>
        public float this[int index]
        {
            get
            {
                switch(index)
                {
                    case 0: return normal.X;
                    case 1: return normal.Y;
                    case 2: return normal.Z;
                    case 3: return d;
                }

                throw new ArgumentOutOfRangeException("index", "Indices for Plane run from 0 to 3, inclusive.");
            }

            set
            {
                switch(index)
                {
                    case 0: normal.X = value; break;
                    case 1: normal.Y = value; break;
                    case 2: normal.Z = value; break;
                    case 3: d = value; break;
                    default: throw new ArgumentOutOfRangeException("index", "Indices for Plane run from 0 to 3, inclusive.");
                }
            }
        }

        /// <summary>
        /// Changes the coefficients of the normal vector of the plane to make it of unit length.
        /// </summary>
        public void Normalize()
        {
            float magnitude = 1.0f / (float)(Math.Sqrt((normal.X * normal.X) + (normal.Y * normal.Y) + (normal.Z * normal.Z)));

            normal.X *= magnitude;
            normal.Y *= magnitude;
            normal.Z *= magnitude;
            d *= magnitude;
        }

        /// Return signed distance to a point.
        public float Distance(in vec3 point) { return vec3.Dot(normal, point) + d; }

        /// <summary>
        /// Creates an array containing the elements of the plane.
        /// </summary>
        /// <returns>A four-element array containing the components of the plane.</returns>
        public float[] ToArray()
        {
            return new float[] { normal.X, normal.Y, normal.Z, d };
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a point.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public PlaneIntersectionType Intersects(in vec3 point)
        {
            return Collision.PlaneIntersectsPoint(in this, in point);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Ray"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in Ray ray)
        {
            float distance;
            return Collision.RayIntersectsPlane(in ray, in this, out distance);
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
            return Collision.RayIntersectsPlane(in ray, in this, out distance);
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
            return Collision.RayIntersectsPlane(in ray, in this, out point);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in Plane plane)
        {
            return Collision.PlaneIntersectsPlane(in this, in plane);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <param name="line">When the method completes, contains the line of intersection
        /// as a <see cref="Ray"/>, or a zero ray if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public bool Intersects(in Plane plane, out Ray line)
        {
            return Collision.PlaneIntersectsPlane(in this, in plane, out line);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a triangle.
        /// </summary>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public PlaneIntersectionType Intersects(in vec3 vertex1, in vec3 vertex2, in vec3 vertex3)
        {
            return Collision.PlaneIntersectsTriangle(in this, in vertex1, in vertex2, in vertex3);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public PlaneIntersectionType Intersects(in BoundingBox box)
        {
            return Collision.PlaneIntersectsBox(in this, in box);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public PlaneIntersectionType Intersects(in Sphere sphere)
        {
            return Collision.PlaneIntersectsSphere(in this, in sphere);
        }

        /// <summary>
        /// Scales the plane by the given scaling factor.
        /// </summary>
        /// <param name="value">The plane to scale.</param>
        /// <param name="scale">The amount by which to scale the plane.</param>
        /// <param name="result">When the method completes, contains the scaled plane.</param>
        public static void Multiply(in Plane value, float scale, out Plane result)
        {
            result.normal.x = value.normal.X * scale;
            result.normal.y = value.normal.Y * scale;
            result.normal.z = value.normal.Z * scale;
            result.d = value.d * scale;
        }

        /// <summary>
        /// Scales the plane by the given scaling factor.
        /// </summary>
        /// <param name="value">The plane to scale.</param>
        /// <param name="scale">The amount by which to scale the plane.</param>
        /// <returns>The scaled plane.</returns>
        public static Plane Multiply(Plane value, float scale)
        {
            return new Plane(value.normal.X * scale, value.normal.Y * scale, value.normal.Z * scale, value.d * scale);
        }

        /// <summary>
        /// Calculates the dot product of the specified vector and plane.
        /// </summary>
        /// <param name="left">The source plane.</param>
        /// <param name="right">The source vector.</param>
        /// <param name="result">When the method completes, contains the dot product of the specified plane and vector.</param>
        public static void Dot(in Plane left, in vec4 right, out float result)
        {
            result = (left.normal.X * right.x) + (left.normal.Y * right.y) + (left.normal.Z * right.z) + (left.d * right.w);
        }

        /// <summary>
        /// Calculates the dot product of the specified vector and plane.
        /// </summary>
        /// <param name="left">The source plane.</param>
        /// <param name="right">The source vector.</param>
        /// <returns>The dot product of the specified plane and vector.</returns>
        public static float Dot(Plane left, vec4 right)
        {
            return (left.normal.X * right.x) + (left.normal.Y * right.y) + (left.normal.Z * right.z) + (left.d * right.w);
        }

        /// <summary>
        /// Calculates the dot product of a specified vector and the normal of the plane plus the distance value of the plane.
        /// </summary>
        /// <param name="left">The source plane.</param>
        /// <param name="right">The source vector.</param>
        /// <param name="result">When the method completes, contains the dot product of a specified vector and the normal of the Plane plus the distance value of the plane.</param>
        public static void DotCoordinate(in Plane left, in vec3 right, out float result)
        {
            result = (left.normal.X * right.X) + (left.normal.Y * right.Y) + (left.normal.Z * right.Z) + left.d;
        }

        /// <summary>
        /// Calculates the dot product of a specified vector and the normal of the plane plus the distance value of the plane.
        /// </summary>
        /// <param name="left">The source plane.</param>
        /// <param name="right">The source vector.</param>
        /// <returns>The dot product of a specified vector and the normal of the Plane plus the distance value of the plane.</returns>
        public static float DotCoordinate(Plane left, vec3 right)
        {
            return (left.normal.X * right.X) + (left.normal.Y * right.Y) + (left.normal.Z * right.Z) + left.d;
        }

        /// <summary>
        /// Calculates the dot product of the specified vector and the normal of the plane.
        /// </summary>
        /// <param name="left">The source plane.</param>
        /// <param name="right">The source vector.</param>
        /// <param name="result">When the method completes, contains the dot product of the specified vector and the normal of the plane.</param>
        public static void DotNormal(in Plane left, in vec3 right, out float result)
        {
            result = (left.normal.X * right.X) + (left.normal.Y * right.Y) + (left.normal.Z * right.Z);
        }

        /// <summary>
        /// Calculates the dot product of the specified vector and the normal of the plane.
        /// </summary>
        /// <param name="left">The source plane.</param>
        /// <param name="right">The source vector.</param>
        /// <returns>The dot product of the specified vector and the normal of the plane.</returns>
        public static float DotNormal(Plane left, vec3 right)
        {
            return (left.normal.X * right.X) + (left.normal.Y * right.Y) + (left.normal.Z * right.Z);
        }

        /// <summary>
        /// Changes the coefficients of the normal vector of the plane to make it of unit length.
        /// </summary>
        /// <param name="plane">The source plane.</param>
        /// <param name="result">When the method completes, contains the normalized plane.</param>
        public static void Normalize(in Plane plane, out Plane result)
        {
            float magnitude = 1.0f / (float)(Math.Sqrt((plane.normal.X * plane.normal.X) + (plane.normal.Y * plane.normal.Y) + (plane.normal.Z * plane.normal.Z)));

            result.normal.x = plane.normal.X * magnitude;
            result.normal.y = plane.normal.Y * magnitude;
            result.normal.z = plane.normal.Z * magnitude;
            result.d = plane.d * magnitude;
        }

        /// <summary>
        /// Changes the coefficients of the normal vector of the plane to make it of unit length.
        /// </summary>
        /// <param name="plane">The source plane.</param>
        /// <returns>The normalized plane.</returns>
        public static Plane Normalize(Plane plane)
        {
            float magnitude = 1.0f / (float)(Math.Sqrt((plane.normal.X * plane.normal.X) + (plane.normal.Y * plane.normal.Y) + (plane.normal.Z * plane.normal.Z)));
            return new Plane(plane.normal.X * magnitude, plane.normal.Y * magnitude, plane.normal.Z * magnitude, plane.d * magnitude);
        }

        /// <summary>
        /// Scales a plane by the given value.
        /// </summary>
        /// <param name="scale">The amount by which to scale the plane.</param>
        /// <param name="plane">The plane to scale.</param>
        /// <returns>The scaled plane.</returns>
        public static Plane operator *(float scale, Plane plane)
        {
            return new Plane(plane.normal.X * scale, plane.normal.Y * scale, plane.normal.Z * scale, plane.d * scale);
        }

        /// <summary>
        /// Scales a plane by the given value.
        /// </summary>
        /// <param name="plane">The plane to scale.</param>
        /// <param name="scale">The amount by which to scale the plane.</param>
        /// <returns>The scaled plane.</returns>
        public static Plane operator *(Plane plane, float scale)
        {
            return new Plane(plane.normal.X * scale, plane.normal.Y * scale, plane.normal.Z * scale, plane.d * scale);
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(Plane left, Plane right)
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
        public static bool operator !=(Plane left, Plane right)
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
            return string.Format(CultureInfo.CurrentCulture, "A:{0} B:{1} C:{2} D:{3}", normal.X, normal.Y, normal.Z, d);
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
            return string.Format(CultureInfo.CurrentCulture, "A:{0} B:{1} C:{2} D:{3}", normal.X.ToString(format, CultureInfo.CurrentCulture),
                normal.Y.ToString(format, CultureInfo.CurrentCulture), normal.Z.ToString(format, CultureInfo.CurrentCulture), d.ToString(format, CultureInfo.CurrentCulture));
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
            return string.Format(formatProvider, "A:{0} B:{1} C:{2} D:{3}", normal.X, normal.Y, normal.Z, d);
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
            return string.Format(formatProvider, "A:{0} B:{1} C:{2} D:{3}", normal.X.ToString(format, formatProvider),
                normal.Y.ToString(format, formatProvider), normal.Z.ToString(format, formatProvider), d.ToString(format, formatProvider));
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
                return (normal.GetHashCode() * 397) ^ d.GetHashCode();
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="vec4"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="vec4"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="vec4"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(in Plane value)
        {
            return normal == value.normal && d == value.d;
        }

        /// <summary>
        /// Determines whether the specified <see cref="vec4"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="vec4"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="vec4"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(Plane value)
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
            if(!(value is Plane))
                return false;

            var strongValue = (Plane)value;
            return Equals(in strongValue);
        }
    }
}
