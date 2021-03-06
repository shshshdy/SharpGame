using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGame
{
    /// <summary>
    /// Represents a 4x4 matrix.
    /// </summary>

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    [DebuggerDisplay("M11 = {M11} M12 = {M12} M13 = {M13} M14 = {M14}")]
    public partial struct mat4 : IEquatable<mat4>
    {
        public float M11, M12, M13, M14;
        public float M21, M22, M23, M24;
        public float M31, M32, M33, M34;
        public float M41, M42, M43, M44;

        public static readonly mat4 Identity = new mat4(1);

        #region Construction

        public mat4(float scale)
            : this(scale, 0.0f, 0.0f, 0.0f,
            0.0f, scale, 0.0f, 0.0f,
            0.0f, 0.0f, scale, 0.0f,
            0.0f, 0.0f, 0.0f, scale)
        {
        }

        public mat4(in vec4 a, in vec4 b, in vec4 c, in vec4 d)
           : this(a.x, a.y, a.z, a.w,
                 b.x, b.y, b.z, b.w,
                 c.x, c.y, c.z, c.w,
                 d.x, d.y, d.z, d.w)
        {
        }

        public mat4(float m00, float m01, float m02, float m03,
            float m10, float m11, float m12, float m13,
            float m20, float m21, float m22, float m23,
            float m30, float m31, float m32, float m33)
        {
            M11 = m00; M12 = m01; M13 = m02; M14 = m03;
            M21 = m10; M22 = m11; M23 = m12; M24 = m13;
            M31 = m20; M32 = m21; M33 = m22; M34 = m23;
            M41 = m30; M42 = m31; M43 = m32; M44 = m33;
        }

        public mat4(in mat3 scale)
            : this(scale[0], scale[1], scale[2], new vec4(0.0f, 0.0f, 0.0f, 1))
        {
        }

        #endregion

        public unsafe ref vec3 TranslationVector
        {
            get
            {
                fixed (float* value = &M11)
                return ref Unsafe.As<float, vec3>(ref value[12]);
            }
        }

        #region Index Access

        public unsafe ref vec4 this[int column]
        {
            get
            {
                fixed (float* value = &M11)
                return ref Unsafe.As<float, vec4>(ref value[column << 2]);
            }
        }

        public float this[int column, int row]
        {
            get { return this[column][row]; }
            set { this[column][row] = value; }
        }

        #endregion

        #region Conversion

        /// <summary>
        /// Returns the <see cref="mat3"/> portion of this matrix.
        /// </summary>
        /// <returns>The <see cref="mat3"/> portion of this matrix.</returns>
        public mat3 to_mat3()
        {
            return new mat3(
            new vec3(M11, M12, M13),
            new vec3(M21, M22, M23),
            new vec3(M31, M32, M33));
        }

        #endregion

        #region Multiplication

        public static vec4 operator *(in mat4 lhs, in vec4 rhs)
        {
            return new vec4(
                lhs.M11 * rhs[0] + lhs.M21 * rhs[1] + lhs.M31 * rhs[2] + lhs.M41 * rhs[3],
                lhs.M12 * rhs[0] + lhs.M22 * rhs[1] + lhs.M32 * rhs[2] + lhs.M42 * rhs[3],
                lhs.M13 * rhs[0] + lhs.M23 * rhs[1] + lhs.M33 * rhs[2] + lhs.M43 * rhs[3],
                lhs.M14 * rhs[0] + lhs.M24 * rhs[1] + lhs.M34 * rhs[2] + lhs.M44 * rhs[3]
            );
        }

        public static vec3 operator *(in mat4 lhs, in vec3 rhs)
        {
            return new vec3(
                lhs.M11 * rhs[0] + lhs.M21 * rhs[1] + lhs.M31 * rhs[2] + lhs.M41,
                lhs.M12 * rhs[0] + lhs.M22 * rhs[1] + lhs.M32 * rhs[2] + lhs.M42,
                lhs.M13 * rhs[0] + lhs.M23 * rhs[1] + lhs.M33 * rhs[2] + lhs.M43
            );
        }

        /// <summary>
        /// Multiplies the <paramref name="lhs"/> matrix by the <paramref name="rhs"/> matrix.
        /// </summary>
        /// <param name="lhs">The LHS matrix.</param>
        /// <param name="rhs">The RHS matrix.</param>
        /// <returns>The product of <paramref name="lhs"/> and <paramref name="rhs"/>.</returns>
        public static mat4 operator *(in mat4 lhs, in mat4 rhs)
        {
            Multiply(lhs, rhs, out mat4 res);
            return res;
        }

        public static mat4 operator *(in mat4 lhs, float s)
        {
            return new mat4(lhs[0]*s, lhs[1]*s, lhs[2]*s, lhs[3]*s);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(in mat4 lhs, in mat4 rhs, out mat4 result)
        {
            result = new mat4
            (
                rhs.M11 * lhs.M11 + rhs.M12 * lhs.M21 + rhs.M13 * lhs.M31 + rhs.M14 * lhs.M41,
                rhs.M11 * lhs.M12 + rhs.M12 * lhs.M22 + rhs.M13 * lhs.M32 + rhs.M14 * lhs.M42,
                rhs.M11 * lhs.M13 + rhs.M12 * lhs.M23 + rhs.M13 * lhs.M33 + rhs.M14 * lhs.M43,
                rhs.M11 * lhs.M14 + rhs.M12 * lhs.M24 + rhs.M13 * lhs.M34 + rhs.M14 * lhs.M44,

                rhs.M21 * lhs.M11 + rhs.M22 * lhs.M21 + rhs.M23 * lhs.M31 + rhs.M24 * lhs.M41,
                rhs.M21 * lhs.M12 + rhs.M22 * lhs.M22 + rhs.M23 * lhs.M32 + rhs.M24 * lhs.M42,
                rhs.M21 * lhs.M13 + rhs.M22 * lhs.M23 + rhs.M23 * lhs.M33 + rhs.M24 * lhs.M43,
                rhs.M21 * lhs.M14 + rhs.M22 * lhs.M24 + rhs.M23 * lhs.M34 + rhs.M24 * lhs.M44,

                rhs.M31 * lhs.M11 + rhs.M32 * lhs.M21 + rhs.M33 * lhs.M31 + rhs.M34 * lhs.M41,
                rhs.M31 * lhs.M12 + rhs.M32 * lhs.M22 + rhs.M33 * lhs.M32 + rhs.M34 * lhs.M42,
                rhs.M31 * lhs.M13 + rhs.M32 * lhs.M23 + rhs.M33 * lhs.M33 + rhs.M34 * lhs.M43,
                rhs.M31 * lhs.M14 + rhs.M32 * lhs.M24 + rhs.M33 * lhs.M34 + rhs.M34 * lhs.M44,

                rhs.M41 * lhs.M11 + rhs.M42 * lhs.M21 + rhs.M43 * lhs.M31 + rhs.M44 * lhs.M41,
                rhs.M41 * lhs.M12 + rhs.M42 * lhs.M22 + rhs.M43 * lhs.M32 + rhs.M44 * lhs.M42,
                rhs.M41 * lhs.M13 + rhs.M42 * lhs.M23 + rhs.M43 * lhs.M33 + rhs.M44 * lhs.M43,
                rhs.M41 * lhs.M14 + rhs.M42 * lhs.M24 + rhs.M43 * lhs.M34 + rhs.M44 * lhs.M44
            );
        }

        public void Transpose()
        {
            Transpose(in this, out this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transpose(in mat4 value, out mat4 result)
        {
            mat4 temp = new mat4
            {
                M11 = value.M11,
                M12 = value.M21,
                M13 = value.M31,
                M14 = value.M41,
                M21 = value.M12,
                M22 = value.M22,
                M23 = value.M32,
                M24 = value.M42,
                M31 = value.M13,
                M32 = value.M23,
                M33 = value.M33,
                M34 = value.M43,
                M41 = value.M14,
                M42 = value.M24,
                M43 = value.M34,
                M44 = value.M44
            };

            result = temp;
        }

        #endregion

        #region ToString support

        public override string ToString()
        {
            return string.Format(
                "[{0}, {1}, {2}, {3}; {4}, {5}, {6}, {7}; {8}, {9}, {10}, {11}; {12}, {13}, {14}, {15}]",
                M11, M21, M31, M41,
                M12, M22, M32, M42,
                M13, M23, M33, M43,
                M14, M24, M34, M44
            );
        }
        #endregion

        #region Comparision

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(mat4))
            {
                var mat = (mat4)obj;
                return Equals(in mat);
            }

            return false;
        }
        
        public static bool operator ==(in mat4 m1, in mat4 m2)
        {
            return m1.Equals(m2);
        }

        public static bool operator !=(in mat4 m1, in mat4 m2)
        {
            return !m1.Equals(m2);
        }

        public override int GetHashCode()
        {
            return this[0].GetHashCode() ^ this[1].GetHashCode() ^ this[2].GetHashCode() ^ this[3].GetHashCode();
        }

        public unsafe bool Equals(in mat4 other)
        {
            fixed(float* value = &M11)
            fixed (float* value1 = &other.M11)
            for (int i = 0; i < 16; i++)
            {
                if(value[i] != value1[i])
                {
                    return false;
                }
            }

            return true;
        }

        public bool Equals(mat4 other)
        {
            return Equals(in other);
        }

        #endregion

    }

    public static partial class glm
    {
        public static mat4 mat4(float scale)
        {
            return new mat4(scale);
        }

        public static mat4 mat4(in mat3 m)
        {
            return new mat4(m);
        }

        public static void inverse(in mat4 m, out mat4 result)
        {
            float Coef00 = m.M33 * m.M44 - m.M43 * m.M34;
            float Coef02 = m.M23 * m.M44 - m.M43 * m.M24;
            float Coef03 = m.M23 * m.M34 - m.M33 * m.M24;

            float Coef04 = m.M32 * m.M44 - m.M42 * m.M34;
            float Coef06 = m.M22 * m.M44 - m.M42 * m.M24;
            float Coef07 = m.M22 * m.M34 - m.M32 * m.M24;

            float Coef08 = m.M32 * m.M43 - m.M42 * m.M33;
            float Coef10 = m.M22 * m.M43 - m.M42 * m.M23;
            float Coef11 = m.M22 * m.M33 - m.M32 * m.M23;

            float Coef12 = m.M31 * m.M44 - m.M41 * m.M34;
            float Coef14 = m.M21 * m.M44 - m.M41 * m.M24;
            float Coef15 = m.M21 * m.M34 - m.M31 * m.M24;

            float Coef16 = m.M31 * m.M43 - m.M41 * m.M33;
            float Coef18 = m.M21 * m.M43 - m.M41 * m.M23;
            float Coef19 = m.M21 * m.M33 - m.M31 * m.M23;

            float Coef20 = m.M31 * m.M42 - m.M41 * m.M32;
            float Coef22 = m.M21 * m.M42 - m.M41 * m.M22;
            float Coef23 = m.M21 * m.M32 - m.M31 * m.M22;

            vec4 Fac0 = new vec4(Coef00, Coef00, Coef02, Coef03);
            vec4 Fac1 = new vec4(Coef04, Coef04, Coef06, Coef07);
            vec4 Fac2 = new vec4(Coef08, Coef08, Coef10, Coef11);
            vec4 Fac3 = new vec4(Coef12, Coef12, Coef14, Coef15);
            vec4 Fac4 = new vec4(Coef16, Coef16, Coef18, Coef19);
            vec4 Fac5 = new vec4(Coef20, Coef20, Coef22, Coef23);

            vec4 Vec0 = new vec4(m.M21, m.M11, m.M11, m.M11);
            vec4 Vec1 = new vec4(m.M22, m.M12, m.M12, m.M12);
            vec4 Vec2 = new vec4(m.M23, m.M13, m.M13, m.M13);
            vec4 Vec3 = new vec4(m.M24, m.M14, m.M14, m.M14);

            vec4 Inv0 = new vec4(Vec1 * Fac0 - Vec2 * Fac1 + Vec3 * Fac2);
            vec4 Inv1 = new vec4(Vec0 * Fac0 - Vec2 * Fac3 + Vec3 * Fac4);
            vec4 Inv2 = new vec4(Vec0 * Fac1 - Vec1 * Fac3 + Vec3 * Fac5);
            vec4 Inv3 = new vec4(Vec0 * Fac2 - Vec1 * Fac4 + Vec2 * Fac5);

            vec4 SignA = new vec4(+1, -1, +1, -1);
            vec4 SignB = new vec4(-1, +1, -1, +1);
            mat4 Inverse = new mat4(Inv0 * SignA, Inv1 * SignB, Inv2 * SignA, Inv3 * SignB);

            vec4 Row0 = new vec4(Inverse.M11, Inverse.M21, Inverse.M31, Inverse.M41);

            vec4 Dot0 = new vec4(m[0] * Row0);
            float Dot1 = (Dot0.x + Dot0.y) + (Dot0.z + Dot0.w);

            float OneOverDeterminant = (1f) / Dot1;

            result = Inverse * OneOverDeterminant;
        }

        public static mat4 inverse(in mat4 m)
        {
            inverse(in m, out mat4 res);
            return res;
        }

        public static float determinant(in mat4 m)
		{
            float SubFactor00 = m.M33 * m.M44 - m.M43 * m.M34;
            float SubFactor01 = m.M32 * m.M44 - m.M42 * m.M34;
            float SubFactor02 = m.M32 * m.M43 - m.M42 * m.M33;
            float SubFactor03 = m.M31 * m.M44 - m.M41 * m.M34;
            float SubFactor04 = m.M31 * m.M43 - m.M41 * m.M33;
            float SubFactor05 = m.M31 * m.M42 - m.M41 * m.M32;

            vec4 DetCof = vec4(
				+ (m.M22 * SubFactor00 - m.M23 * SubFactor01 + m.M24 * SubFactor02),
				- (m.M21 * SubFactor00 - m.M23 * SubFactor03 + m.M24 * SubFactor04),
				+ (m.M21 * SubFactor01 - m.M22 * SubFactor03 + m.M24 * SubFactor05),
				- (m.M21 * SubFactor02 - m.M22 * SubFactor04 + m.M23 * SubFactor05));

			return
				m.M11 * DetCof[0] + m.M12 * DetCof[1] +
				m.M13 * DetCof[2] + m.M14 * DetCof[3];
		}

        public static mat4 transpose(in mat4 m)
		{
			mat4 Result;
            Result.M11 = m.M11;
			Result.M12 = m.M21;
			Result.M13 = m.M31;
			Result.M14 = m.M41;

			Result.M21 = m.M12;
			Result.M22 = m.M22;
			Result.M23 = m.M32;
			Result.M24 = m.M42;

			Result.M31 = m.M13;
			Result.M32 = m.M23;
			Result.M33 = m.M33;
			Result.M34 = m.M43;

			Result.M41 = m.M14;
			Result.M42 = m.M24;
			Result.M43 = m.M34;
			Result.M44 = m.M44;
			return Result;
		}

    }
}
