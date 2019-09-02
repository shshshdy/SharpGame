using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SharpGame
{
    /// <summary>
    /// Represents a 4x4 matrix.
    /// </summary>
    public unsafe struct mat4
    {
        #region Construction

        public mat4(float scale)
            : this(scale, 0.0f, 0.0f, 0.0f,
            0.0f, scale, 0.0f, 0.0f,
            0.0f, 0.0f, scale, 0.0f,
            0.0f, 0.0f, 0.0f, scale)
        {
        }

        public mat4(vec4 a, vec4 b, vec4 c, vec4 d)
        {
            value[0] = a.x; value[1] = a.y; value[2] = a.z; value[3] = a.w;
            value[4] = b.x; value[5] = b.y; value[6] = b.z; value[7] = b.w;
            value[8] = c.x; value[9] = c.y; value[10] = c.z; value[11] = c.w;
            value[12] = d.x; value[13] = d.y; value[14] = d.z; value[15] = d.w;
        }

        public mat4(float m00, float m01, float m02, float m03,
            float m10, float m11, float m12, float m13,
            float m20, float m21, float m22, float m23,
            float m30, float m31, float m32, float m33)
        {
            value[0] = m00; value[1] = m01; value[2] = m02; value[3] = m03;
            value[4] = m10; value[5] = m11; value[6] = m12; value[7] = m13;
            value[8] = m20; value[9] = m21; value[10] = m22; value[11] = m23;
            value[12] = m30; value[13] = m31; value[14] = m32; value[15] = m33;
        }

        #endregion

        #region Index Access

        /// <summary>
        /// Gets or sets the <see cref="vec4"/> column at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="vec4"/> column.
        /// </value>
        /// <param name="column">The column index.</param>
        /// <returns>The column at index <paramref name="column"/>.</returns>
        public ref vec4 this[int column]
        {
            get { return ref Unsafe.As<float, vec4>(ref value[column << 2]); }
        }

        /// <summary>
        /// Gets or sets the element at <paramref name="column"/> and <paramref name="row"/>.
        /// </summary>
        /// <value>
        /// The element at <paramref name="column"/> and <paramref name="row"/>.
        /// </value>
        /// <param name="column">The column index.</param>
        /// <param name="row">The row index.</param>
        /// <returns>
        /// The element at <paramref name="column"/> and <paramref name="row"/>.
        /// </returns>
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
            new vec3(this[0][0], this[0][1], this[0][2]),
            new vec3(this[1][0], this[1][1], this[1][2]),
            new vec3(this[2][0], this[2][1], this[2][2]));
        }

        #endregion

        #region Multiplication

        /// <summary>
        /// Multiplies the <paramref name="lhs"/> matrix by the <paramref name="rhs"/> vector.
        /// </summary>
        /// <param name="lhs">The LHS matrix.</param>
        /// <param name="rhs">The RHS vector.</param>
        /// <returns>The product of <paramref name="lhs"/> and <paramref name="rhs"/>.</returns>
        public static vec4 operator *(mat4 lhs, vec4 rhs)
        {
            return new vec4(
                lhs[0, 0] * rhs[0] + lhs[1, 0] * rhs[1] + lhs[2, 0] * rhs[2] + lhs[3, 0] * rhs[3],
                lhs[0, 1] * rhs[0] + lhs[1, 1] * rhs[1] + lhs[2, 1] * rhs[2] + lhs[3, 1] * rhs[3],
                lhs[0, 2] * rhs[0] + lhs[1, 2] * rhs[1] + lhs[2, 2] * rhs[2] + lhs[3, 2] * rhs[3],
                lhs[0, 3] * rhs[0] + lhs[1, 3] * rhs[1] + lhs[2, 3] * rhs[2] + lhs[3, 3] * rhs[3]
            );
        }

        /// <summary>
        /// Multiplies the <paramref name="lhs"/> matrix by the <paramref name="rhs"/> matrix.
        /// </summary>
        /// <param name="lhs">The LHS matrix.</param>
        /// <param name="rhs">The RHS matrix.</param>
        /// <returns>The product of <paramref name="lhs"/> and <paramref name="rhs"/>.</returns>
        public static mat4 operator *(mat4 lhs, mat4 rhs)
        {
            return new mat4(              
                rhs[0][0] * lhs[0] + rhs[0][1] * lhs[1] + rhs[0][2] * lhs[2] + rhs[0][3] * lhs[3],
                rhs[1][0] * lhs[0] + rhs[1][1] * lhs[1] + rhs[1][2] * lhs[2] + rhs[1][3] * lhs[3],
                rhs[2][0] * lhs[0] + rhs[2][1] * lhs[1] + rhs[2][2] * lhs[2] + rhs[2][3] * lhs[3],
                rhs[3][0] * lhs[0] + rhs[3][1] * lhs[1] + rhs[3][2] * lhs[2] + rhs[3][3] * lhs[3]
            );
        }

        public static mat4 operator *(mat4 lhs, float s)
        {
            return new mat4(lhs[0]*s, lhs[1]*s, lhs[2]*s, lhs[3]*s);
        }

        #endregion

        #region ToString support
            
        public override string ToString()
        {
            return String.Format(
                "[{0}, {1}, {2}, {3}; {4}, {5}, {6}, {7}; {8}, {9}, {10}, {11}; {12}, {13}, {14}, {15}]",
                this[0, 0], this[1, 0], this[2, 0], this[3, 0],
                this[0, 1], this[1, 1], this[2, 1], this[3, 1],
                this[0, 2], this[1, 2], this[2, 2], this[3, 2],
                this[0, 3], this[1, 3], this[2, 3], this[3, 3]
            );
        }
        #endregion

        #region Comparision
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// The Difference is detected by the different values
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(mat4))
            {
                var mat = (mat4)obj;
                if (mat[0] == this[0] && mat[1] == this[1] && mat[2] == this[2] && mat[3] == this[3])
                    return true;
            }

            return false;
        }
        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="m1">The first Matrix.</param>
        /// <param name="m2">The second Matrix.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(mat4 m1, mat4 m2)
        {
            return m1.Equals(m2);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="m1">The first Matrix.</param>
        /// <param name="m2">The second Matrix.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(mat4 m1, mat4 m2)
        {
            return !m1.Equals(m2);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this[0].GetHashCode() ^ this[1].GetHashCode() ^ this[2].GetHashCode() ^ this[3].GetHashCode();
        }
        #endregion

        fixed float value[16];
    }

    public static partial class glm
    {
        public static mat4 identity()
        {
            return new mat4
            (
                new vec4(1,0,0,0),
                new vec4(0,1,0,0),
                new vec4(0,0,1,0),
                new vec4(0,0,0,1)
            );
        }

        public static mat4 mat4(float scale)
        {
            return new mat4(scale);
        }

        public static mat2 inverse(mat2 m)
        {

            float OneOverDeterminant = (1f) / (
                +m[0][0] * m[1][1]
                - m[1][0] * m[0][1]);

            mat2 Inverse = new mat2(
                +m[1][1] * OneOverDeterminant,
                -m[0][1] * OneOverDeterminant,
                -m[1][0] * OneOverDeterminant,
                +m[0][0] * OneOverDeterminant);

            return Inverse;
        }

        public static mat3 inverse(mat3 m)
        {
            float OneOverDeterminant = (1f) / (
                +m[0][0] * (m[1][1] * m[2][2] - m[2][1] * m[1][2])
                - m[1][0] * (m[0][1] * m[2][2] - m[2][1] * m[0][2])
                + m[2][0] * (m[0][1] * m[1][2] - m[1][1] * m[0][2]));

            mat3 Inverse = new mat3(0);
            Inverse[0, 0] = +(m[1][1] * m[2][2] - m[2][1] * m[1][2]) * OneOverDeterminant;
            Inverse[1, 0] = -(m[1][0] * m[2][2] - m[2][0] * m[1][2]) * OneOverDeterminant;
            Inverse[2, 0] = +(m[1][0] * m[2][1] - m[2][0] * m[1][1]) * OneOverDeterminant;
            Inverse[0, 1] = -(m[0][1] * m[2][2] - m[2][1] * m[0][2]) * OneOverDeterminant;
            Inverse[1, 1] = +(m[0][0] * m[2][2] - m[2][0] * m[0][2]) * OneOverDeterminant;
            Inverse[2, 1] = -(m[0][0] * m[2][1] - m[2][0] * m[0][1]) * OneOverDeterminant;
            Inverse[0, 2] = +(m[0][1] * m[1][2] - m[1][1] * m[0][2]) * OneOverDeterminant;
            Inverse[1, 2] = -(m[0][0] * m[1][2] - m[1][0] * m[0][2]) * OneOverDeterminant;
            Inverse[2, 2] = +(m[0][0] * m[1][1] - m[1][0] * m[0][1]) * OneOverDeterminant;

            return Inverse;

        }

        public static mat4 inverse(mat4 m)
        {
            float Coef00 = m[2][2] * m[3][3] - m[3][2] * m[2][3];
            float Coef02 = m[1][2] * m[3][3] - m[3][2] * m[1][3];
            float Coef03 = m[1][2] * m[2][3] - m[2][2] * m[1][3];

            float Coef04 = m[2][1] * m[3][3] - m[3][1] * m[2][3];
            float Coef06 = m[1][1] * m[3][3] - m[3][1] * m[1][3];
            float Coef07 = m[1][1] * m[2][3] - m[2][1] * m[1][3];

            float Coef08 = m[2][1] * m[3][2] - m[3][1] * m[2][2];
            float Coef10 = m[1][1] * m[3][2] - m[3][1] * m[1][2];
            float Coef11 = m[1][1] * m[2][2] - m[2][1] * m[1][2];

            float Coef12 = m[2][0] * m[3][3] - m[3][0] * m[2][3];
            float Coef14 = m[1][0] * m[3][3] - m[3][0] * m[1][3];
            float Coef15 = m[1][0] * m[2][3] - m[2][0] * m[1][3];

            float Coef16 = m[2][0] * m[3][2] - m[3][0] * m[2][2];
            float Coef18 = m[1][0] * m[3][2] - m[3][0] * m[1][2];
            float Coef19 = m[1][0] * m[2][2] - m[2][0] * m[1][2];

            float Coef20 = m[2][0] * m[3][1] - m[3][0] * m[2][1];
            float Coef22 = m[1][0] * m[3][1] - m[3][0] * m[1][1];
            float Coef23 = m[1][0] * m[2][1] - m[2][0] * m[1][1];

            vec4 Fac0 = new vec4(Coef00, Coef00, Coef02, Coef03);
            vec4 Fac1 = new vec4(Coef04, Coef04, Coef06, Coef07);
            vec4 Fac2 = new vec4(Coef08, Coef08, Coef10, Coef11);
            vec4 Fac3 = new vec4(Coef12, Coef12, Coef14, Coef15);
            vec4 Fac4 = new vec4(Coef16, Coef16, Coef18, Coef19);
            vec4 Fac5 = new vec4(Coef20, Coef20, Coef22, Coef23);

            vec4 Vec0 = new vec4(m[1][0], m[0][0], m[0][0], m[0][0]);
            vec4 Vec1 = new vec4(m[1][1], m[0][1], m[0][1], m[0][1]);
            vec4 Vec2 = new vec4(m[1][2], m[0][2], m[0][2], m[0][2]);
            vec4 Vec3 = new vec4(m[1][3], m[0][3], m[0][3], m[0][3]);

            vec4 Inv0 = new vec4(Vec1 * Fac0 - Vec2 * Fac1 + Vec3 * Fac2);
            vec4 Inv1 = new vec4(Vec0 * Fac0 - Vec2 * Fac3 + Vec3 * Fac4);
            vec4 Inv2 = new vec4(Vec0 * Fac1 - Vec1 * Fac3 + Vec3 * Fac5);
            vec4 Inv3 = new vec4(Vec0 * Fac2 - Vec1 * Fac4 + Vec2 * Fac5);

            vec4 SignA = new vec4(+1, -1, +1, -1);
            vec4 SignB = new vec4(-1, +1, -1, +1);
            mat4 Inverse = new mat4(Inv0 * SignA, Inv1 * SignB, Inv2 * SignA, Inv3 * SignB);

            vec4 Row0 = new vec4(Inverse[0][0], Inverse[1][0], Inverse[2][0], Inverse[3][0]);

            vec4 Dot0 = new vec4(m[0] * Row0);
            float Dot1 = (Dot0.x + Dot0.y) + (Dot0.z + Dot0.w);

            float OneOverDeterminant = (1f) / Dot1;

            return Inverse * OneOverDeterminant;
        }
    }
}
