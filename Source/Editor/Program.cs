using System;
using SharpGame;
using SharpGame.Sdl2;

namespace SharpGame.Editor
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var app = new EditorApplication())
            {
                app.Run(new Sdl2Window("SharpGame", 100, 100, 1280, 720, SDL_WindowFlags.Hidden, false));
            }
        }
    }
}
