using System;
using SharpGame;
using SharpGame.Sdl2;

namespace SharpGame.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var app = new SampleApplication())
            {
                app.Run(new Sdl2Window("SharpGame Samples", 100, 100, 1280, 720, SDL_WindowFlags.Hidden, false));
            }
        }
    }
}
