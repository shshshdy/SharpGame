using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SharpGame
{
    /// <summary>
    /// Represents a 2x2 matrix.
    /// </summary>
    public unsafe struct mat2
    {
        #region Construction

        public mat2(float m00, float m01, float m10, float m11)
        {
            value[0] = m00; value[1] = m01;
            value[2] = m10; value[3] = m11;
        }

        public mat2(float scale)
        {
            value[0] = scale; value[1] = 0.0f;
            value[2] = 0.0f; value[3] = scale;
        }

        public mat2(vec2 a, vec2 b)
        {
            value[0] = a.x; value[1] = a.y;
            value[2] = b.x; value[3] = b.y;
        }

        /// <summary>
        /// Creates an identity matrix.
        /// </summary>
        /// <returns>A new identity matrix.</returns>
        public static mat2 identity()
        {
            return new mat2
            (
                new vec2(1, 0),
                new vec2(0, 1)
            );
        }

        #endregion

        #region Index Access

        /// <summary>
        /// Gets or sets the <see cref="vec2"/> column at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="vec2"/> column.
        /// </value>
        /// <param name="column">The column index.</param>
        /// <returns>The column at index <paramref name="column"/>.</returns>
        public ref vec2 this[int column]
        {
            get { return ref Unsafe.As<float, vec2>(ref value[column << 1]); }
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

        #endregion

        #region Multiplication

        /// <summary>
        /// Multiplies the <paramref name="lhs"/> matrix by the <paramref name="rhs"/> vector.
        /// </summary>
        /// <param name="lhs">The LHS matrix.</param>
        /// <param name="rhs">The RHS vector.</param>
        /// <returns>The product of <paramref name="lhs"/> and <paramref name="rhs"/>.</returns>
        public static vec2 operator *(mat2 lhs, vec2 rhs)
        {
            return new vec2(
                lhs[0, 0] * rhs[0] + lhs[1, 0] * rhs[1],
                lhs[0, 1] * rhs[0] + lhs[1, 1] * rhs[1]
            );
        }

        /// <summary>
        /// Multiplies the <paramref name="lhs"/> matrix by the <paramref name="rhs"/> matrix.
        /// </summary>
        /// <param name="lhs">The LHS matrix.</param>
        /// <param name="rhs">The RHS matrix.</param>
        /// <returns>The product of <paramref name="lhs"/> and <paramref name="rhs"/>.</returns>
        public static mat2 operator *(mat2 lhs, mat2 rhs)
        {
            return new mat2 (
                lhs[0][0] * rhs[0] + lhs[1][0] * rhs[1],
                lhs[0][1] * rhs[0] + lhs[1][1] * rhs[1]
            );
        }

        public static mat2 operator *(mat2 lhs, float s)
        {
            return new mat2 (
                lhs[0]*s,
                lhs[1]*s
            );
        }

        #endregion

        #region ToString support

        public override string ToString()
        {
            return String.Format(
                "[{0}, {1}; {2}, {3}]",
                this[0, 0], this[1, 0],
                this[0, 1], this[1, 1]
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
            if (obj.GetType() == typeof(mat2))
            {
                var mat = (mat2)obj;
                if (mat[0] == this[0] && mat[1] == this[1])
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
        public static bool operator ==(mat2 m1, mat2 m2)
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
        public static bool operator !=(mat2 m1, mat2 m2)
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
            return this[0].GetHashCode() ^ this[1].GetHashCode();
        }
        
        #endregion

        private fixed float value[4];
    }

    public static partial class glm
    {
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

    }
}
