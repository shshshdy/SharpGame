using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace SharpGame
{
    /// <summary>
    /// Represents a four dimensional vector.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DataContract]
    public unsafe partial struct vec4 : IEquatable<vec4>, IFormattable
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public static readonly vec4 Zero = new vec4(0, 0, 0, 0);
        public static readonly vec4 One = new vec4(1, 1, 1, 1);

        public ref float this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                System.Diagnostics.Debug.Assert(index >= 0 && index < 4);
                fixed (float* value = &x)
                return ref value[index];
            }
        }

        public vec4(float s)
        {
            x = y = z = w = s;
        }

        public vec4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public vec4(vec4 v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
            this.w = v.w;
        }

        public vec4(in vec3 xyz, float w)
        {
            this.x = xyz.x;
            this.y = xyz.y;
            this.z = xyz.z;
            this.w = w;
        }

        public static vec4 operator +(in vec4 lhs, in vec4 rhs)
        {
            return new vec4(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w);
        }

        public static vec4 operator +(in vec4 lhs, float rhs)
        {
            return new vec4(lhs.x + rhs, lhs.y + rhs, lhs.z + rhs, lhs.w + rhs);
        }

        public static vec4 operator -(in vec4 lhs, float rhs)
        {
            return new vec4(lhs.x - rhs, lhs.y - rhs, lhs.z - rhs, lhs.w - rhs);
        }

        public static vec4 operator -(in vec4 lhs, in vec4 rhs)
        {
            return new vec4(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w);
        }

        public static vec4 operator *(in vec4 self, float s)
        {
            return new vec4(self.x * s, self.y * s, self.z * s, self.w * s);
        }

        public static vec4 operator *(float lhs, in vec4 rhs)
        {
            return new vec4(rhs.x * lhs, rhs.y * lhs, rhs.z * lhs, rhs.w * lhs);
        }

        public static vec4 operator *(in vec4 lhs, in vec4 rhs)
        {
            return new vec4(rhs.x * lhs.x, rhs.y * lhs.y, rhs.z * lhs.z, rhs.w * lhs.w);
        }

        public static vec4 operator /(in vec4 lhs, float rhs)
        {
            return new vec4(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs);
        }

        public static implicit operator vec4(in vec3 value)
        {
            return new vec4(value.X, value.Y, value.Z, 0);
        }

        public static explicit operator vec3(in vec4 value)
        {
            return new vec3(value.x, value.y, value.z);
        }

        public float[] ToArray()
        {
            return new[] { x, y, z, w };
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(in vec4 left, in vec4 right)
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
        public static bool operator !=(in vec4 left, in vec4 right)
        {
            return !left.Equals(in right);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="vec4"/> to <see cref="Vector2"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator vec2(in vec4 value)
        {
            return new vec2(value.x, value.y);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Z:{2} W:{3}", x, y, z, w);
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

            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Z:{2} W:{3}", x.ToString(format, CultureInfo.CurrentCulture),
                y.ToString(format, CultureInfo.CurrentCulture), z.ToString(format, CultureInfo.CurrentCulture), w.ToString(format, CultureInfo.CurrentCulture));
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
            return string.Format(formatProvider, "X:{0} Y:{1} Z:{2} W:{3}", x, y, z, w);
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
                ToString(formatProvider);

            return string.Format(formatProvider, "X:{0} Y:{1} Z:{2} W:{3}", x.ToString(format, formatProvider),
                y.ToString(format, formatProvider), z.ToString(format, formatProvider), w.ToString(format, formatProvider));
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
                var hashCode = x.GetHashCode();
                hashCode = (hashCode * 397) ^ y.GetHashCode();
                hashCode = (hashCode * 397) ^ z.GetHashCode();
                hashCode = (hashCode * 397) ^ w.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="vec4"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="vec4"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="vec4"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(in vec4 other)
        {
            return (MathUtil.NearEqual(other.x, x) &&
                MathUtil.NearEqual(other.y, y) &&
                MathUtil.NearEqual(other.z, z) &&
                MathUtil.NearEqual(other.w, w));
        }

        /// <summary>
        /// Determines whether the specified <see cref="vec4"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="vec4"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="vec4"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(vec4 other)
        {
            return Equals(in other);
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
            if (!(value is vec4))
                return false;

            var strongValue = (vec4)value;
            return Equals(in strongValue);
        }

    }

    public static partial class glm
    {
        public static vec4 vec4(float x, float y, float z, float w)
        {
            return new vec4(x, y, z, w);
        }

        public static vec4 vec4(in vec3 v, float w)
        {
            return new vec4(v, w);
        }

        public static float length(in vec4 v)
        {
            return (float)Math.Sqrt(dot(v, v));
        }

        public static vec4 normalize(in vec4 v)
        {
            float sqr = v.x * v.x + v.y * v.y + v.z * v.z + v.w * v.w;
            return v * (1.0f / (float)Math.Sqrt(sqr));
        }

        public static float dot(in vec4 a, in vec4 b)
        {
            vec4 tmp = new vec4(a * b);
            return (tmp.x + tmp.y) + (tmp.z + tmp.w);
        }
    }
}
