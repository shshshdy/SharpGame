// Copyright (c) 2018-2022 SharpGame
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace SharpGame
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Uint2 : IEquatable<Uint2>
    {
        public uint x;

        public uint y;

        public static readonly Uint2 Zero = new Uint2(0, 0);

        public Uint2(uint x, uint y)
        {
            this.x = x;
            this.y = y;
        }

        public Uint2(uint xy) : this(xy, xy)
        {
            this.x = xy;
            this.y = xy;
        }

        public uint X => x;

        public uint Y => y;

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(ref Uint2 other)
        {
            return other.X == X && other.Y == Y;
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(Uint2 other)
        {
            return Equals(ref other);
        }

        public override bool Equals(object obj)
        {
            if(!(obj is Uint2))
                return false;

            var strongValue = (Uint2)obj;
            return Equals(ref strongValue);
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(Uint2 left, Uint2 right)
        {
            return left.Equals(ref right);
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator !=(Uint2 left, Uint2 right)
        {
            return !left.Equals(ref right);
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", X, Y);
        }

        public override int GetHashCode()
        {
            int hashCode = 1502939027;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            return hashCode;
        }

        public static explicit operator Uint2(vec2 value)
        {
            return new Uint2((uint)value.X, (uint)value.Y);
        }

        public static implicit operator vec2(Uint2 value)
        {
            return new vec2(value.X, value.Y);
        }

        public static Uint2 operator +(in Uint2 lhs, in Uint2 rhs)
        {
            return new Uint2(lhs.x + rhs.x, lhs.y + rhs.y);
        }

        public static Uint2 operator +(in Uint2 lhs, uint rhs)
        {
            return new Uint2(lhs.x + rhs, lhs.y + rhs);
        }

        public static Uint2 operator -(in Uint2 lhs, in Uint2 rhs)
        {
            return new Uint2(lhs.x - rhs.x, lhs.y - rhs.y);
        }

        public static Uint2 operator -(in Uint2 lhs, uint rhs)
        {
            return new Uint2(lhs.x - rhs, lhs.y - rhs);
        }

        public static Uint2 operator *(in Uint2 self, uint s)
        {
            return new Uint2(self.x * s, self.y * s);
        }

        public static Uint2 operator *(uint lhs, in Uint2 rhs)
        {
            return new Uint2(rhs.x * lhs, rhs.y * lhs);
        }

        public static Uint2 operator *(in Uint2 lhs, in Uint2 rhs)
        {
            return new Uint2(rhs.x * lhs.x, rhs.y * lhs.y);
        }

        public static Uint2 operator /(in Uint2 lhs, uint rhs)
        {
            return new Uint2(lhs.x / rhs, lhs.y / rhs);
        }
    }
}
