using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace SharpGame
{
    /// <summary>
    /// Represents a two dimensional vector.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DataContract]
    public partial struct vec2 : IEquatable<vec2>, IFormattable
    {
        public float x;
        public float y;

        public static readonly vec2 Zero = new vec2(0);
        public static readonly vec2 One = new vec2(1);

        public float X => x;
        public float Y => y;


        [IgnoreDataMember]
        public float this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                System.Diagnostics.Debug.Assert(index >= 0 && index < 2);
                return Unsafe.Add(ref x, index);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                System.Diagnostics.Debug.Assert(index >= 0 && index < 2);
                Unsafe.Add(ref x, index) = value;
            }
        }


        public vec2(float s)
        {
            x = y = s;
        }

        public vec2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public vec2(vec2 v)
        {
            this.x = v.x;
            this.y = v.y;
        }

        public vec2(vec3 v)
        {
            this.x = v.x;
            this.y = v.y;
        }

        public static vec2 operator +(vec2 lhs, vec2 rhs)
        {
            return new vec2(lhs.x + rhs.x, lhs.y + rhs.y);
        }

        public static vec2 operator +(vec2 lhs, float rhs)
        {
            return new vec2(lhs.x + rhs, lhs.y + rhs);
        }

        public static vec2 operator -(vec2 lhs, vec2 rhs)
        {
            return new vec2(lhs.x - rhs.x, lhs.y - rhs.y);
        }

        public static vec2 operator -(vec2 lhs, float rhs)
        {
            return new vec2(lhs.x - rhs, lhs.y - rhs);
        }

        public static vec2 operator *(vec2 self, float s)
        {
            return new vec2(self.x * s, self.y * s);
        }

        public static vec2 operator *(float lhs, vec2 rhs)
        {
            return new vec2(rhs.x * lhs, rhs.y * lhs);
        }

        public static vec2 operator *(vec2 lhs, vec2 rhs)
        {
            return new vec2(rhs.x * lhs.x, rhs.y * lhs.y);
        }

        public static vec2 operator /(vec2 lhs, float rhs)
        {
            return new vec2(lhs.x / rhs, lhs.y / rhs);
        }

        public float[] ToArray()
        {
            return new[] { x, y };
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(vec2 left, vec2 right)
        {
            return left.Equals(ref right);
        }

        /// <summary>
        /// Tests for inequality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator !=(vec2 left, vec2 right)
        {
            return !left.Equals(ref right);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="vec2"/> to <see cref="Vector3"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator vec3(vec2 value)
        {
            return new vec3(value, 0.0f);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="vec2"/> to <see cref="Vector4"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator vec4(vec2 value)
        {
            return new vec4(value.x, value.y, 0.0f, 0.0f);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1}", x, y);
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

            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1}", x.ToString(format, CultureInfo.CurrentCulture), y.ToString(format, CultureInfo.CurrentCulture));
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
            return string.Format(formatProvider, "X:{0} Y:{1}", x, y);
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

            return string.Format(formatProvider, "X:{0} Y:{1}", x.ToString(format, formatProvider), y.ToString(format, formatProvider));
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
                return (x.GetHashCode() * 397) ^ y.GetHashCode();
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="vec2"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="vec2"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="vec2"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(ref vec2 other)
        {
            return MathUtil.NearEqual(other.x, x) && MathUtil.NearEqual(other.y, y);
        }

        /// <summary>
        /// Determines whether the specified <see cref="vec2"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="vec2"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="vec2"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(vec2 other)
        {
            return Equals(ref other);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object value)
        {
            if (!(value is vec2))
                return false;

            var strongValue = (vec2)value;
            return Equals(ref strongValue);
        }

    }

    public static partial class glm
    {
        public static vec2 vec2(float x, float y)
        {
            return new vec2(x, y);
        }

        public static float length(vec2 v)
        {
            return (float)Math.Sqrt(dot(v, v));
        }

        public static vec2 normalize(vec2 v)
        {
            float sqr = v.x * v.x + v.y * v.y;
            return v * (1.0f / (float)Math.Sqrt(sqr));
        }

        public static float dot(vec2 a, vec2 b)
        {
            vec2 tmp = new vec2(a * b);
            return tmp.x + tmp.y;
        }

    }
}
