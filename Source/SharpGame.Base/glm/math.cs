using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public static partial class glm
    {
        /// <summary>
        /// The value for which all absolute numbers smaller than are considered equal to zero.
        /// </summary>
        public const float epsilon = 1e-6f; // Value a 8x higher than 1.19209290E-07F

        /// <summary>
        /// A value specifying the approximation of π which is 180 degrees.
        /// </summary>
        public const float pi = (float)Math.PI;

        /// <summary>
        /// A value specifying the approximation of 2π which is 360 degrees.
        /// </summary>
        public const float twoPi = (float)(2 * Math.PI);

        /// <summary>
        /// A value specifying the approximation of π/2 which is 90 degrees.
        /// </summary>
        public const float half_pi = (float)(Math.PI / 2);

        const float FLT_EPSILON = 1.192092896e-07F;

        static readonly Random rand_ = new Random();

        public static Random rand() => rand_;

        /// Return a random float between 0.0 (inclusive) and 1.0 (exclusive.)
        public static float random() { return (float)rand_.NextDouble(); }

        /// Return a random float between 0.0 and range, inclusive from both ends.
        public static float random(float range) { return rand_.NextFloat(0, range); }

        /// Return a random float between min and max, inclusive from both ends.
        public static float random(float min, float max) { return rand_.NextFloat(min, max); }

        /// Return a random integer between 0 and range - 1.
        public static int random(int range) { return (int)rand_.NextLong(0, range); }

        /// Return a random integer between min and max - 1.
        public static int random(int min, int max) { return (int)rand_.NextLong(min, max); }

        public static float abs(float v)
        {
            return Math.Abs(v);
        }

        public static bool epsilonEqual(float x, float y, float epsilon)
        {
            return abs(x - y) < epsilon;
        }

        public static bool epsilonNotEqual(float x, float y, float epsilon)
        {
            return abs(x - y) >= epsilon;
        }

        public static float sqrt(float v)
        {
            return (float)Math.Sqrt(v);
        }

        public static float invSqrt(float v) => 1 / glm.sqrt(v);

        public static int max(int left, int right)
        {
            return left > right ? left : right;
        }

        public static int min(int left, int right)
        {
            return left < right ? left : right;
        }

        public static float max(float left, float right)
        {
            return left > right ? left : right;
        }

        public static float min(float left, float right)
        {
            return left < right ? left : right;
        }

        public static float clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static float clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static double lerp(double from, double to, double amount)
        {
            return (1 - amount) * from + amount * to;
        }

        public static float lerp(float from, float to, float amount)
        {
            return (1 - amount) * from + amount * to;
        }

        public static byte lerp(byte from, byte to, float amount)
        {
            return (byte)lerp((float)from, (float)to, amount);
        }

        public static float mix(float x, float y, float a)
		{
			return ((x) + a* (y - x));
		}

        public static void hash_combine(ref uint seed, ref uint hash)
        {
            hash += 0x9e3779b9 + (seed << 6) + (seed >> 2);
            seed ^= hash;
        }
    }
}
