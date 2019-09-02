﻿#define GLM_LEFT_HANDED
#define GLM_DEPTH_ZERO_TO_ONE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Glm
{
    public static partial class glm
    {
        public static mat4 rotate(mat4 m, float angle, vec3 v)
        {
            float c = cos(angle);
            float s = sin(angle);

            vec3 axis = normalize(v);
            vec3 temp = (1.0f - c) * axis;

            mat4 rotate = identity();
            rotate[0, 0] = c + temp[0] * axis[0];
            rotate[0, 1] = 0 + temp[0] * axis[1] + s * axis[2];
            rotate[0, 2] = 0 + temp[0] * axis[2] - s * axis[1];

            rotate[1, 0] = 0 + temp[1] * axis[0] - s * axis[2];
            rotate[1, 1] = c + temp[1] * axis[1];
            rotate[1, 2] = 0 + temp[1] * axis[2] + s * axis[0];

            rotate[2, 0] = 0 + temp[2] * axis[0] + s * axis[1];
            rotate[2, 1] = 0 + temp[2] * axis[1] - s * axis[0];
            rotate[2, 2] = c + temp[2] * axis[2];

            mat4 result = identity();
            result[0] = m[0] * rotate[0][0] + m[1] * rotate[0][1] + m[2] * rotate[0][2];
            result[1] = m[0] * rotate[1][0] + m[1] * rotate[1][1] + m[2] * rotate[1][2];
            result[2] = m[0] * rotate[2][0] + m[1] * rotate[2][1] + m[2] * rotate[2][2];
            result[3] = m[3];
            return result;
        }

        public static mat4 rotate(float angle, vec3 v)
        {
            return rotate(identity(), angle, v);
        }

        public static mat4 scale(mat4 m, vec3 v)
        {
            mat4 result = m;
            result[0] = m[0] * v[0];
            result[1] = m[1] * v[1];
            result[2] = m[2] * v[2];
            result[3] = m[3];
            return result;
        }

        public static mat4 scale(vec3 v)
        {
            return scale(identity(), v);
        }

        public static mat4 translate(mat4 m, vec3 v)
        {
            mat4 result = m;
            result[3] = m[0] * v[0] + m[1] * v[1] + m[2] * v[2] + m[3];
            return result;
        }

        public static mat4 translate(vec3 v)
        {
            return translate(identity(), v);
        }

        public static mat4 translate(float x, float y, float z)
        {
            return translate(identity(), new vec3(x, y, z));
        }

        public static mat4 ortho(float left, float right, float bottom, float top, float zNear, float zFar)
        {
#if GLM_LEFT_HANDED
            return orthoLH(left, right, bottom, top, zNear, zFar);
#else
			return orthoRH(left, right, bottom, top, zNear, zFar);
#endif
        }

        public static mat4 orthoLH
        (
            float left, float right,
            float bottom, float top,
            float zNear, float zFar
        )
        {
            mat4 Result = new mat4(1);
            Result[0][0] = 2 / (right - left);
            Result[1][1] = 2 / (top - bottom);
            Result[3][0] = -(right + left) / (right - left);
            Result[3][1] = -(top + bottom) / (top - bottom);

#if GLM_DEPTH_ZERO_TO_ONE
            Result[2][2] = (1) / (zFar - zNear);
            Result[3][2] = -zNear / (zFar - zNear);
#else
			Result[2][2] = 2 / (zFar - zNear);
			Result[3][2] = - (zFar + zNear) / (zFar - zNear);
#endif
            return Result;
        }

        public static mat4 orthoRH(float left, float right, float bottom, float top, float zNear, float zFar)
        {
            mat4 Result = new mat4(1);
            Result[0][0] = (2) / (right - left);
            Result[1][1] = (2) / (top - bottom);
            Result[3][0] = -(right + left) / (right - left);
            Result[3][1] = -(top + bottom) / (top - bottom);

#if GLM_DEPTH_ZERO_TO_ONE
            Result[2][2] = -(1) / (zFar - zNear);
            Result[3][2] = -zNear / (zFar - zNear);
#else
			Result[2][2] = - (2) / (zFar - zNear);
			Result[3][2] = - (zFar + zNear) / (zFar - zNear);
#endif
            return Result;
        }

        public static mat4 ortho(float left, float right, float bottom, float top)
        {
            var result = identity();
            result[0, 0] = (2f) / (right - left);
            result[1, 1] = (2f) / (top - bottom);
            result[2, 2] = -(1f);
            result[3, 0] = -(right + left) / (right - left);
            result[3, 1] = -(top + bottom) / (top - bottom);
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
            Result[0][0] = ((2) * nearVal) / (right - left);
            Result[1][1] = ((2) * nearVal) / (top - bottom);
            Result[2][0] = (right + left) / (right - left);
            Result[2][1] = (top + bottom) / (top - bottom);
            Result[2][3] = (1);

#if GLM_DEPTH_ZERO_TO_ONE
            Result[2][2] = farVal / (farVal - nearVal);
            Result[3][2] = -(farVal * nearVal) / (farVal - nearVal);
#else
            Result[2][2] = (farVal + nearVal) / (farVal - nearVal);
            Result[3][2] = -((2) * farVal * nearVal) / (farVal - nearVal);
#endif
            return Result;
        }

        public static mat4 frustumRH(float left, float right, float bottom, float top, float nearVal, float farVal)
        {
            mat4 Result = new mat4(0);
            Result[0][0] = ((2) * nearVal) / (right - left);
            Result[1][1] = ((2) * nearVal) / (top - bottom);
            Result[2][0] = (right + left) / (right - left);
            Result[2][1] = (top + bottom) / (top - bottom);
            Result[2][3] = (-1);

#if GLM_DEPTH_ZERO_TO_ONE
            Result[2][2] = farVal / (nearVal - farVal);
            Result[3][2] = -(farVal * nearVal) / (farVal - nearVal);
#else
            Result[2][2] = -(farVal + nearVal) / (farVal - nearVal);
            Result[3][2] = -((2) * farVal * nearVal) / (farVal - nearVal);
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
            float tanHalfFovy = tan(fovy / (2));

            mat4 Result = new mat4(0);
            Result[0][0] = (1) / (aspect * tanHalfFovy);
            Result[1][1] = (1) / (tanHalfFovy);
            Result[2][3] = -(1);

#if GLM_DEPTH_ZERO_TO_ONE
            Result[2][2] = zFar / (zNear - zFar);
            Result[3][2] = -(zFar * zNear) / (zFar - zNear);
#else
            Result[2][2] = -(zFar + zNear) / (zFar - zNear);
            Result[3][2] = -((2) * zFar * zNear) / (zFar - zNear);
#endif

            return Result;
        }


        public static mat4 perspectiveLH(float fovy, float aspect, float zNear, float zFar)
        {
            float tanHalfFovy = tan(fovy / (2));

            mat4 Result = new mat4(0);
            Result[0][0] = (1) / (aspect * tanHalfFovy);
            Result[1][1] = (1) / (tanHalfFovy);
            Result[2][3] = (1);

#if GLM_DEPTH_ZERO_TO_ONE
            Result[2][2] = zFar / (zFar - zNear);
            Result[3][2] = -(zFar * zNear) / (zFar - zNear);
#else
            Result[2][2] = (zFar + zNear) / (zFar - zNear);
            Result[3][2] = -((2) * zFar * zNear) / (zFar - zNear);
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
            float h = cos((0.5f) * rad) / sin((0.5f) * rad);
            float w = h * height / width; ///todo max(width , Height) / min(width , Height)?

            mat4 Result = new mat4((0));
            Result[0][0] = w;
            Result[1][1] = h;
            Result[2][3] = -(1);

#if GLM_DEPTH_ZERO_TO_ONE
            Result[2][2] = zFar / (zNear - zFar);
            Result[3][2] = -(zFar * zNear) / (zFar - zNear);
#else
            Result[2][2] = -(zFar + zNear) / (zFar - zNear);
            Result[3][2] = -((2) * zFar * zNear) / (zFar - zNear);
#endif

            return Result;
        }


        public static mat4 perspectiveFovLH(float fov, float width, float height, float zNear, float zFar)
        {
            float rad = fov;
            float h = cos((0.5f) * rad) / sin((0.5f) * rad);
            float w = h * height / width; ///todo max(width , Height) / min(width , Height)?

            mat4 Result = new mat4(0);
            Result[0][0] = w;
            Result[1][1] = h;
            Result[2][3] = (1);

#if GLM_DEPTH_ZERO_TO_ONE
            Result[2][2] = zFar / (zFar - zNear);
            Result[3][2] = -(zFar * zNear) / (zFar - zNear);
#else
            Result[2][2] = (zFar + zNear) / (zFar - zNear);
            Result[3][2] = -((2) * zFar * zNear) / (zFar - zNear);
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
            float range = tan(fovy / (2)) * zNear;
            float left = -range * aspect;
            float right = range * aspect;
            float bottom = -range;
            float top = range;

            mat4 Result = new mat4(0);
            Result[0][0] = ((2) * zNear) / (right - left);
            Result[1][1] = ((2) * zNear) / (top - bottom);
            Result[2][2] = -(1);
            Result[2][3] = -(1);
            Result[3][2] = -(2) * zNear;
            return Result;
        }


        public static mat4 infinitePerspectiveLH(float fovy, float aspect, float zNear)
        {
            float range = tan(fovy / (2)) * zNear;
            float left = -range * aspect;
            float right = range * aspect;
            float bottom = -range;
            float top = range;

            mat4 Result = new mat4(0);
            Result[0][0] = ((2) * zNear) / (right - left);
            Result[1][1] = ((2) * zNear) / (top - bottom);
            Result[2][2] = (1);
            Result[2][3] = (1);
            Result[3][2] = -(2) * zNear;
            return Result;
        }

        // Infinite projection matrix: http://www.terathon.com/gdc07_lengyel.pdf

        public static mat4 tweakedInfinitePerspective(float fovy, float aspect, float zNear, float ep)
        {
            float range = tan(fovy / (2)) * zNear;
            float left = -range * aspect;
            float right = range * aspect;
            float bottom = -range;
            float top = range;

            mat4 Result = new mat4((0));
            Result[0][0] = ((2) * zNear) / (right - left);
            Result[1][1] = ((2) * zNear) / (top - bottom);
            Result[2][2] = ep - (1);
            Result[2][3] = (-1);
            Result[3][2] = (ep - (2)) * zNear;
            return Result;
        }


        public static mat4 tweakedInfinitePerspective(float fovy, float aspect, float zNear)
        {
            return tweakedInfinitePerspective(fovy, aspect, zNear, float.Epsilon);
        }

        public static vec3 project(ref vec3 obj, ref mat4 model, ref mat4 proj, ref vec4 viewport)
        {
            vec4 tmp = vec4(obj, (1));
            tmp = model * tmp;
            tmp = proj * tmp;

            tmp /= tmp.w;
#if GLM_DEPTH_ZERO_TO_ONE
            tmp.x = tmp.x * (0.5f) + (0.5f);
            tmp.y = tmp.y * (0.5f) + (0.5f);
#else
			tmp = tmp* (0.5) + (0.5);
#endif

            tmp[0] = tmp[0] * viewport[2] + viewport[0];
            tmp[1] = tmp[1] * viewport[3] + viewport[1];

            return vec3(tmp.x, tmp.y, tmp.z);
        }

        public static vec3 unProject(ref vec3 win, ref mat4 model, ref mat4 proj, ref vec4 viewport)
        {
            mat4 Inverse = inverse(proj * model);

            vec4 tmp = vec4(win, 1);
            tmp.x = (tmp.x - viewport[0]) / viewport[2];
            tmp.y = (tmp.y - viewport[1]) / viewport[3];
#if GLM_DEPTH_ZERO_TO_ONE
            tmp.x = tmp.x * (2) - (1);
            tmp.y = tmp.y * (2) - (1);
#else
        tmp = tmp * (2) - (1);
#endif

            vec4 obj = Inverse * tmp;
            obj /= obj.w;

            return vec3(obj);
        }

        public static mat4 pickMatrix(ref vec2 center, ref vec2 delta, ref vec4 viewport)
        {
            //assert(delta.x > (0) && delta.y > (0));
            mat4 Result = mat4(1);

            if (!(delta.x > (0) && delta.y > (0)))
                return Result; // Error

            var Temp = vec3(
                ((viewport[2]) - (2) * (center.x - (viewport[0]))) / delta.x,
                ((viewport[3]) - (2) * (center.y - (viewport[1]))) / delta.y,
                (0));

            // Translate and scale the picked region to the entire window
            Result = translate(Result, Temp);
            return scale(Result, vec3((viewport[2]) / delta.x, (viewport[3]) / delta.y, (1)));
        }


        public static mat4 lookAt(vec3 eye, vec3 center, vec3 up)
        {
#if GLM_LEFT_HANDED
            return lookAtLH(eye, center, up);
#else
        return lookAtRH(eye, center, up);
#endif
        }

        public static mat4 lookAtRH(vec3 eye, vec3 center, vec3 up)
        {
            vec3 f = normalize(center - eye);
            vec3 s = normalize(cross(f, up));
            vec3 u = cross(s, f);

            mat4 Result = mat4(1);
            Result[0][0] = s.x;
            Result[1][0] = s.y;
            Result[2][0] = s.z;
            Result[0][1] = u.x;
            Result[1][1] = u.y;
            Result[2][1] = u.z;
            Result[0][2] = -f.x;
            Result[1][2] = -f.y;
            Result[2][2] = -f.z;
            Result[3][0] = -dot(s, eye);
            Result[3][1] = -dot(u, eye);
            Result[3][2] = dot(f, eye);
            return Result;
        }

        public static mat4 lookAtLH(vec3 eye, vec3 center, vec3 up)
        {
            vec3 f = (normalize(center - eye));
            vec3 s = (normalize(cross(up, f)));
            vec3 u = (cross(f, s));

            mat4 Result = mat4(1);
            Result[0][0] = s.x;
            Result[1][0] = s.y;
            Result[2][0] = s.z;
            Result[0][1] = u.x;
            Result[1][1] = u.y;
            Result[2][1] = u.z;
            Result[0][2] = f.x;
            Result[1][2] = f.y;
            Result[2][2] = f.z;
            Result[3][0] = -dot(s, eye);
            Result[3][1] = -dot(u, eye);
            Result[3][2] = -dot(f, eye);
            return Result;
        }
    }

}
