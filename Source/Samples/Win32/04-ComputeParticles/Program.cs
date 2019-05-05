using System;
using System.Reflection;

namespace SharpGame.Samples.ComputeParticles
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            using (var app = new ComputeParticlesApp())
            {
                app.Run(new Win32Window(
                Assembly.GetExecutingAssembly().GetName().Name, app));
            }
        }
    }
}
