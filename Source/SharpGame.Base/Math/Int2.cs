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
    public struct Int2 : IEquatable<Int2>
    {
        public int x;

        public int y;

        public static readonly Int2 Zero = new Int2(0, 0);

        public Int2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public Int2(int xy) : this(xy, xy)
        {
            this.x = xy;
            this.y = xy;
        }

        public int X => x;

        public int Y => y;

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(ref Int2 other)
        {
            return other.X == X && other.Y == Y;
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public bool Equals(Int2 other)
        {
            return Equals(ref other);
        }

        public override bool Equals(object obj)
        {
            if(!(obj is Int2))
                return false;

            var strongValue = (Int2)obj;
            return Equals(ref strongValue);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator ==(Int2 left, Int2 right)
        {
            return left.Equals(ref right);
        }

        [MethodImpl((MethodImplOptions)0x100)] // MethodImplOptions.AggressiveInlining
        public static bool operator !=(Int2 left, Int2 right)
        {
            return !left.Equals(ref right);
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", X, Y);
        }

        public static explicit operator Int2(vec2 value)
        {
            return new Int2((int)value.X, (int)value.Y);
        }

        public static implicit operator vec2(Int2 value)
        {
            return new vec2(value.X, value.Y);
        }

        public static Int2 operator +(in Int2 lhs, in Int2 rhs)
        {
            return new Int2(lhs.x + rhs.x, lhs.y + rhs.y);
        }

        public static Int2 operator +(in Int2 lhs, int rhs)
        {
            return new Int2(lhs.x + rhs, lhs.y + rhs);
        }

        public static Int2 operator -(in Int2 lhs, in Int2 rhs)
        {
            return new Int2(lhs.x - rhs.x, lhs.y - rhs.y);
        }

        public static Int2 operator -(in Int2 lhs, int rhs)
        {
            return new Int2(lhs.x - rhs, lhs.y - rhs);
        }

        public static Int2 operator *(in Int2 self, int s)
        {
            return new Int2(self.x * s, self.y * s);
        }

        public static Int2 operator *(int lhs, in Int2 rhs)
        {
            return new Int2(rhs.x * lhs, rhs.y * lhs);
        }

        public static Int2 operator *(in Int2 lhs, in Int2 rhs)
        {
            return new Int2(rhs.x * lhs.x, rhs.y * lhs.y);
        }

        public static Int2 operator /(in Int2 lhs, int rhs)
        {
            return new Int2(lhs.x / rhs, lhs.y / rhs);
        }

    }
}
