using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public static partial class glm
    {

        public static float pi()
        {
            return (float)(3.14159265358979323846264338327950288);
        }

        public static float two_pi()
        {
            return (float)(6.28318530717958647692528676655900576);
        }

        public static float root_pi()
        {
            return (float)(1.772453850905516027);
        }

        public static float half_pi()
        {
            return (float)(1.57079632679489661923132169163975144);
        }

        public static float abs(float v)
        {
            return Math.Abs(v);
        }

        public static float sqrt(float v)
        {
            return (float)Math.Sqrt(v);
        }

        public static float clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static float clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static float mix(float x, float y, float a)
		{
			return ((x) + a* (y - x));
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

        public static float asin(float x)
        {
            return (float)Math.Asin(x);
        }

        public static float asinh(float x)
        {
            return (float)(x < 0f ? -1f : (x > 0f ? 1f : 0f)) * (float)Math.Log(Math.Abs(x) + Math.Sqrt(1f + x * x));
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

        public static float cos(float angle)
        {
            return (float)Math.Cos(angle);
        }

        public static vec2 cos(vec2 angle)
        {
            return new vec2((float)Math.Cos(angle.x), (float)Math.Cos(angle.y));
        }

        public static vec3 cos(vec3 angle)
        {
            return new vec3((float)Math.Cos(angle.x), (float)Math.Cos(angle.y), (float)Math.Cos(angle.z));
        }

        public static float cosh(float angle)
        {
            return (float)Math.Cosh(angle);
        }

        public static float degrees(float radians)
        {
            return radians * (57.295779513082320876798154814105f);
        }

        public static float radians(float degrees)
        {
            return degrees * (0.01745329251994329576923690768489f);
        }

        public static vec2 radians(vec2 degrees)
        {
            return new vec2(radians(degrees.x), radians(degrees.y));
        }

        public static vec3 radians(vec3 degrees)
        {
            return new vec3(radians(degrees.x), radians(degrees.y), radians(degrees.z));
        }

        public static float sin(float angle)
        {
            return (float)Math.Sin(angle);
        }

        public static vec2 sin(vec2 angle)
        {
            return new vec2(sin(angle.x), sin(angle.y));
        }

        public static vec3 sin(vec3 angle)
        {
            return new vec3(sin(angle.x), sin(angle.y), sin(angle.z));
        }

        public static float sinh(float angle)
        {
            return (float)Math.Sinh(angle);
        }

        public static float tan(float angle)
        {
            return (float)Math.Tan(angle);
        }

        public static vec2 tan(vec2 angle)
        {
            return new vec2(tan(angle.x), tan(angle.y));
        }

        public static vec3 tan(vec3 angle)
        {
            return new vec3(tan(angle.x), tan(angle.y), tan(angle.z));
        }

        public static float tanh(float angle)
        {
            return (float)Math.Tanh(angle);
        }
    }
}
