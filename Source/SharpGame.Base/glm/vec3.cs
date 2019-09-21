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
        public static readonly vec3 UnitX = new vec3(1, 0, 0);
        public static readonly vec3 UnitY = new vec3(0, 1, 0);
        public static readonly vec3 UnitZ = new vec3(0, 0, 1);

        public static readonly vec3 Up = new vec3(0, 1, 0);
        public static readonly vec3 Right = new vec3(1, 0, 0);
        public static readonly vec3 Forward = new vec3(0, 0, 1);

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

        public vec3(in vec3 v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }

        public vec3(in vec4 v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }

        public vec3(in vec2 xy, float z)
        {
            this.x = xy.x;
            this.y = xy.y;
            this.z = z;
        }

        [IgnoreDataMember]
        public float X { get => x; set => x = value; }

        [IgnoreDataMember]
        public float Y { get => y; set => y = value; }

        [IgnoreDataMember]
        public float Z { get => z; set => z = value; }

        [IgnoreDataMember]
        public ref float this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                System.Diagnostics.Debug.Assert(index >= 0 && index < 3);
                unsafe
                {
                    fixed (float* value = &x)
                    {
                        return ref Unsafe.AsRef<float>(value + index);
                    }
                }
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

        public static void Add(in vec3 left, in vec3 right, out vec3 result)
        {
            result = new vec3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static vec3 Add(in vec3 left, in vec3 right)
        {
            return new vec3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static void Subtract(in vec3 left, in float right, out vec3 result)
        {
            result = new vec3(left.X - right, left.Y - right, left.Z - right);
        }

        public static vec3 Subtract(in vec3 left, float right)
        {
            return new vec3(left.X - right, left.Y - right, left.Z - right);
        }

        public static void Subtract(in vec3 left, in vec3 right, out vec3 result)
        {
            result = new vec3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static vec3 Subtract(in vec3 left, in vec3 right)
        {
            return new vec3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static void Cross(in vec3 left, in vec3 right, out vec3 result)
        {
            result = new vec3(
                (left.Y * right.Z) - (left.Z * right.Y),
                (left.Z * right.X) - (left.X * right.Z),
                (left.X * right.Y) - (left.Y * right.X));
        }

        public static vec3 Cross(in vec3 left, in vec3 right)
        {
            vec3 result;
            Cross(in left, in right, out result);
            return result;
        }

        public static void Distance(in vec3 value1, in vec3 value2, out float result)
        {
            float x = value1.X - value2.X;
            float y = value1.Y - value2.Y;
            float z = value1.Z - value2.Z;

            result = (float)Math.Sqrt((x * x) + (y * y) + (z * z));
        }

        public static float Distance(in vec3 value1, in vec3 value2)
        {
            float x = value1.X - value2.X;
            float y = value1.Y - value2.Y;
            float z = value1.Z - value2.Z;

            return (float)Math.Sqrt((x * x) + (y * y) + (z * z));
        }

        public static void DistanceSquared(in vec3 value1, in vec3 value2, out float result)
        {
            float x = value1.X - value2.X;
            float y = value1.Y - value2.Y;
            float z = value1.Z - value2.Z;

            result = (x * x) + (y * y) + (z * z);
        }

        public static float DistanceSquared(in vec3 value1, in vec3 value2)
        {
            float x = value1.X - value2.X;
            float y = value1.Y - value2.Y;
            float z = value1.Z - value2.Z;

            return (x * x) + (y * y) + (z * z);
        }

        public static void Dot(in vec3 left, in vec3 right, out float result)
        {
            result = (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
        }

        public static float Dot(in vec3 left, in vec3 right)
        {
            return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
        }

        public static void Transform(in vec3 vector, in quat rotation, out vec3 result)
        {
            result = rotation * vector;
        }

        public static vec3 Transform(in vec3 vector, in quat rotation)
        {
            vec3 result;
            Transform(vector, rotation, out result);
            return result;
        }

        public static void Transform(in vec3 vector, in mat3 transform, out vec3 result)
        {
            result = transform* vector;
        }

        public static vec3 Transform(in vec3 vector, in mat3 transform)
        {
            vec3 result;
            Transform(in vector, in transform, out result);
            return result;
        }

        public static void Transform(in vec3 vector, in mat4 transform, out vec3 result)
        {
            vec4 intermediate;
            Transform(in vector, in transform, out intermediate);
            result = (vec3)intermediate;
        }

        public static void Transform(in vec3 vector, in mat4 transform, out vec4 result)
        {
            result = new vec4(
                (vector.X * transform.M11) + (vector.Y * transform.M21) + (vector.Z * transform.M31) + transform.M41,
                (vector.X * transform.M12) + (vector.Y * transform.M22) + (vector.Z * transform.M32) + transform.M42,
                (vector.X * transform.M13) + (vector.Y * transform.M23) + (vector.Z * transform.M33) + transform.M43,
                (vector.X * transform.M14) + (vector.Y * transform.M24) + (vector.Z * transform.M34) + transform.M44);
        }

        public static vec3 Transform(in vec3 vector, in mat4 transform)
        {
            vec3 result;
            Transform(in vector, in transform, out result);
            return result;
        }

        public static void Transform(vec3[] source, in mat4 transform, vec3[] destination)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException("destination", "The destination array must be of same length or larger length than the source array.");

            for (int i = 0; i < source.Length; ++i)
            {
                Transform(in source[i], in transform, out destination[i]);
            }
        }

        public static void TransformCoordinate(in vec3 coordinate, in mat4 transform, out vec3 result)
        {
            vec4 vector = new vec4();
            vector.x = (coordinate.X * transform.M11) + (coordinate.Y * transform.M21) + (coordinate.Z * transform.M31) + transform.M41;
            vector.y = (coordinate.X * transform.M12) + (coordinate.Y * transform.M22) + (coordinate.Z * transform.M32) + transform.M42;
            vector.z = (coordinate.X * transform.M13) + (coordinate.Y * transform.M23) + (coordinate.Z * transform.M33) + transform.M43;
            vector.w = 1f / ((coordinate.X * transform.M14) + (coordinate.Y * transform.M24) + (coordinate.Z * transform.M34) + transform.M44);

            result = new vec3(vector.x * vector.w, vector.y * vector.w, vector.z * vector.w);
        }

        public static vec3 TransformCoordinate(in vec3 coordinate, in mat4 transform)
        {
            vec3 result;
            TransformCoordinate(in coordinate, in transform, out result);
            return result;
        }

        public static void TransformCoordinate(vec3[] source, in mat4 transform, vec3[] destination)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException("destination", "The destination array must be of same length or larger length than the source array.");

            for (int i = 0; i < source.Length; ++i)
            {
                TransformCoordinate(in source[i], in transform, out destination[i]);
            }
        }

        public static void TransformNormal(in vec3 normal, in mat4 transform, out vec3 result)
        {
            result = new vec3(
                (normal.X * transform.M11) + (normal.Y * transform.M21) + (normal.Z * transform.M31),
                (normal.X * transform.M12) + (normal.Y * transform.M22) + (normal.Z * transform.M32),
                (normal.X * transform.M13) + (normal.Y * transform.M23) + (normal.Z * transform.M33));
        }

        public static vec3 TransformNormal(in vec3 normal, in mat4 transform)
        {
            vec3 result;
            TransformNormal(in normal, in transform, out result);
            return result;
        }

        public static vec3 operator +(in vec3 lhs, in vec3 rhs)
        {
            return new vec3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
        }

        public static vec3 operator +(in vec3 lhs, float rhs)
        {
            return new vec3(lhs.x + rhs, lhs.y + rhs, lhs.z + rhs);
        }

        public static vec3 operator -(in vec3 lhs, in vec3 rhs)
        {
            return new vec3(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
        }

        public static vec3 operator -(in vec3 lhs, float rhs)
        {
            return new vec3(lhs.x - rhs, lhs.y - rhs, lhs.z - rhs);
        }

        public static vec3 operator *(in vec3 self, float s)
        {
            return new vec3(self.x * s, self.y * s, self.z * s);
        }

        public static vec3 operator *(float lhs, in vec3 rhs)
        {
            return new vec3(rhs.x * lhs, rhs.y * lhs, rhs.z * lhs);
        }

        public static vec3 operator /(in vec3 lhs, float rhs)
        {
            return new vec3(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs);
        }

        public static vec3 operator *(in vec3 lhs, in vec3 rhs)
        {
            return new vec3(rhs.x * lhs.x, rhs.y * lhs.y, rhs.z * lhs.z);
        }

        public static vec3 operator +(in vec3 lhs)
        {
            return new vec3(lhs.x, lhs.y, lhs.z);
        }

        public static vec3 operator -(in vec3 lhs)
        {
            return new vec3(-lhs.x, -lhs.y, -lhs.z);
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(in vec3 left, in vec3 right)
        {
            return left.Equals(in right);
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator !=(in vec3 left, in vec3 right)
        {
            return !left.Equals(in right);
        }

        public static explicit operator vec2(in vec3 value)
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
        public bool Equals(in vec3 other)
        {
            return other.x == x && other.y == y && other.z == z;
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(vec3 other)
        {
            return Equals(in other);
        }

        public override bool Equals(object value)
        {
            if (!(value is vec3))
                return false;

            var strongValue = (vec3)value;
            return Equals(in strongValue);
        }

    }

    public static partial class glm
    {
        public static vec3 vec3(float s)
        {
            return new vec3(s);
        }

        public static vec3 vec3(float x, float y, float z)
        {
            return new vec3(x, y, z);
        }

        public static vec3 vec3(in vec4 v)
        {
            return new vec3(v.x, v.y, v.z);
        }

        public static float length(in vec3 v)
        {
            return (float)Math.Sqrt(dot(v, v));
        }

        public static float length2(in vec3 v)
        {
            return v.x * v.x + v.y * v.y + v.z * v.z;
        }

        public static vec3 cross(in vec3 lhs, in vec3 rhs)
        {
            return new vec3(
                lhs.y * rhs.z - rhs.y * lhs.z,
                lhs.z * rhs.x - rhs.z * lhs.x,
                lhs.x * rhs.y - rhs.x * lhs.y);
        }

        public static float dot(in vec3 a, in vec3 b)
        {
            vec3 tmp = new vec3(a * b);
            return tmp.x + tmp.y + tmp.z;
        }

        public static vec3 normalize(in vec3 v)
        {
            float sqr = v.x * v.x + v.y * v.y + v.z * v.z;
            return v * (1.0f / (float)Math.Sqrt(sqr));
        }

        public static void clamp(in vec3 value, in vec3 min, in vec3 max, out vec3 result)
        {
            float x = value.X;
            x = (x > max.X) ? max.X : x;
            x = (x < min.X) ? min.X : x;

            float y = value.Y;
            y = (y > max.Y) ? max.Y : y;
            y = (y < min.Y) ? min.Y : y;

            float z = value.Z;
            z = (z > max.Z) ? max.Z : z;
            z = (z < min.Z) ? min.Z : z;

            result = new vec3(x, y, z);
        }

        public static vec3 clamp(in vec3 value, in vec3 min, in vec3 max)
        {
            vec3 result;
            clamp(value, min, max, out result);
            return result;
        }

        public static void lerp(in vec3 start, in vec3 end, float amount, out vec3 result)
        {
            result.x = MathUtil.Lerp(start.X, end.X, amount);
            result.y = MathUtil.Lerp(start.Y, end.Y, amount);
            result.z = MathUtil.Lerp(start.Z, end.Z, amount);
        }

        public static vec3 lerp(in vec3 start, in vec3 end, float amount)
        {
            vec3 result;
            lerp(start, end, amount, out result);
            return result;
        }

        public static void smoothStep(in vec3 start, in vec3 end, float amount, out vec3 result)
        {
            amount = MathUtil.SmoothStep(amount);
            lerp(start, end, amount, out result);
        }

        public static vec3 smoothStep(in vec3 start, in vec3 end, float amount)
        {
            vec3 result;
            smoothStep(start, end, amount, out result);
            return result;
        }

        public static void hermite(in vec3 value1, in vec3 tangent1, in vec3 value2, in vec3 tangent2, float amount, out vec3 result)
        {
            float squared = amount * amount;
            float cubed = amount * squared;
            float part1 = ((2.0f * cubed) - (3.0f * squared)) + 1.0f;
            float part2 = (-2.0f * cubed) + (3.0f * squared);
            float part3 = (cubed - (2.0f * squared)) + amount;
            float part4 = cubed - squared;

            result.x = (((value1.X * part1) + (value2.X * part2)) + (tangent1.X * part3)) + (tangent2.X * part4);
            result.y = (((value1.Y * part1) + (value2.Y * part2)) + (tangent1.Y * part3)) + (tangent2.Y * part4);
            result.z = (((value1.Z * part1) + (value2.Z * part2)) + (tangent1.Z * part3)) + (tangent2.Z * part4);
        }

        public static vec3 hermite(in vec3 value1, in vec3 tangent1, in vec3 value2, in vec3 tangent2, float amount)
        {
            vec3 result;
            hermite(value1, tangent1, value2, tangent2, amount, out result);
            return result;
        }

        public static void catmullRom(in vec3 value1, in vec3 value2, in vec3 value3, in vec3 value4, float amount, out vec3 result)
        {
            float squared = amount * amount;
            float cubed = amount * squared;

            result.x = 0.5f * ((((2.0f * value2.X) + ((-value1.X + value3.X) * amount)) +
            (((((2.0f * value1.X) - (5.0f * value2.X)) + (4.0f * value3.X)) - value4.X) * squared)) +
            ((((-value1.X + (3.0f * value2.X)) - (3.0f * value3.X)) + value4.X) * cubed));

            result.y = 0.5f * ((((2.0f * value2.Y) + ((-value1.Y + value3.Y) * amount)) +
                (((((2.0f * value1.Y) - (5.0f * value2.Y)) + (4.0f * value3.Y)) - value4.Y) * squared)) +
                ((((-value1.Y + (3.0f * value2.Y)) - (3.0f * value3.Y)) + value4.Y) * cubed));

            result.z = 0.5f * ((((2.0f * value2.Z) + ((-value1.Z + value3.Z) * amount)) +
                (((((2.0f * value1.Z) - (5.0f * value2.Z)) + (4.0f * value3.Z)) - value4.Z) * squared)) +
                ((((-value1.Z + (3.0f * value2.Z)) - (3.0f * value3.Z)) + value4.Z) * cubed));
        }

        public static vec3 catmullRom(in vec3 value1, in vec3 value2, in vec3 value3, in vec3 value4, float amount)
        {
            vec3 result;
            catmullRom(value1, value2, value3, value4, amount, out result);
            return result;
        }

        public static void max(in vec3 left, in  vec3 right, out vec3 result)
        {
            result.x = (left.X > right.X) ? left.X : right.X;
            result.y = (left.Y > right.Y) ? left.Y : right.Y;
            result.z = (left.Z > right.Z) ? left.Z : right.Z;
        }

        public static vec3 max(in vec3 left, in vec3 right)
        {
            vec3 result;
            max(left, right, out result);
            return result;
        }

        public static void min(in vec3 left, in vec3 right, out vec3 result)
        {
            result.x = (left.X < right.X) ? left.X : right.X;
            result.y = (left.Y < right.Y) ? left.Y : right.Y;
            result.z = (left.Z < right.Z) ? left.Z : right.Z;
        }

        public static vec3 min(in vec3 left, in vec3 right)
        {
            vec3 result;
            min(left, right, out result);
            return result;
        }
    }

}
