using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public partial struct vec2
    {
        public static explicit operator System.Numerics.Vector2(vec2 value)
        {
            return new System.Numerics.Vector2(value.x, value.y);
        }

        public static explicit operator vec2(System.Numerics.Vector2 value)
        {
            return new vec2(value.X, value.Y);
        }

        public static implicit operator Vector2(vec2 value)
        {
            return new Vector2(value.x, value.y);
        }

        public static implicit operator vec2(Vector2 value)
        {
            return new vec2(value.X, value.Y);
        }
    }

    public partial struct vec3
    {
        public static readonly vec3 Up = new vec3(0, 1, 0);
        public static readonly vec3 Right = new vec3(1, 0, 0);
        public static readonly vec3 ForwardLH = new vec3(0, 0, 1);

        public static explicit operator System.Numerics.Vector3(vec3 value)
        {
            return new System.Numerics.Vector3(value.X, value.Y, value.Z);
        }

        public static explicit operator vec3(System.Numerics.Vector3 value)
        {
            return new vec3(value.X, value.Y, value.Z);
        }

        public static explicit operator Vector3(vec3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        public static explicit operator vec3(Vector3 value)
        {
            return new vec3(value.X, value.Y, value.Z);
        }
    }

    public partial struct vec4
    {
        public static explicit operator System.Numerics.Vector4(vec4 value)
        {
            return new System.Numerics.Vector4(value.x, value.y, value.z, value.w);
        }

        public static explicit operator vec4(System.Numerics.Vector4 value)
        {
            return new vec4(value.X, value.Y, value.Z, value.W);
        }

        public static explicit operator Vector4(vec4 value)
        {
            return new Vector4(value.x, value.y, value.z, value.w);
        }

        public static explicit operator vec4(Vector4 value)
        {
            return new vec4(value.X, value.Y, value.Z, value.W);
        }
    }

    public partial struct mat4
    {
        public static readonly mat4 Identity = new mat4(1);
    }

}
