using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SharpGame
{
    /// <summary>
    /// Represents a 3x3 matrix.
    /// </summary>
    public struct mat3 : IEquatable<mat3>
    {
        public float M11, M12, M13;
        public float M21, M22, M23;
        public float M31, M32, M33;


        #region Construction
        public mat3(float m00, float m01, float m02,
            float m10, float m11, float m12,
            float m20, float m21, float m22)
        {
            M11 = m00; M12 = m01; M13 = m02;
            M21 = m10; M22 = m11; M23 = m12;
            M31 = m20; M32 = m21; M33 = m22;
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

        public mat3(in vec3 a, in vec3 b, in vec3 c)
        {
            M11 = a.x; M12 = a.y; M13 = a.z;
            M21 = b.x; M22 = b.y; M23 = b.z;
            M31 = c.x; M32 = c.y; M33 = c.z;
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
            get
            {
                unsafe
                {
                    fixed (float* value = &M11)
                    {
                        return ref Unsafe.As<float, vec3>(ref value[column * 3]);
                    }
                }
            }
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
        public ref float this[int column, int row]
        {
            get { return ref this[column][row]; }
            //set { this[column][row] = value; }
        }

        #endregion

        #region Multiplication

        /// <summary>
        /// Multiplies the <paramref name="lhs"/> matrix by the <paramref name="rhs"/> vector.
        /// </summary>
        /// <param name="lhs">The LHS matrix.</param>
        /// <param name="rhs">The RHS vector.</param>
        /// <returns>The product of <paramref name="lhs"/> and <paramref name="rhs"/>.</returns>
        public static vec3 operator *(in mat3 lhs, in vec3 rhs)
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
            return string.Format(
                "[{0}, {1}, {2}; {3}, {4}, {5}; {6}, {7}, {8}]",
                this[0, 0], this[1, 0], this[2, 0],
                this[0, 1], this[1, 1], this[2, 1],
                this[0, 2], this[1, 2], this[2, 2]
            );
        }
        #endregion

        #region comparision
        
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
        
        public static bool operator ==(in mat3 m1, in mat3 m2)
        {
            return m1.Equals(m2);
        }

        public static bool operator !=(in mat3 m1, in mat3 m2)
        {
            return !m1.Equals(m2);
        }

        public override int GetHashCode()
        {
            return this[0].GetHashCode() ^ this[1].GetHashCode() ^ this[2].GetHashCode();
        }

        public bool Equals(mat3 other)
        {
            return Equals(in other);
        }

        public unsafe bool Equals(in mat3 other)
        {
            fixed(float* value = &M11)
            fixed (float* value1 = &other.M11)
            for (int i = 0; i < 9; i++)
            {
                if (value[i] != value1[i])
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

    }

    public static partial class glm
    {
        public static mat3 mat3(float scale)
        {
            return new mat3(scale);
        }
        
        public static mat3 translate(in mat3 m, vec2 v)
        {
            mat3 Result = (m);
            Result[2] = m[0] * v[0] + m[1] * v[1] + m[2];
            return Result;
        }

        public static mat3 rotate(in mat3 m, float angle)
        {
            float a = angle;
            float c = cos(a);
            float s = sin(a);

            return new mat3
            (
                m[0] * c + m[1] * s,
                m[0] * -s + m[1] * c,
                m[2]
            );
        }


        public static mat3 scale(in mat3 m, vec2 v)
        {
            return new mat3(m[0] * v[0], m[1] * v[1], m[2]);            
        }

        public static void transformation2D(in vec2 translation, float rotation, in vec2 scaling, out mat3 result)
        {
            result = mat3(1);
            result = scale(in result, scaling);
            result = rotate(in result, rotation);
            result = translate(in result, translation);
        }

        public static mat3 transformation2D(in vec2 translation, float rotation, in vec2 scaling)
        {
            mat3 result;
            transformation2D(in translation, rotation, in scaling, out result);
            return result;
        }

        public static mat3 inverse(in mat3 m)
        {
            float OneOverDeterminant = (1f) / (
                + m[0][0] * (m[1][1] * m[2][2] - m[2][1] * m[1][2])
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

        public static mat3 transpose(in mat3 m)
		{
            return new mat3(m.M11, m.M21, m.M31, m.M12, m.M22, m.M32, m.M13, m.M23, m.M33);
		}

        public static float determinant(in mat3 m)
		{
			return
				+ m[0][0] * (m[1][1] * m[2][2] - m[2][1] * m[1][2])
				- m[1][0] * (m[0][1] * m[2][2] - m[2][1] * m[0][2])
				+ m[2][0] * (m[0][1] * m[1][2] - m[1][1] * m[0][2]);
		}


    }
}

