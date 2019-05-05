using System;
using System.Reflection;

namespace SharpGame.Samples.TexturedCube
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {

            using (var app = new TexturedCubeApp())
            {
                app.Run(new MacOSWindow(
                Assembly.GetExecutingAssembly().GetName().Name, app));
            }
            /*
            using (var window = new MacOSWindow(
                Assembly.GetExecutingAssembly().GetName().Name, app
                ))
            {
                window.Run();
            }*/
        }
    }
}
