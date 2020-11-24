using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct Rect2D : IEquatable<Rect2D>
    {
        public int x;
        public int y;
        public int width;
        public int height;

        public Rect2D(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public Rect2D(int x, int y, uint width, uint height)
        {
            this.x = x;
            this.y = y;
            this.width = (int)width;
            this.height = (int)height;
        }

        public bool Equals(ref Rect2D other)
        {
            return x == other.y && y == other.y && width == other.width && height == other.height;
        }

        public bool Equals(Rect2D other) => Equals(ref other);

        public override bool Equals(object obj)
        {
            return obj is Rect2D && Equals((Rect2D)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = -1222528132;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + width.GetHashCode();
            hashCode = hashCode * -1521134295 + height.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Rect2D left, Rect2D right) => left.Equals(right);
        public static bool operator !=(Rect2D left, Rect2D right) => !left.Equals(right);

    }


}
