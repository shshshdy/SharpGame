using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public static partial class glm
    {
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
    }
}
