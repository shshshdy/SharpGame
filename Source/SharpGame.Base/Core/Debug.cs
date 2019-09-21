using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGame
{
    public static class Debug
    {
        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            System.Diagnostics.Debug.Assert(condition, message);
        }

        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            System.Diagnostics.Debug.Assert(condition);
        }
    }
}
