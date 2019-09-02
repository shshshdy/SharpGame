using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SharpGame
{
    /// <summary>
    /// Represents a 3x3 matrix.
    /// </summary>
    public unsafe struct mat3
    {
        #region Construction
        public mat3(float m00, float m01, float m02,
            float m10, float m11, float m12,
            float m20, float m21, float m22)
        {
            value[0] = m00; value[1] = m01; value[2] = m02;
            value[3] = m10; value[4] = m11; value[5] = m12;
            value[6] = m20; value[7] = m21; value[8] = m22;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="mat3"/> struct.
        /// This matrix is the identity matrix scaled by <paramref name="scale"/>.
        /// </summary>
        /// <param name="scale">The scale.</param>
        public mat3(float scale) : this(
                new vec3(scale, 0.0f, 0.0f),
                new vec3(0.0f, scale, 0.0f),
                new vec3(0.0f, 0.0f, scale))
        {  
        }

        public mat3(vec3 a, vec3 b, vec3 c)
        {
            value[0] = a.x; value[1] = a.y; value[2] = a.z;
            value[3] = b.x; value[4] = b.y; value[5] = b.z;
            value[6] = c.x; value[7] = c.y; value[8] = c.z;
        }

        #endregion

        #region Index Access

        /// <summary>
        /// Gets or sets the <see cref="vec3"/> column at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="vec3"/> column.
        /// </value>
        /// <param name="column">The column index.</param>
        /// <returns>The column at index <paramref name="column"/>.</returns>
        public ref vec3 this[int column]
        {
            get { return ref Unsafe.As<float,vec3>(ref value[column*3]) ; }
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
        public mat2 to_mat2()
        {
            return new mat2(
          new vec2(this[0][0], this[0][1]),
          new vec2(this[1][0], this[1][1]));
        }

        #endregion

        #region Multiplication

        /// <summary>
        /// Multiplies the <paramref name="lhs"/> matrix by the <paramref name="rhs"/> vector.
        /// </summary>
        /// <param name="lhs">The LHS matrix.</param>
        /// <param name="rhs">The RHS vector.</param>
        /// <returns>The product of <paramref name="lhs"/> and <paramref name="rhs"/>.</returns>
        public static vec3 operator *(mat3 lhs, vec3 rhs)
        {
            return new vec3(
                lhs[0, 0] * rhs[0] + lhs[1, 0] * rhs[1] + lhs[2, 0] * rhs[2],
                lhs[0, 1] * rhs[0] + lhs[1, 1] * rhs[1] + lhs[2, 1] * rhs[2],
                lhs[0, 2] * rhs[0] + lhs[1, 2] * rhs[1] + lhs[2, 2] * rhs[2]
            );
        }

        /// <summary>
        /// Multiplies the <paramref name="lhs"/> matrix by the <paramref name="rhs"/> matrix.
        /// </summary>
        /// <param name="lhs">The LHS matrix.</param>
        /// <param name="rhs">The RHS matrix.</param>
        /// <returns>The product of <paramref name="lhs"/> and <paramref name="rhs"/>.</returns>
        public static mat3 operator *(mat3 lhs, mat3 rhs)
        {
            return new mat3(
          lhs[0][0] * rhs[0] + lhs[1][0] * rhs[1] + lhs[2][0] * rhs[2],
          lhs[0][1] * rhs[0] + lhs[1][1] * rhs[1] + lhs[2][1] * rhs[2],
          lhs[0][2] * rhs[0] + lhs[1][2] * rhs[1] + lhs[2][2] * rhs[2]
            );
        }

        public static mat3 operator *(mat3 lhs, float s)
        {
            return new mat3(
                lhs[0]*s,
                lhs[1]*s,
                lhs[2]*s
            );
        }

        #endregion

        #region ToString support
         
        public override string ToString()
        {
            return String.Format(
                "[{0}, {1}, {2}; {3}, {4}, {5}; {6}, {7}, {8}]",
                this[0, 0], this[1, 0], this[2, 0],
                this[0, 1], this[1, 1], this[2, 1],
                this[0, 2], this[1, 2], this[2, 2]
            );
        }
        #endregion

        #region comparision
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
            if (obj.GetType() == typeof(mat3))
            {
                var mat = (mat3)obj;
                if (mat[0] == this[0] && mat[1] == this[1] && mat[2] == this[2])
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
        public static bool operator ==(mat3 m1, mat3 m2)
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
        public static bool operator !=(mat3 m1, mat3 m2)
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
            return this[0].GetHashCode() ^ this[1].GetHashCode() ^ this[2].GetHashCode();
        }
        #endregion

        /// <summary>
        /// The columms of the matrix.
        /// </summary>
        //private vec3 col1;
        //private vec3 col2;
        //private vec3 col3;
        private fixed float value[9];
    }
}
