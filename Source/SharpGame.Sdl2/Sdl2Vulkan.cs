using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame.Sdl2
{
    public static unsafe partial class Sdl2Native
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SDL_Vulkan_CreateSurface_t(IntPtr window,
                                                IntPtr instance,
                                                IntPtr surface);
        private static SDL_Vulkan_CreateSurface_t s_sdl_vulkan_create_surface = LoadFunction<SDL_Vulkan_CreateSurface_t>("SDL_Vulkan_CreateSurface");

        public static int SDL_Vulkan_CreateSurface(IntPtr window, IntPtr instance, IntPtr surface)
            => s_sdl_vulkan_create_surface(window, instance, surface);
    }
}
