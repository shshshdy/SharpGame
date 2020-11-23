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

    public struct Viewport
    {
        public float x;
        public float y;
        public float width;
        public float height;
        public float minDepth;
        public float maxDepth;

        public Viewport(float x, float y, float width, float height, float minDepth = 0, float maxDepth = 1.0f)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.minDepth = minDepth;
            this.maxDepth = maxDepth;
        }

        public void Define(float x, float y, float width, float height, float minDepth = 0, float maxDepth = 1.0f)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.minDepth = minDepth;
            this.maxDepth = maxDepth;
        }

    }

    public struct Extent3D
    {
        public uint width;
        public uint height;
        public uint depth;

        public Extent3D(uint w, uint h, uint depth)
        {
            this.width = w;
            this.height = h;
            this.depth = depth;
        }

        public Extent3D(in Vulkan.VkExtent3D vkExtent3D)
        {
            this.width = vkExtent3D.width;
            this.height = vkExtent3D.height;
            this.depth = vkExtent3D.depth;
        }

        public override bool Equals(object obj)
        {
            return obj is Extent3D d &&
                   width == d.width &&
                   height == d.height &&
                   depth == d.depth;
        }

        public override int GetHashCode()
        {
            int hashCode = 1868473867;
            hashCode = hashCode * -1521134295 + width.GetHashCode();
            hashCode = hashCode * -1521134295 + height.GetHashCode();
            hashCode = hashCode * -1521134295 + depth.GetHashCode();
            return hashCode;
        }

        public static bool operator==(in Extent3D left, in Extent3D right)
        {
            return left.width == right.width && left.height == right.height && left.depth == right.depth;
        }

        public static bool operator !=(in Extent3D left, in Extent3D right)
        {
            return !(left == right);
        }
    }

    public struct Offset3D
    {
        public int x;
        public int y;
        public int z;

        public Offset3D(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Offset3D(in Vulkan.VkOffset3D vkOffset3D)
        {
            this.x = vkOffset3D.x;
            this.y = vkOffset3D.y;
            this.z = vkOffset3D.z;
        }

        public override bool Equals(object obj)
        {
            return obj is Offset3D d &&
                   x == d.x &&
                   y == d.y &&
                   z == d.z;
        }

        public override int GetHashCode()
        {
            int hashCode = 373119288;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(in Offset3D left, in Offset3D right)
        {
            return left.x == right.x && left.y == right.y && left.z == right.z;
        }

        public static bool operator !=(in Offset3D left, in Offset3D right)
        {
            return !(left == right);
        }
    }
}
