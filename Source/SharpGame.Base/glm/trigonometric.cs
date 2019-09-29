using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public static partial class glm
    {
        public static float degrees(float radians)
        {
            return radians * (57.295779513082320876798154814105f);
        }

        public static vec2 degrees(in vec2 radians)
        {
            return new vec2(degrees(radians.x), degrees(radians.y));
        }

        public static vec3 degrees(in vec3 radians)
        {
            return new vec3(degrees(radians.x), degrees(radians.y), degrees(radians.z));
        }

        public static vec3 degrees(float x, float y, float z)
        {
            return new vec3(degrees(x), degrees(y), degrees(z));
        }

        public static float radians(float degrees)
        {
            return degrees * (0.01745329251994329576923690768489f);
        }

        public static vec2 radians(in vec2 degrees)
        {
            return new vec2(radians(degrees.x), radians(degrees.y));
        }

        public static vec3 radians(in vec3 degrees)
        {
            return new vec3(radians(degrees.x), radians(degrees.y), radians(degrees.z));
        }

        public static vec3 radians(float x, float y, float z)
        {
            return new vec3(radians(x), radians(y), radians(z));
        }

        public static float sin(float angle)
        {
            return (float)Math.Sin(angle);
        }

        public static vec2 sin(in vec2 angle)
        {
            return new vec2(sin(angle.x), sin(angle.y));
        }

        public static vec3 sin(in vec3 angle)
        {
            return new vec3(sin(angle.x), sin(angle.y), sin(angle.z));
        }

        public static float sinh(float angle)
        {
            return (float)Math.Sinh(angle);
        }

        public static float asin(float x)
        {
            return (float)Math.Asin(x);
        }

        public static float asinh(float x)
        {
            return (float)(x < 0f ? -1f : (x > 0f ? 1f : 0f)) * (float)Math.Log(Math.Abs(x) + Math.Sqrt(1f + x * x));
        }

        public static float cos(float angle)
        {
            return (float)Math.Cos(angle);
        }

        public static vec2 cos(in vec2 angle)
        {
            return new vec2((float)Math.Cos(angle.x), (float)Math.Cos(angle.y));
        }

        public static vec3 cos(in vec3 angle)
        {
            return new vec3((float)Math.Cos(angle.x), (float)Math.Cos(angle.y), (float)Math.Cos(angle.z));
        }

        public static float cosh(float angle)
        {
            return (float)Math.Cosh(angle);
        }

        public static float acos(float x)
        {
            return (float)Math.Acos(x);
        }

        public static float acosh(float x)
        {

            if (x < (1f))
                return (0f);
            return (float)Math.Log(x + Math.Sqrt(x * x - (1f)));
        }

        public static float tan(float angle)
        {
            return (float)Math.Tan(angle);
        }

        public static vec2 tan(in vec2 angle)
        {
            return new vec2(tan(angle.x), tan(angle.y));
        }

        public static vec3 tan(in vec3 angle)
        {
            return new vec3(tan(angle.x), tan(angle.y), tan(angle.z));
        }

        public static float tanh(float angle)
        {
            return (float)Math.Tanh(angle);
        }

        public static float atan(float y, float x)
        {
            return (float)Math.Atan2(y, x);
        }

        public static float atan(float y_over_x)
        {
            return (float)Math.Atan(y_over_x);
        }

        public static float atanh(float x)
        {
            if (Math.Abs(x) >= 1f)
                return 0;
            return (0.5f) * (float)Math.Log((1f + x) / (1f - x));
        }

    }
}
