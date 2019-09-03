using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public static class MathHelper
    {
        public static vec2 Max(this RectangleF f)
        {
            return new vec2(f.Left + f.Width, f.Top + f.Height);
        }

        public static vec2 Min(this RectangleF f)
        {
            return new vec2(f.Left, f.Top);
        }

        public static vec2 Dimensions(this RectangleF f)
        {
            return new vec2(f.Width, f.Height);
        }

        public static float SquaredLength(this vec2 v)
        {
            return (v.X * v.X) + (v.Y * v.Y);
        }

        public static float SquaredDistance(this RectangleF f, vec2 v)
        {
            var max = f.Max();

            var dx1 = v.X - max.X;
            if (dx1 < 0) dx1 = 0f;
            var dx = f.Left - v.X;
            if (dx1 > dx) dx = dx1;

            var dy1 = v.Y - max.Y;
            if (dy1 < 0) dy1 = 0f;
            var dy = f.Top - v.Y;
            if (dy1 > dy) dy = dy1;

            return (dx * dx) + (dy * dy);
        }
    }
}
