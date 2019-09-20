#define GLM_LEFT_HANDED
#define GLM_DEPTH_ZERO_TO_ONE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGame
{
    public static partial class glm
    {
        public static mat4 scale(in vec3 v)
        {
            return new mat4(v.x, 0.0f, 0.0f, 0.0f,
            0.0f, v.y, 0.0f, 0.0f,
            0.0f, 0.0f, v.z, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f);
        }

        public static mat4 scale(in mat4 m, in vec3 v)
        {
            mat4 result = m;
            result[0] = m[0] * v[0];
            result[1] = m[1] * v[1];
            result[2] = m[2] * v[2];
            result[3] = m[3];
            return result;
        }

        public static mat4 rotate(float angle, in vec3 v)
        {
            float c = cos(angle);
            float s = sin(angle);

            vec3 axis = normalize(v);
            vec3 temp = (1.0f - c) * axis;

            mat4 result = mat4(1.0f);
            result.M11 = c + temp[0] * axis[0];
            result.M12 = 0 + temp[0] * axis[1] + s * axis[2];
            result.M13 = 0 + temp[0] * axis[2] - s * axis[1];

            result.M21 = 0 + temp[1] * axis[0] - s * axis[2];
            result.M22 = c + temp[1] * axis[1];
            result.M23 = 0 + temp[1] * axis[2] + s * axis[0];

            result.M31 = 0 + temp[2] * axis[0] + s * axis[1];
            result.M32 = 0 + temp[2] * axis[1] - s * axis[0];
            result.M33 = c + temp[2] * axis[2];
            return result;
        }

        public static mat4 rotate(in mat4 m, float angle, in vec3 v)
        {
            float c = cos(angle);
            float s = sin(angle);

            vec3 axis = normalize(v);
            vec3 temp = (1.0f - c) * axis;

            mat4 rotate = mat4(1.0f);
            rotate.M11 = c + temp[0] * axis[0];
            rotate.M12 = 0 + temp[0] * axis[1] + s * axis[2];
            rotate.M13 = 0 + temp[0] * axis[2] - s * axis[1];

            rotate.M21 = 0 + temp[1] * axis[0] - s * axis[2];
            rotate.M22 = c + temp[1] * axis[1];
            rotate.M23 = 0 + temp[1] * axis[2] + s * axis[0];

            rotate.M31 = 0 + temp[2] * axis[0] + s * axis[1];
            rotate.M32 = 0 + temp[2] * axis[1] - s * axis[0];
            rotate.M33 = c + temp[2] * axis[2];

            mat4 result = mat4(1.0f);
            result[0] = m[0] * rotate.M11 + m[1] * rotate.M12 + m[2] * rotate.M13;
            result[1] = m[0] * rotate.M21 + m[1] * rotate.M22 + m[2] * rotate.M23;
            result[2] = m[0] * rotate.M31 + m[1] * rotate.M32 + m[2] * rotate.M33;
            result[3] = m[3];
            return result;
        }

        public static mat4 rotate(in quat q)
        {
            return mat4_cast(q);
        }

        public static mat4 rotate(in mat4 m, in quat q)
        {
            mat4 result = mat4_cast(q);
            return result * m;
        }

        public static mat4 translate(in vec3 v)
        {
            return translate(mat4(1.0f), v.x, v.y, v.z);
        }

        public static mat4 translate(float x, float y, float z)
        {
            return new mat4(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                x, y, z, 1.0f);
        }

        public static mat4 translate(in mat4 m, in vec3 v)
        {
            return translate(m, v.x, v.y, v.z);
        }

        public static mat4 translate(in mat4 m, float x, float y, float z)
        {
            mat4 result = m;
            result[3] = m[0] * x + m[1] * y + m[2] * z + m[3];
            return result;
        }

        public static void yawPitchRoll(float yaw, float pitch, float roll, out mat3 Result)
        {
            float tmp_ch = cos(yaw);
            float tmp_sh = sin(yaw);
            float tmp_cp = cos(pitch);
            float tmp_sp = sin(pitch);
            float tmp_cb = cos(roll);
            float tmp_sb = sin(roll);

            Result.M11 = tmp_ch * tmp_cb + tmp_sh * tmp_sp * tmp_sb;
            Result.M12 = tmp_sb * tmp_cp;
            Result.M13 = -tmp_sh * tmp_cb + tmp_ch * tmp_sp * tmp_sb;

            Result.M21 = -tmp_ch * tmp_sb + tmp_sh * tmp_sp * tmp_cb;
            Result.M22 = tmp_cb * tmp_cp;
            Result.M23 = tmp_sb * tmp_sh + tmp_ch * tmp_sp * tmp_cb;

            Result.M31 = tmp_sh * tmp_cp;
            Result.M32 = -tmp_sp;
            Result.M33 = tmp_ch * tmp_cp;
        }

        public static mat4 yawPitchRoll(float yaw, float pitch, float roll)
        {
            float tmp_ch = cos(yaw);
            float tmp_sh = sin(yaw);
            float tmp_cp = cos(pitch);
            float tmp_sp = sin(pitch);
            float tmp_cb = cos(roll);
            float tmp_sb = sin(roll);

            mat4 Result;
            Result.M11 = tmp_ch * tmp_cb + tmp_sh * tmp_sp * tmp_sb;
            Result.M12 = tmp_sb * tmp_cp;
            Result.M13 = -tmp_sh * tmp_cb + tmp_ch * tmp_sp * tmp_sb;
            Result.M14 = 0;
            Result.M21 = -tmp_ch * tmp_sb + tmp_sh * tmp_sp * tmp_cb;
            Result.M22 = tmp_cb * tmp_cp;
            Result.M23 = tmp_sb * tmp_sh + tmp_ch * tmp_sp * tmp_cb;
            Result.M24 = 0;
            Result.M31 = tmp_sh * tmp_cp;
            Result.M32 = -tmp_sp;
            Result.M33 = tmp_ch * tmp_cp;
            Result.M34 = 0;
            Result.M41 = 0;
            Result.M42 = 0;
            Result.M43 = 0;
            Result.M44 = 1;
            return Result;
        }

        public static quat quatLookAt(in vec3 eye, in vec3 target, in vec3 up)
        {
#if GLM_LEFT_HANDED
            return quatLookAtLH(eye, target, up);
#else
            return quatLookAtRH(eye, target, up);
#endif
        }

        public static quat quatLookDirection(in vec3 direction, in vec3 up)
        {
#if GLM_LEFT_HANDED
            return quatLookDirectionLH(direction, up);
#else
            return quatLookDirectionRH(direction, up);
#endif
        }

        public static quat quatLookAtRH(in vec3 eye, in vec3 target, in vec3 up)
        {
            vec3 dir = target - eye;

            return quatLookDirectionRH(dir, up);
        }

        public static quat quatLookAtLH(in vec3 eye, in vec3 target, in vec3 up)
        {
            vec3 dir = target - eye;

            return quatLookDirectionLH(dir, up);
        }

        public static quat quatLookDirectionRH(in vec3 direction, in vec3 up)
        {
            mat3 Result;

            Result[2] = -normalize(direction);
            Result[0] = normalize(cross(up, Result[2]));
            Result[1] = cross(Result[2], Result[0]);

            return quat_cast(Result);
        }

        public static quat quatLookDirectionLH(in vec3 direction, in vec3 up)
        {
            mat3 Result;

            Result[2] = normalize(direction);
            Result[0] = normalize(cross(up, Result[2]));
            Result[1] = cross(Result[2], Result[0]);

            return quat_cast(Result);
        }

        public static void transformation(in vec3 translation, in quat rotation, out mat4 result)
        {
            //result = translate(translation) * rotate(rotation);
            result = rotate(rotation);
            result.TranslationVector = translation;
        }

        public static mat4 transformation(in vec3 translation, in quat rotation)
        {
            mat4 result;
            transformation(in translation, in rotation, out result);
            return result;
        }

        public static void transformation(in vec3 translation, in quat rotation, in vec3 scaling, out mat4 result)
        {
            //result = translate(translation) * rotate(rotation) * scale(scaling);
            result = scale(rotate(rotation), scaling);
            result.TranslationVector = translation;
        }

        public static mat4 transformation(in vec3 translation, in quat rotation, in vec3 scaling)
        {
            mat4 result;
            transformation(in translation, in rotation, in scaling, out result);
            return result;
        }

        public static mat4 ortho(float left, float right, float bottom, float top, float zNear, float zFar)
        {
#if GLM_LEFT_HANDED
            return orthoLH(left, right, bottom, top, zNear, zFar);
#else
			return orthoRH(left, right, bottom, top, zNear, zFar);
#endif
        }

        public static mat4 orthoLH(float left, float right, float bottom, float top, float zNear, float zFar)
        {
            mat4 Result = new mat4(1);
            Result.M11 = 2 / (right - left);
            Result.M22 = 2 / (top - bottom);
            Result.M41 = -(right + left) / (right - left);
            Result.M42 = -(top + bottom) / (top - bottom);

#if GLM_DEPTH_ZERO_TO_ONE
            Result.M33 = 1 / (zFar - zNear);
            Result.M43 = -zNear / (zFar - zNear);
#else
			Result.M33 = 2 / (zFar - zNear);
			Result.M43 = - (zFar + zNear) / (zFar - zNear);
#endif
            return Result;
        }

        public static mat4 orthoRH(float left, float right, float bottom, float top, float zNear, float zFar)
        {
            mat4 Result = new mat4(1);
            Result.M11 = 2 / (right - left);
            Result.M22 = 2 / (top - bottom);
            Result.M41 = -(right + left) / (right - left);
            Result.M42 = -(top + bottom) / (top - bottom);

#if GLM_DEPTH_ZERO_TO_ONE
            Result.M33 = -1 / (zFar - zNear);
            Result.M43 = -zNear / (zFar - zNear);
#else
			Result.M33 = - (2) / (zFar - zNear);
			Result.M43 = - (zFar + zNear) / (zFar - zNear);
#endif
            return Result;
        }

        public static mat4 ortho(float left, float right, float bottom, float top)
        {
            var result = mat4(1.0f);
            result.M11 = 2f / (right - left);
            result.M22 = 2f / (top - bottom);
            result.M33 = -1f;
            result.M41 = -(right + left) / (right - left);
            result.M42 = -(top + bottom) / (top - bottom);
            return result;
        }

        public static mat4 frustum(float left, float right, float bottom, float top, float nearVal, float farVal)
        {
#if GLM_LEFT_HANDED
            return frustumLH(left, right, bottom, top, nearVal, farVal);
#else
            return frustumRH(left, right, bottom, top, nearVal, farVal);
#endif
        }

        public static mat4 frustumLH(float left, float right, float bottom, float top, float nearVal, float farVal)
        {
            mat4 Result = new mat4(0);
            Result.M11 = 2 * nearVal / (right - left);
            Result.M22 = 2 * nearVal / (top - bottom);
            Result.M31 = (right + left) / (right - left);
            Result.M32 = (top + bottom) / (top - bottom);
            Result.M34 = 1;

#if GLM_DEPTH_ZERO_TO_ONE
            Result.M33 = farVal / (farVal - nearVal);
            Result.M43 = -(farVal * nearVal) / (farVal - nearVal);
#else
            Result.M33 = (farVal + nearVal) / (farVal - nearVal);
            Result.M43 = -((2) * farVal * nearVal) / (farVal - nearVal);
#endif
            return Result;
        }

        public static mat4 frustumRH(float left, float right, float bottom, float top, float nearVal, float farVal)
        {
            mat4 Result = new mat4(0);
            Result.M11 = 2 * nearVal / (right - left);
            Result.M22 = 2 * nearVal / (top - bottom);
            Result.M31 = (right + left) / (right - left);
            Result.M32 = (top + bottom) / (top - bottom);
            Result.M34 = -1;

#if GLM_DEPTH_ZERO_TO_ONE
            Result.M33 = farVal / (nearVal - farVal);
            Result.M43 = -(farVal * nearVal) / (farVal - nearVal);
#else
            Result.M33 = -(farVal + nearVal) / (farVal - nearVal);
            Result.M43 = -((2) * farVal * nearVal) / (farVal - nearVal);
#endif
            return Result;
        }


        public static mat4 perspective(float fovy, float aspect, float zNear, float zFar)
        {
#if GLM_LEFT_HANDED
            return perspectiveLH(fovy, aspect, zNear, zFar);
#else
            return perspectiveRH(fovy, aspect, zNear, zFar);
#endif
        }

        public static mat4 perspectiveRH(float fovy, float aspect, float zNear, float zFar)
        {
            float tanHalfFovy = tan(fovy / 2);

            mat4 Result = new mat4(0);
            Result.M11 = 1 / (aspect * tanHalfFovy);
            Result.M22 = 1 / tanHalfFovy;
            Result.M34 = -1;

#if GLM_DEPTH_ZERO_TO_ONE
            Result.M33 = zFar / (zNear - zFar);
            Result.M43 = -(zFar * zNear) / (zFar - zNear);
#else
            Result.M33 = -(zFar + zNear) / (zFar - zNear);
            Result.M43 = -((2) * zFar * zNear) / (zFar - zNear);
#endif
            return Result;
        }

        public static mat4 perspectiveLH(float fovy, float aspect, float zNear, float zFar)
        {
            float tanHalfFovy = tan(fovy / 2);
            mat4 Result = new mat4(0);
            Result.M11 = 1 / (aspect * tanHalfFovy);
            Result.M22 = 1 / tanHalfFovy;
            Result.M34 = 1;

#if GLM_DEPTH_ZERO_TO_ONE
            Result.M33 = zFar / (zFar - zNear);
            Result.M43 = -(zFar * zNear) / (zFar - zNear);
#else
            Result.M33 = (zFar + zNear) / (zFar - zNear);
            Result.M43 = -((2) * zFar * zNear) / (zFar - zNear);
#endif
            return Result;
        }

        public static mat4 perspectiveFov(float fov, float width, float height, float zNear, float zFar)
        {
#if GLM_LEFT_HANDED
            return perspectiveFovLH(fov, width, height, zNear, zFar);
#else
            return perspectiveFovRH(fov, width, height, zNear, zFar);
#endif
        }

        public static mat4 perspectiveFovRH(float fov, float width, float height, float zNear, float zFar)
        {
            float rad = fov;
            float h = cos(0.5f * rad) / sin(0.5f * rad);
            float w = h * height / width; ///todo max(width , Height) / min(width , Height)?

            mat4 Result = new mat4(0);
            Result.M11 = w;
            Result.M22 = h;
            Result.M34 = -1;

#if GLM_DEPTH_ZERO_TO_ONE
            Result.M33 = zFar / (zNear - zFar);
            Result.M43 = -(zFar * zNear) / (zFar - zNear);
#else
            Result.M33 = -(zFar + zNear) / (zFar - zNear);
            Result.M43 = -((2) * zFar * zNear) / (zFar - zNear);
#endif

            return Result;
        }

        public static mat4 perspectiveFovLH(float fov, float width, float height, float zNear, float zFar)
        {
            float rad = fov;
            float h = cos(0.5f * rad) / sin(0.5f * rad);
            float w = h * height / width; ///todo max(width , Height) / min(width , Height)?

            mat4 Result = new mat4(0);
            Result.M11 = w;
            Result.M22 = h;
            Result.M34 = 1;

#if GLM_DEPTH_ZERO_TO_ONE
            Result.M33 = zFar / (zFar - zNear);
            Result.M43 = -(zFar * zNear) / (zFar - zNear);
#else
            Result.M33 = (zFar + zNear) / (zFar - zNear);
            Result.M43 = -((2) * zFar * zNear) / (zFar - zNear);
#endif
            return Result;
        }

        public static mat4 infinitePerspective(float fovy, float aspect, float zNear)
        {
#if GLM_LEFT_HANDED
            return infinitePerspectiveLH(fovy, aspect, zNear);
#else
            return infinitePerspectiveRH(fovy, aspect, zNear);
#endif
        }

        public static mat4 infinitePerspectiveRH(float fovy, float aspect, float zNear)
        {
            float range = tan(fovy / 2) * zNear;
            float left = -range * aspect;
            float right = range * aspect;
            float bottom = -range;
            float top = range;

            mat4 Result = new mat4(0);
            Result.M11 = 2 * zNear / (right - left);
            Result.M22 = 2 * zNear / (top - bottom);
            Result.M33 = -1;
            Result.M34 = -1;
            Result.M43 = -2 * zNear;
            return Result;
        }

        public static mat4 infinitePerspectiveLH(float fovy, float aspect, float zNear)
        {
            float range = tan(fovy / 2) * zNear;
            float left = -range * aspect;
            float right = range * aspect;
            float bottom = -range;
            float top = range;

            mat4 Result = new mat4(0);
            Result.M11 = 2 * zNear / (right - left);
            Result.M22 = 2 * zNear / (top - bottom);
            Result.M33 = 1;
            Result.M34 = 1;
            Result.M43 = -2 * zNear;
            return Result;
        }

        // Infinite projection matrix: http://www.terathon.com/gdc07_lengyel.pdf
        public static mat4 tweakedInfinitePerspective(float fovy, float aspect, float zNear, float ep)
        {
            float range = tan(fovy / 2) * zNear;
            float left = -range * aspect;
            float right = range * aspect;
            float bottom = -range;
            float top = range;

            mat4 Result = new mat4(0);
            Result.M11 = 2 * zNear / (right - left);
            Result.M22 = 2 * zNear / (top - bottom);
            Result.M33 = ep - 1;
            Result.M34 = -1;
            Result.M43 = (ep - 2) * zNear;
            return Result;
        }

        public static mat4 tweakedInfinitePerspective(float fovy, float aspect, float zNear)
        {
            return tweakedInfinitePerspective(fovy, aspect, zNear, float.Epsilon);
        }

        public static vec3 project(in vec3 obj, in mat4 model, in mat4 proj, in vec4 viewport)
        {
            vec4 tmp = vec4(obj, 1);
            tmp = model * tmp;
            tmp = proj * tmp;

            tmp /= tmp.w;
#if GLM_DEPTH_ZERO_TO_ONE
            tmp.x = tmp.x * 0.5f + 0.5f;
            tmp.y = tmp.y * 0.5f + 0.5f;
#else
			tmp = tmp* (0.5) + (0.5);
#endif

            tmp[0] = tmp[0] * viewport[2] + viewport[0];
            tmp[1] = tmp[1] * viewport[3] + viewport[1];

            return vec3(tmp.x, tmp.y, tmp.z);
        }

        public static vec3 unProject(in vec3 win, in mat4 model, in mat4 proj, in vec4 viewport)
        {
            mat4 Inverse = inverse(proj * model);

            vec4 tmp = vec4(win, 1);
            tmp.x = (tmp.x - viewport[0]) / viewport[2];
            tmp.y = (tmp.y - viewport[1]) / viewport[3];
#if GLM_DEPTH_ZERO_TO_ONE
            tmp.x = tmp.x * 2 - 1;
            tmp.y = tmp.y * 2 - 1;
#else
        tmp = tmp * (2) - (1);
#endif

            vec4 obj = Inverse * tmp;
            obj /= obj.w;

            return vec3(obj);
        }

        public static mat4 pickMatrix(in vec2 center, in vec2 delta, in vec4 viewport)
        {
            //assert(delta.x > (0) && delta.y > (0));
            mat4 Result = mat4(1);

            if (!(delta.x > 0 && delta.y > 0))
                return Result; // Error

            var Temp = vec3(
                (viewport[2] - 2 * (center.x - viewport[0])) / delta.x,
                (viewport[3] - 2 * (center.y - viewport[1])) / delta.y,
                0);

            // Translate and scale the picked region to the entire window
            Result = translate(Result, Temp);
            return scale(Result, vec3(viewport[2] / delta.x, viewport[3] / delta.y, 1));
        }


        public static mat4 lookAt(in vec3 eye, in vec3 center, in vec3 up)
        {
#if GLM_LEFT_HANDED
            return lookAtLH(in eye, in center, in up);
#else
            return lookAtRH(in eye, in center, in up);
#endif
        }

        public static mat4 lookAtRH(in vec3 eye, in vec3 center, in vec3 up)
        {
            vec3 f = normalize(center - eye);
            vec3 s = normalize(cross(f, up));
            vec3 u = cross(s, f);

            mat4 Result = mat4(1);
            Result.M11 = s.x;
            Result.M21 = s.y;
            Result.M31 = s.z;
            Result.M12 = u.x;
            Result.M22 = u.y;
            Result.M32 = u.z;
            Result.M13 = -f.x;
            Result.M23 = -f.y;
            Result.M33 = -f.z;
            Result.M41 = -dot(s, eye);
            Result.M42 = -dot(u, eye);
            Result.M43 = dot(f, eye);
            return Result;
        }

        public static mat4 lookAtLH(in vec3 eye, in vec3 center, in vec3 up)
        {
            vec3 f = normalize(center - eye);
            vec3 s = normalize(cross(up, f));
            vec3 u = cross(f, s);

            mat4 Result = mat4(1);
            Result.M11 = s.x;
            Result.M21 = s.y;
            Result.M31 = s.z;
            Result.M12 = u.x;
            Result.M22 = u.y;
            Result.M32 = u.z;
            Result.M13 = f.x;
            Result.M23 = f.y;
            Result.M33 = f.z;
            Result.M41 = -dot(s, eye);
            Result.M42 = -dot(u, eye);
            Result.M43 = -dot(f, eye);
            return Result;
        }

        static vec3 combine(vec3 a, vec3 b, float ascl, float bscl)
        {
            return (a * ascl) + (b * bscl);
        }

        static vec3 scale(vec3 v, float desiredLength)
        {
            return v * desiredLength / length(v);
        }

        // Matrix decompose
        // http://www.opensource.apple.com/source/WebCore/WebCore-514/platform/graphics/transforms/TransformationMatrix.cpp
        // Decomposes the mode matrix to translations,rotation scale components

        public static bool decompose(in mat4 ModelMatrix, ref vec3 Scale, ref quat Orientation, ref vec3 Translation, ref vec3 Skew, ref vec4 Perspective)
        {
            mat4 LocalMatrix = ModelMatrix;

            // Normalize the matrix.
            if (epsilonEqual(LocalMatrix.M44, 0, epsilon()))
            {
                return false;
            }

            for (int i = 0; i < 4; ++i)
                for (int j = 0; j < 4; ++j)
                    LocalMatrix[i][j] /= LocalMatrix.M44;

            // perspectiveMatrix is used to solve for perspective, but it also provides
            // an easy way to test for singularity of the upper 3x3 component.
            mat4 PerspectiveMatrix = LocalMatrix;

            for (int i = 0; i < 3; i++)
                PerspectiveMatrix[i][3] = 0;

            PerspectiveMatrix.M44 = 1;

            /// TODO: Fixme!
            if (epsilonEqual(determinant(in PerspectiveMatrix), 0, epsilon()))
                return false;

            // First, isolate perspective.  This is the messiest.
            if (
                epsilonNotEqual(LocalMatrix.M14, 0, epsilon()) ||
                epsilonNotEqual(LocalMatrix.M24, 0, epsilon()) ||
                epsilonNotEqual(LocalMatrix.M34, 0, epsilon()))
            {
                // rightHandSide is the right hand side of the equation.
                vec4 RightHandSide = new vec4();
                RightHandSide.x = LocalMatrix.M14;
                RightHandSide.y = LocalMatrix.M24;
                RightHandSide.z = LocalMatrix.M34;
                RightHandSide.w = LocalMatrix.M44;

                // Solve the equation by inverting PerspectiveMatrix and multiplying
                // rightHandSide by the inverse.  (This is the easiest way, not
                // necessarily the best.)
                mat4 InversePerspectiveMatrix = inverse(PerspectiveMatrix);//   inverse(PerspectiveMatrix, inversePerspectiveMatrix);
                mat4 TransposedInversePerspectiveMatrix = transpose(InversePerspectiveMatrix);//   transposeMatrix4(inversePerspectiveMatrix, transposedInversePerspectiveMatrix);

                Perspective = TransposedInversePerspectiveMatrix * RightHandSide;
                //  v4MulPointByMatrix(rightHandSide, transposedInversePerspectiveMatrix, perspectivePoint);

                // Clear the perspective partition
                LocalMatrix.M14 = LocalMatrix.M24 = LocalMatrix.M34 = 0;
                LocalMatrix.M44 = 1;
            }
            else
            {
                // No perspective.
                Perspective = vec4(0, 0, 0, 1);
            }

            // Next take care of translation (easy).
            Translation = vec3(LocalMatrix[3]);
            LocalMatrix[3] = vec4(0, 0, 0, LocalMatrix[3].w);

            Span<vec3> Row = stackalloc vec3[3];
            vec3 Pdum3;
            {
                // Now get scale and shear.
                for (int i = 0; i < 3; ++i)
                    for (int j = 0; j < 3; ++j)
                        Row[i][j] = LocalMatrix[i][j];
            }
            // Compute X scale factor and normalize first row.
            Scale.x = length(Row[0]);// v3Length(Row[0]);

            Row[0] = scale(Row[0], 1);

            // Compute XY shear factor and make 2nd row orthogonal to 1st.
            Skew.z = dot(Row[0], Row[1]);
            Row[1] = combine(Row[1], Row[0], 1, -Skew.z);

            // Now, compute Y scale and normalize 2nd row.
            Scale.y = length(Row[1]);
            Row[1] = scale(Row[1], 1);
            Skew.z /= Scale.y;

            // Compute XZ and YZ shears, orthogonalize 3rd row.
            Skew.y = dot(Row[0], Row[2]);
            Row[2] = combine(Row[2], Row[0], 1, -Skew.y);
            Skew.x = dot(Row[1], Row[2]);
            Row[2] = combine(Row[2], Row[1], 1, -Skew.x);

            // Next, get Z scale and normalize 3rd row.
            Scale.z = length(Row[2]);
            Row[2] = scale(Row[2], 1);
            Skew.y /= Scale.z;
            Skew.x /= Scale.z;

            // At this point, the matrix (in rows[]) is orthonormal.
            // Check for a coordinate system flip.  If the determinant
            // is -1, then negate the matrix and the scaling factors.
            Pdum3 = cross(Row[1], Row[2]); // v3Cross(row[1], row[2], Pdum3);
            if (dot(Row[0], Pdum3) < 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    Scale[i] *= -1;
                    Row[i] *= -1;
                }
            }

            // Now, get the rotations out, as described in the gem.

            // FIXME - Add the ability to return either quaternions (which are
            // easier to recompose with) or Euler angles (rx, ry, rz), which
            // are easier for authors to deal with. The latter will only be useful
            // when we fix https://bugs.webkit.org/show_bug.cgi?id=23799, so I
            // will leave the Euler angle code here for now.

            // ret.rotateY = asin(-Row.M13);
            // if (cos(ret.rotateY) != 0) {
            //     ret.rotateX = atan2(Row.M23, Row.M33);
            //     ret.rotateZ = atan2(Row.M12, Row.M11);
            // } else {
            //     ret.rotateX = atan2(-Row.M31, Row.M22);
            //     ret.rotateZ = 0;
            // }

            {
                int i, j, k = 0;
                float root, trace = Row[0].x + Row[1].y + Row[2].z;
                if (trace > 0)
                {
                    root = sqrt(trace + 1.0f);
                    Orientation.w = 0.5f * root;
                    root = 0.5f / root;
                    Orientation.x = root * (Row[1].z - Row[2].y);
                    Orientation.y = root * (Row[2].x - Row[0].z);
                    Orientation.z = root * (Row[0].y - Row[1].x);
                } // End if > 0
                else
                {
                    Span<int> Next = stackalloc[] { 1, 2, 0 };
                    i = 0;
                    if (Row[1].y > Row[0].x) i = 1;
                    if (Row[2].z > Row[i][i]) i = 2;
                    j = Next[i];
                    k = Next[j];

                    root = sqrt(Row[i][i] - Row[j][j] - Row[k][k] + 1.0f);

                    Orientation[i] = 0.5f * root;
                    root = 0.5f / root;
                    Orientation[j] = root * (Row[i][j] + Row[j][i]);
                    Orientation[k] = root * (Row[i][k] + Row[k][i]);
                    Orientation.w = root * (Row[j][k] - Row[k][j]);
                } // End if <= 0
            }

            return true;
        }
    }

}
