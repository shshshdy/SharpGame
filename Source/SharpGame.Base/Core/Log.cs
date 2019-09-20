//#define RENDER_LOG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGame
{
    public class Log
    {
        public static void Info(string msg)
        {
            Console.WriteLine(msg);
        }

        public static void Info(string msg, params object[] arg)
        {
            Console.WriteLine(msg, arg);
        }

        public static void Debug(string msg)
        {
            Console.WriteLine(msg);
        }

        public static void Debug(string msg, params object[] arg)
        {
            Console.WriteLine(msg, arg);
        }

        public static void Warn(string msg)
        {
            Console.WriteLine(msg);
        }

        public static void Warn(string msg, params object[] arg)
        {
            Console.WriteLine(msg, arg);
        }

        public static void Error(string msg)
        {
            Console.WriteLine(msg);
        }

        public static void Error(string msg, params object[] arg)
        {
            Console.WriteLine(msg, arg);
        }

        public static void Exception(Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }

        [Conditional("RENDER_LOG")]
        public static void Render(string msg, params object[] arg)
        {
            Console.WriteLine(msg, arg);
        }

        public static void Error(int line, string message)
        {
            Log.Error("[line " + line + "]: " +  message);
        }

        public static void Assert(string message)
        {
            System.Diagnostics.Debug.Assert(false, message);
        }
    }
}
