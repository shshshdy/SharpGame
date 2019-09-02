using System;

namespace SharpGame
{
    using global::System.Globalization;
    using global::System.Runtime.CompilerServices;
    using global::System.Runtime.InteropServices;
    using global::System.Runtime.Serialization;
    using static glm;
    /// <summary>
    /// Represents a three dimensional vector.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DataContract]
    public partial struct vec3 : IEquatable<vec3>, IFormattable
    {
        [DataMember(Order = 0)]
        public float x;
        [DataMember(Order = 1)]
        public float y;
        [DataMember(Order = 2)]
        public float z;

        public static readonly vec3 Zero = new vec3(0, 0, 0);
        public static readonly vec3 One = new vec3(1, 1, 1);

        public vec3(float s)
        {
            x = y = z = s;
        }

        public vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public vec3(vec3 v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }

        public vec3(vec4 v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }

        public vec3(vec2 xy, float z)
        {
            this.x = xy.x;
            this.y = xy.y;
            this.z = z;
        }

        public float X { get => x; set => x = value; }
        public float Y { get => y; set => y = value; }
        public float Z { get => z; set => z = value; }

        public float this[int index]
        {
            get
            {
                if (index == 0) return x;
                else if (index == 1) return y;
                else if (index == 2) return z;
                else throw new Exception("Out of range.");
            }
            set
            {
                if (index == 0) x = value;
                else if (index == 1) y = value;
                else if (index == 2) z = value;
                else throw new Exception("Out of range.");
            }
        }

        public float Length() => length(this);
        public float LengthSquared() => (x * x) + (y * y) + (z * z);
        public void Normalize()
        {
            float length = Length();
            if (!MathUtil.IsZero(length))
            {
                float inv = 1.0f / length;
                X *= inv;
                Y *= inv;
                Z *= inv;
            }
        }

        public static void Transform(ref vec3 vector, ref quat rotation, out vec3 result)
        {
            result = rotation * vector;
        }

        public static vec3 Transform(vec3 vector, quat rotation)
        {
            vec3 result;
            Transform(ref vector, ref rotation, out result);
            return result;
        }


        public static void Transform(ref vec3 vector, ref mat3 transform, out vec3 result)
        {
            result = transform* vector;
        }

        public static vec3 Transform(vec3 vector, mat3 transform)
        {
            vec3 result;
            Transform(ref vector, ref transform, out result);
            return result;
        }

        public static void Transform(ref vec3 vector, ref mat4 transform, out vec3 result)
        {
            vec4 intermediate;
            Transform(ref vector, ref transform, out intermediate);
            result = (vec3)intermediate;
        }

        public static void Transform(ref vec3 vector, ref mat4 transform, out vec4 result)
        {
            result = new vec4(
                (vector.X * transform.M11) + (vector.Y * transform.M21) + (vector.Z * transform.M31) + transform.M41,
                (vector.X * transform.M12) + (vector.Y * transform.M22) + (vector.Z * transform.M32) + transform.M42,
                (vector.X * transform.M13) + (vector.Y * transform.M23) + (vector.Z * transform.M33) + transform.M43,
                (vector.X * transform.M14) + (vector.Y * transform.M24) + (vector.Z * transform.M34) + transform.M44);
        }

        public static vec3 Transform(vec3 vector, mat4 transform) => Transform(vector, transform);

        public static vec3 Transform(ref vec3 vector, ref mat4 transform)
        {
            vec3 result;
            Transform(ref vector, ref transform, out result);
            return result;
        }

        public static void Transform(vec3[] source, ref mat4 transform, vec3[] destination)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException("destination", "The destination array must be of same length or larger length than the source array.");

            for (int i = 0; i < source.Length; ++i)
            {
                Transform(ref source[i], ref transform, out destination[i]);
            }
        }

        public static void TransformNormal(ref vec3 normal, ref mat4 transform, out vec3 result)
        {
            result = new vec3(
                (normal.X * transform.M11) + (normal.Y * transform.M21) + (normal.Z * transform.M31),
                (normal.X * transform.M12) + (normal.Y * transform.M22) + (normal.Z * transform.M32),
                (normal.X * transform.M13) + (normal.Y * transform.M23) + (normal.Z * transform.M33));
        }

        public static vec3 TransformNormal(vec3 normal, mat4 transform)
        {
            vec3 result;
            TransformNormal(ref normal, ref transform, out result);
            return result;
        }

        public static vec3 operator +(vec3 lhs, vec3 rhs)
        {
            return new vec3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
        }

        public static vec3 operator +(vec3 lhs, float rhs)
        {
            return new vec3(lhs.x + rhs, lhs.y + rhs, lhs.z + rhs);
        }

        public static vec3 operator -(vec3 lhs, vec3 rhs)
        {
            return new vec3(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
        }

        public static vec3 operator -(vec3 lhs, float rhs)
        {
            return new vec3(lhs.x - rhs, lhs.y - rhs, lhs.z - rhs);
        }

        public static vec3 operator *(vec3 self, float s)
        {
            return new vec3(self.x * s, self.y * s, self.z * s);
        }
        public static vec3 operator *(float lhs, vec3 rhs)
        {
            return new vec3(rhs.x * lhs, rhs.y * lhs, rhs.z * lhs);
        }

        public static vec3 operator /(vec3 lhs, float rhs)
        {
            return new vec3(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs);
        }

        public static vec3 operator *(vec3 lhs, vec3 rhs)
        {
            return new vec3(rhs.x * lhs.x, rhs.y * lhs.y, rhs.z * lhs.z);
        }

        public static vec3 operator +(vec3 lhs)
        {
            return new vec3(lhs.x, lhs.y, lhs.z);
        }

        public static vec3 operator -(vec3 lhs)
        {
            return new vec3(-lhs.x, -lhs.y, -lhs.z);
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(vec3 left, vec3 right)
        {
            return left.Equals(ref right);
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator !=(vec3 left, vec3 right)
        {
            return !left.Equals(ref right);
        }

        public static explicit operator vec2(vec3 value)
        {
            return new vec2(value.X, value.Y);
        }

        public float[] ToArray()
        {
            return new[] { x, y, z };
        }
 
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Z:{2}", X, Y, Z);
        }

        public string ToString(string format)
        {
            if (format == null)
                return ToString();

            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Z:{2}", X.ToString(format, CultureInfo.CurrentCulture),
                Y.ToString(format, CultureInfo.CurrentCulture), Z.ToString(format, CultureInfo.CurrentCulture));
        }

        public string ToString(IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "X:{0} Y:{1} Z:{2}", X, Y, Z);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null)
                return ToString(formatProvider);

            return string.Format(formatProvider, "X:{0} Y:{1} Z:{2}", X.ToString(format, formatProvider),
                Y.ToString(format, formatProvider), Z.ToString(format, formatProvider));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(ref vec3 other)
        {
            return MathUtil.NearEqual(other.X, X) && MathUtil.NearEqual(other.Y, Y) && MathUtil.NearEqual(other.Z, Z);
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(vec3 other)
        {
            return Equals(ref other);
        }

        public override bool Equals(object value)
        {
            if (!(value is Vector3))
                return false;

            var strongValue = (vec3)value;
            return Equals(ref strongValue);
        }

    }

    public static partial class glm
    {
        public static vec3 vec3(float x, float y, float z)
        {
            return new vec3(x, y, z);
        }

        public static vec3 vec3(vec4 v)
        {
            return new vec3(v.x, v.y, v.z);
        }

        public static float length(vec3 v)
        {
            return (float)Math.Sqrt(dot(v, v));
        }

        public static float length2(vec3 v)
        {
            return v.x * v.x + v.y * v.y + v.z * v.z;
        }

        public static vec3 cross(vec3 lhs, vec3 rhs)
        {
            return new vec3(
                lhs.y * rhs.z - rhs.y * lhs.z,
                lhs.z * rhs.x - rhs.z * lhs.x,
                lhs.x * rhs.y - rhs.x * lhs.y);
        }

        public static float dot(vec3 a, vec3 b)
        {
            vec3 tmp = new vec3(a * b);
            return tmp.x + tmp.y + tmp.z;
        }

        public static vec3 normalize(vec3 v)
        {
            float sqr = v.x * v.x + v.y * v.y + v.z * v.z;
            return v * (1.0f / (float)Math.Sqrt(sqr));
        }
    }

}
