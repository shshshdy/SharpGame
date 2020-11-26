using System;
using System.Runtime.InteropServices;

namespace SharpGame
{
    public struct DebugReportCallbackInfo
    {
        public VkDebugReportFlagsEXT Flags;
        public VkDebugReportObjectTypeEXT ObjectType;
        public long Object;
        public IntPtr Location;
        public int MessageCode;
        public string LayerPrefix;
        public string Message;
        public IntPtr UserData;
    }

    public unsafe class DebugReportCallbackExt : DisposeBase
    {
        private DebugReportCallback _callback;
        public VkDebugReportCallbackEXT handle;

        internal DebugReportCallbackExt(VkInstance parent, VkDebugReportFlagsEXT flags,
            Func<DebugReportCallbackInfo, bool> callback, IntPtr userData = default(IntPtr))
        {
            Parent = parent;

            Func<DebugReportCallbackInfo, bool> createInfoCallback = callback;
            IntPtr callbackHandle = IntPtr.Zero;
            if (createInfoCallback != null)
            {
                _callback = (flags, objectType, @object, location, messageCode, layerPrefix, message, userData)
                    => createInfoCallback(new DebugReportCallbackInfo
                    {
                        Flags = flags,
                        ObjectType = objectType,
                        Object = @object,
                        Location = location,
                        MessageCode = messageCode,
                        LayerPrefix = Utilities.FromPointer(layerPrefix),
                        Message = Utilities.FromPointer(message),
                        UserData = userData
                    });
                callbackHandle = Marshal.GetFunctionPointerForDelegate(_callback);
            }

            var nativeCreateInfo = new VkDebugReportCallbackCreateInfoEXT
            {
                sType = VkStructureType.DebugReportCallbackCreateInfoEXT,
                flags = flags,
                pfnCallback = callbackHandle,
                pUserData = (void*)userData
            };

            long handle;
            VkResult result = vkCreateDebugReportCallbackEXT(Parent)(Parent.Handle, &nativeCreateInfo, null, &handle);

            this.handle = (VkDebugReportCallbackEXT)(ulong)handle;
   
        }

        public VkInstance Parent { get; }

        protected override void Destroy(bool disposing)
        {
            Vulkan.vkDestroyDebugReportCallbackEXT(Parent, handle, null);
            _callback = null;
         
        }
        
        private delegate VkBool32 DebugReportCallback(
            VkDebugReportFlagsEXT flags, VkDebugReportObjectTypeEXT objectType, long @object,
            IntPtr location, int messageCode, byte* layerPrefix, byte* message, IntPtr userData);

        private delegate VkResult vkCreateDebugReportCallbackEXTDelegate(IntPtr instance, VkDebugReportCallbackCreateInfoEXT* createInfo, VkAllocationCallbacks* allocator, long* callback);
        private static vkCreateDebugReportCallbackEXTDelegate vkCreateDebugReportCallbackEXT(VkInstance instance) => instance.GetProc<vkCreateDebugReportCallbackEXTDelegate>(nameof(vkCreateDebugReportCallbackEXT));
    }


}
