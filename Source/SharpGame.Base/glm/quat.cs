using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    using global::System.Runtime.InteropServices;
    using global::System.Runtime.Serialization;
    using static glm;
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DataContract]
    public struct quat
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public readonly static quat Identity = new quat(1, 0, 0, 0);

        public float this[int index]
        {
            get
            {
                if (index == 0) return x;
                else if (index == 1) return y;
                else if (index == 2) return z;
                else if (index == 3) return w;
                else throw new Exception("Out of range.");
            }
            set
            {
                if (index == 0) x = value;
                else if (index == 1) y = value;
                else if (index == 2) z = value;
                else if (index == 3) w = value;
                else throw new Exception("Out of range.");
            }
        }

        public quat(float s)
        {
            x = y = z = w = s;
        }

        public quat(float w, float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public quat(float s, vec3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
            w = s;
        }

        public quat(vec3 u, vec3 v)
        {
            float norm_u_norm_v = sqrt(dot(u, u) * dot(v, v));
            float real_part = norm_u_norm_v + dot(u, v);
            vec3 t;

            if (real_part < (1.0E-6F) * norm_u_norm_v)
            {
                // If u and v are exactly opposite, rotate 180 degrees
                // around an arbitrary orthogonal axis. Axis normalisation
                // can happen later, when we normalise the quaternion.
                real_part = (0);
                t = abs(u.x) > abs(u.z) ? vec3(-u.y, u.x, (0)) : vec3((0), -u.z, u.y);
            }
            else
            {
                // Otherwise, build quaternion the standard way.
                t = cross(u, v);
            }

            this = normalize(quat(real_part, t.x, t.y, t.z));
        }

        public quat(vec3 eulerAngle)
        {
            vec3 c = cos(eulerAngle * 0.5f);
            vec3 s = sin(eulerAngle * 0.5f);

            w = c.x * c.y * c.z + s.x * s.y * s.z;
            x = s.x * c.y * c.z - c.x * s.y * s.z;
            y = c.x * s.y * c.z + s.x * c.y * s.z;
            z = c.x * c.y * s.z - s.x * s.y * c.z;
        }

        public quat(float x, float y, float z)
            : this(new vec3(x, y, z))
        {
        }

        public vec3 EulerAngles => vec3(Pitch, Yaw, Roll);

        public float Roll => atan((2) * (x * y + w * z), w * w + x * x - y * y - z * z);

        public float Pitch
        {
            get
            {
                //return float(atan(float(2) * (q.y * q.z + q.w * q.x), q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z));
                float y = (2) * (this.y * z + w * this.x);
                float x = w * w - this.x * this.x - this.y * this.y + z * z;

                if (y == 0 && x == 0)
                {
                    return 2 * atan(this.x, w);
                }

                return atan(y, x);
            }
        }

        public float Yaw
        {
            get
            {
                return asin(clamp((-2) * (x * z - w * y), (-1), (1)));
            }
        }

        public static quat operator +(quat lhs, quat rhs)
        {
            return new quat(lhs.w + rhs.w, lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
        }

        public static quat operator +(quat lhs, float rhs)
        {
            return new quat(lhs.w + rhs, lhs.x + rhs, lhs.y + rhs, lhs.z + rhs);
        }

        public static quat operator -(quat lhs, float rhs)
        {
            return new quat(lhs.w - rhs, lhs.x - rhs, lhs.y - rhs, lhs.z - rhs);
        }

        public static quat operator -(quat lhs, quat rhs)
        {
            return new quat(lhs.w - rhs.w, lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
        }

        public static quat operator *(quat self, float s)
        {
            return new quat(self.w * s, self.x * s, self.y * s, self.z * s);
        }

        public static quat operator *(float lhs, quat rhs)
        {
            return new quat(rhs.w * lhs, rhs.x * lhs, rhs.y * lhs, rhs.z * lhs);
        }

        public static quat operator *(quat p, quat q)
        {
            return quat(p.w * q.w - p.x * q.x - p.y * q.y - p.z * q.z,
                        p.w * q.x + p.x * q.w + p.y * q.z - p.z * q.y,
                        p.w * q.y + p.y * q.w + p.z * q.x - p.x * q.z,
                        p.w * q.z + p.z * q.w + p.x * q.y - p.y * q.x);
        }

        public static quat operator /(quat lhs, float rhs)
        {
            return new quat(lhs.w / rhs, lhs.x / rhs, lhs.y / rhs, lhs.z / rhs);
        }

        public static quat operator +(quat lhs)
        {
            return lhs;
        }

        public static quat operator -(quat lhs)
        {
            return new quat(-lhs.w, -lhs.x, -lhs.y, -lhs.z);
        }

        public static vec3 operator *(quat q, vec3 v)
        {
            vec3 QuatVector = vec3(q.x, q.y, q.z);
            vec3 uv = cross(QuatVector, v);
            vec3 uuv = cross(QuatVector, uv);
            return v + ((uv * q.w) + uuv) * (2);
        }

        public static vec3 operator *(vec3 v, quat q)
        {
            return inverse(q) * v;
        }

        public float[] ToArray()
        {
            return new[] { x, y, z, w };
        }

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
            if (obj.GetType() == typeof(quat))
            {
                var vec = (quat)obj;
                if (x == vec.x && y == vec.y && z == vec.z && w == vec.w)
                    return true;
            }

            return false;
        }
        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="v1">The first Vector.</param>
        /// <param name="v2">The second Vector.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(quat v1, quat v2)
        {
            return v1.Equals(v2);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="v1">The first Vector.</param>
        /// <param name="v2">The second Vector.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(quat v1, quat v2)
        {
            return !v1.Equals(v2);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode() ^ w.GetHashCode();
        }

        #endregion

        #region ToString support

        public override string ToString()
        {
            return string.Format("[{0}, {1}, {2}, {3}]", x, y, z, w);
        }

        #endregion
    }


    public static partial class glm
    {
        public static quat quat(vec3 euler)
        {
            return new quat(euler);
        }

        public static quat quat(float w, float x, float y, float z)
        {
            return new quat(w, x, y, z);
        }

        public static quat quat_identity()
        {
            return quat((1), (0), (0), (0));
        }

        public static float length(quat q)
        {
            return sqrt(dot(q, q));
        }

        public static float length2(quat q)
        {
            return q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
        }

        public static quat normalize(quat q)
        {
            float len = length(q);
            if (len <= (0)) // Problem
                return quat((1), (0), (0), (0));
            float oneOverLen = (1) / len;
            return quat(q.w * oneOverLen, q.x * oneOverLen, q.y * oneOverLen, q.z * oneOverLen);
        }

        public static float dot(quat a, quat b)
        {
            vec4 tmp = vec4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
            return (tmp.x + tmp.y) + (tmp.z + tmp.w);
        }

        public static quat conjugate(quat q)
        {
            return quat(q.w, -q.x, -q.y, -q.z);
        }

        public static quat inverse(quat q)
        {
            return conjugate(q) / dot(q, q);
        }

        public static quat cross(quat q1, quat q2)
        {
            return quat(
                q1.w * q2.w - q1.x * q2.x - q1.y * q2.y - q1.z * q2.z,
                q1.w * q2.x + q1.x * q2.w + q1.y * q2.z - q1.z * q2.y,
                q1.w * q2.y + q1.y * q2.w + q1.z * q2.x - q1.x * q2.z,
                q1.w * q2.z + q1.z * q2.w + q1.x * q2.y - q1.y * q2.x);
        }


        public static quat mix(quat x, quat y, float a)
        {
            float cosTheta = dot(x, y);

            // Perform a linear interpolation when cosTheta is close to 1 to avoid side effect of sin(angle) becoming a zero denominator
            if (cosTheta > (1) - float.Epsilon)
            {
                // Linear interpolation
                return quat(
                    mix(x.w, y.w, a),
                    mix(x.x, y.x, a),
                    mix(x.y, y.y, a),
                    mix(x.z, y.z, a));
            }
            else
            {
                // Essential Mathematics, page 467
                float angle = acos(cosTheta);
                return (sin(((1) - a) * angle) * x + sin(a * angle) * y) / sin(angle);
            }
        }


        public static quat lerp(quat x, quat y, float a)
        {
            // Lerp is only defined in [0, 1]
            System.Diagnostics.Debug.Assert(a >= (0));
            System.Diagnostics.Debug.Assert(a <= (1));

            return x * ((1) - a) + (y * a);
        }

        public static quat slerp(quat x, quat y, float a)
        {
            quat z = y;

            float cosTheta = dot(x, y);

            // If cosTheta < 0, the interpolation will take the long way around the sphere. 
            // To fix this, one quat must be negated.
            if (cosTheta < (0))
            {
                z = -y;
                cosTheta = -cosTheta;
            }

            // Perform a linear interpolation when cosTheta is close to 1 to avoid side effect of sin(angle) becoming a zero denominator
            if (cosTheta > (1) - float.Epsilon)
            {
                // Linear interpolation
                return quat(
                    mix(x.w, z.w, a),
                    mix(x.x, z.x, a),
                    mix(x.y, z.y, a),
                    mix(x.z, z.z, a));
            }
            else
            {
                // Essential Mathematics, page 467
                float angle = acos(cosTheta);
                return (sin(((1) - a) * angle) * x + sin(a * angle) * z) / sin(angle);
            }
        }


        public static quat rotate(quat q, float angle, vec3 v)
        {
            vec3 Tmp = v;

            // Axis of rotation must be normalised
            float len = length(Tmp);
            if (abs(len - (1)) > (0.001f))
            {
                float oneOverLen = (1) / len;
                Tmp.x *= oneOverLen;
                Tmp.y *= oneOverLen;
                Tmp.z *= oneOverLen;
            }

            float AngleRad = (angle);
            float Sin = sin(AngleRad * (0.5f));

            return q * quat(cos(AngleRad * (0.5f)), Tmp.x * Sin, Tmp.y * Sin, Tmp.z * Sin);
            //return gtc::quaternion::cross(q, quat(cos(AngleRad * float(0.5)), Tmp.x * fSin, Tmp.y * fSin, Tmp.z * fSin));
        }

        public static mat3 mat3_cast(quat q)
        {
            mat3 Result = mat3(1);
            float qxx = (q.x * q.x);
            float qyy = (q.y * q.y);
            float qzz = (q.z * q.z);
            float qxz = (q.x * q.z);
            float qxy = (q.x * q.y);
            float qyz = (q.y * q.z);
            float qwx = (q.w * q.x);
            float qwy = (q.w * q.y);
            float qwz = (q.w * q.z);

            Result[0][0] = (1) - (2) * (qyy + qzz);
            Result[0][1] = (2) * (qxy + qwz);
            Result[0][2] = (2) * (qxz - qwy);

            Result[1][0] = (2) * (qxy - qwz);
            Result[1][1] = (1) - (2) * (qxx + qzz);
            Result[1][2] = (2) * (qyz + qwx);

            Result[2][0] = (2) * (qxz + qwy);
            Result[2][1] = (2) * (qyz - qwx);
            Result[2][2] = (1) - (2) * (qxx + qyy);
            return Result;
        }

        public static mat4 mat4_cast(quat q)
        {
            mat4 Result = mat4(1);
            float qxx = (q.x * q.x);
            float qyy = (q.y * q.y);
            float qzz = (q.z * q.z);
            float qxz = (q.x * q.z);
            float qxy = (q.x * q.y);
            float qyz = (q.y * q.z);
            float qwx = (q.w * q.x);
            float qwy = (q.w * q.y);
            float qwz = (q.w * q.z);

            Result[0][0] = (1) - (2) * (qyy + qzz);
            Result[0][1] = (2) * (qxy + qwz);
            Result[0][2] = (2) * (qxz - qwy);

            Result[1][0] = (2) * (qxy - qwz);
            Result[1][1] = (1) - (2) * (qxx + qzz);
            Result[1][2] = (2) * (qyz + qwx);

            Result[2][0] = (2) * (qxz + qwy);
            Result[2][1] = (2) * (qyz - qwx);
            Result[2][2] = (1) - (2) * (qxx + qyy);
            return Result;
        }

        public static quat quat_cast(mat3 m)
        {
            float fourXSquaredMinus1 = m[0][0] - m[1][1] - m[2][2];
            float fourYSquaredMinus1 = m[1][1] - m[0][0] - m[2][2];
            float fourZSquaredMinus1 = m[2][2] - m[0][0] - m[1][1];
            float fourWSquaredMinus1 = m[0][0] + m[1][1] + m[2][2];

            int biggestIndex = 0;
            float fourBiggestSquaredMinus1 = fourWSquaredMinus1;
            if (fourXSquaredMinus1 > fourBiggestSquaredMinus1)
            {
                fourBiggestSquaredMinus1 = fourXSquaredMinus1;
                biggestIndex = 1;
            }
            if (fourYSquaredMinus1 > fourBiggestSquaredMinus1)
            {
                fourBiggestSquaredMinus1 = fourYSquaredMinus1;
                biggestIndex = 2;
            }
            if (fourZSquaredMinus1 > fourBiggestSquaredMinus1)
            {
                fourBiggestSquaredMinus1 = fourZSquaredMinus1;
                biggestIndex = 3;
            }

            float biggestVal = sqrt(fourBiggestSquaredMinus1 + (1)) * (0.5f);
            float mult = (0.25f) / biggestVal;

            switch (biggestIndex)
            {
                case 0:
                    return quat(biggestVal, (m[1][2] - m[2][1]) * mult, (m[2][0] - m[0][2]) * mult, (m[0][1] - m[1][0]) * mult);
                case 1:
                    return quat((m[1][2] - m[2][1]) * mult, biggestVal, (m[0][1] + m[1][0]) * mult, (m[2][0] + m[0][2]) * mult);
                case 2:
                    return quat((m[2][0] - m[0][2]) * mult, (m[0][1] + m[1][0]) * mult, biggestVal, (m[1][2] + m[2][1]) * mult);
                case 3:
                    return quat((m[0][1] - m[1][0]) * mult, (m[2][0] + m[0][2]) * mult, (m[1][2] + m[2][1]) * mult, biggestVal);
                default: // Silence a -Wswitch-default warning in GCC. Should never actually get here. Assert is just for sanity.
                         //assert(false);
                    return quat(1, 0, 0, 0);
            }
        }

        public static float angle(quat x)
        {
            return acos(x.w) * (2);
        }

        public static vec3 axis(quat x)
        {
            float tmp1 = (1) - x.w * x.w;
            if (tmp1 <= (0))
                return vec3(0, 0, 1);
            float tmp2 = (1) / sqrt(tmp1);
            return vec3(x.x * tmp2, x.y * tmp2, x.z * tmp2);
        }

        public static quat angleAxis(float angle, vec3 v)
        {
            quat Result;
            float a = (angle);
            float s = sin(a * 0.5f);

            Result.w = cos(a * 0.5f);
            Result.x = v.x * s;
            Result.y = v.y * s;
            Result.z = v.z * s;
            return Result;
        }

        public static quat rotation(vec3 orig, vec3 dest)
        {
            float cosTheta = dot(orig, dest);
            vec3 rotationAxis;

            if (cosTheta >= (1) - float.Epsilon)
            {
                // orig and dest point in the same direction
                return quat_identity();
            }

            if (cosTheta < (-1) + float.Epsilon)
            {
                // special case when vectors in opposite directions :
                // there is no "ideal" rotation axis
                // So guess one; any will do as long as it's perpendicular to start
                // This implementation favors a rotation around the Up axis (Y),
                // since it's often what you want to do.
                rotationAxis = cross(vec3(0, 0, 1), orig);
                if (length2(rotationAxis) < float.Epsilon) // bad luck, they were parallel, try again!
                    rotationAxis = cross(vec3(1, 0, 0), orig);

                rotationAxis = normalize(rotationAxis);
                return angleAxis(pi(), rotationAxis);
            }

            // Implementation from Stan Melax's Game Programming Gems 1 article
            rotationAxis = cross(orig, dest);

            float s = sqrt(((1) + cosTheta) * (2));
            float invs = (1) / s;

            return quat(
                s * (0.5f),
                rotationAxis.x * invs,
                rotationAxis.y * invs,
                rotationAxis.z * invs);
        }

        public static quat quatYawPitchRoll(float yaw, float pitch, float roll)
        {
            yawPitchRoll(yaw, pitch, roll, out mat3 res);
            return quat_cast(res);
        }

        public static quat quatLookAt(vec3 direction, vec3 up)
        {
#if GLM_LEFT_HANDED
            return quatLookAtLH(direction, up);
#else
            return quatLookAtRH(direction, up);
#endif
        }

        public static quat quatLookAtRH(vec3 direction, vec3 up)
        {
            mat3 Result;

            Result[2] = -direction;
            Result[0] = normalize(cross(up, Result[2]));
            Result[1] = cross(Result[2], Result[0]);

            return quat_cast(Result);
        }

        public static quat quatLookAtLH(vec3 direction, vec3 up)
        {
            mat3 Result;

            Result[2] = direction;
            Result[0] = normalize(cross(up, Result[2]));
            Result[1] = cross(Result[2], Result[0]);

            return quat_cast(Result);
        }
    }
}
