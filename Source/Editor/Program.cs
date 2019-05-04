using System;
using Unique;
using Unique.Sdl2;

namespace SharpGame.Editor
{
    class Program
    {
        static void Main(string[] args)
        {
            //CoreTest.Go();

           // Console.ReadLine();

            var app = new EditorApplication();
            app.Run(new SdlGameWindow("UniqueStudio", 100, 100, 1280, 720, SDL_WindowFlags.Resizable));
        }
    }
}
