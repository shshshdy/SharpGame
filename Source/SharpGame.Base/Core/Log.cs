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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public static void Warn(string msg, params object[] arg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg, arg);
            Console.ResetColor();
        }

        public static void Error(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public static void Error(string msg, params object[] arg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg, arg);
            Console.ResetColor();
        }

        public static void Exception(Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            Console.ResetColor();
        }

        [Conditional("RENDER_LOG")]
        public static void Render(string msg, params object[] arg)
        {
            Console.WriteLine(msg, arg);
        }

        public static void Error(int line, string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log.Error("[line " + line + "]: " +  message);
            Console.ResetColor();
        }

    }
}
