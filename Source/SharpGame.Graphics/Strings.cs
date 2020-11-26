using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpGame
{
    public class CStringList : DisposeBase
    {
        private Vector<IntPtr> nativeStrs = new Vector<IntPtr>();

        public IntPtr Data => nativeStrs.Data;
        public uint Count => nativeStrs.Count;


        public void Add(string str)
        {
            var ptr = Marshal.StringToHGlobalAnsi(str);
            nativeStrs.Add(ptr);
        }

        protected override void Destroy(bool disposing)
        {
            foreach(var ptr in nativeStrs)
            {
                Marshal.FreeHGlobal(ptr);
            }

            nativeStrs.Clear();
        }
    }

    public static class Strings
    {
        public static UTF8String VK_KHR_SURFACE_EXTENSION_NAME = "VK_KHR_surface";
        public static UTF8String VK_KHR_WIN32_SURFACE_EXTENSION_NAME = "VK_KHR_win32_surface";
        public static UTF8String VK_KHR_XCB_SURFACE_EXTENSION_NAME = "VK_KHR_xcb_surface";
        public static UTF8String VK_KHR_XLIB_SURFACE_EXTENSION_NAME = "VK_KHR_xlib_surface";
        public static UTF8String VK_KHR_SWAPCHAIN_EXTENSION_NAME = "VK_KHR_swapchain";
        public static UTF8String VK_EXT_DEBUG_REPORT_EXTENSION_NAME = "VK_EXT_debug_report";
        public static UTF8String StandardValidationLayeName = "VK_LAYER_KHRONOS_validation";// "VK_LAYER_LUNARG_standard_validation";
        public static UTF8String VK_DESCRIPTOR_BINDING_PARTIALLY_BOUND_BIT_EXT = "VK_DESCRIPTOR_BINDING_PARTIALLY_BOUND_BIT_EXT";
        public static UTF8String VK_KHR_GET_PHYSICAL_DEVICE_PROPERTIES_2_EXTENSION_NAME = "VK_KHR_get_physical_device_properties2";
        public static UTF8String VK_KHR_MAINTENANCE1_EXTENSION_NAME = "VK_KHR_maintenance1";
        public static UTF8String VK_EXT_INLINE_UNIFORM_BLOCK_EXTENSION_NAME = "VK_EXT_inline_uniform_block";
        public static UTF8String main = "main";

        public static void AddString(this Vector<IntPtr> strs, string str)
        {

        }
    }
}
