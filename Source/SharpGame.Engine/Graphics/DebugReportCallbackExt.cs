using SharpGame;
using System;
using System.Runtime.InteropServices;

namespace Vulkan
{
    /// <summary>
    /// Opaque handle to a debug report callback object.
    /// </summary>
    public unsafe class DebugReportCallbackExt : DisposeBase
    {
        // We need to keep the callback alive since it is being called from unmanaged code.
        private DebugReportCallback _callback;
        public VkDebugReportCallbackEXT handle;

        internal DebugReportCallbackExt(VkInstance parent,
            ref DebugReportCallbackCreateInfoExt createInfo)
        {
            Parent = parent;

            Func<DebugReportCallbackInfo, bool> createInfoCallback = createInfo.Callback;
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

            var nativeCreateInfo = VkDebugReportCallbackCreateInfoEXT.New();

            nativeCreateInfo.flags = createInfo.Flags;
            nativeCreateInfo.pfnCallback = callbackHandle;
            nativeCreateInfo.pUserData = (void*)createInfo.UserData;
            long handle;
            VkResult result = vkCreateDebugReportCallbackEXT(Parent)
                (Parent.Handle, &nativeCreateInfo, null, &handle);

            this.handle = (VkDebugReportCallbackEXT)(ulong)handle;
   
        }

        /// <summary>
        /// Gets the parent of the resource.
        /// </summary>
        public VkInstance Parent { get; }

        /// <summary>
        /// Destroy a debug report callback object.
        /// </summary>
        protected override void Destroy(bool disposing)
        {
            VulkanNative.vkDestroyDebugReportCallbackEXT(Parent, handle, null);
            _callback = null;
         
        }
        
        private delegate VkBool32 DebugReportCallback(
            DebugReportFlagsExt flags, DebugReportObjectTypeExt objectType, long @object,
            IntPtr location, int messageCode, byte* layerPrefix, byte* message, IntPtr userData);

        private delegate VkResult vkCreateDebugReportCallbackEXTDelegate(IntPtr instance, VkDebugReportCallbackCreateInfoEXT* createInfo, VkAllocationCallbacks* allocator, long* callback);
        private static vkCreateDebugReportCallbackEXTDelegate vkCreateDebugReportCallbackEXT(VkInstance instance) => instance.GetProc<vkCreateDebugReportCallbackEXTDelegate>(nameof(vkCreateDebugReportCallbackEXT));
    }

    public struct DebugReportCallbackCreateInfoExt
    {
        public VkDebugReportFlagsEXT Flags;
        public Func<DebugReportCallbackInfo, bool> Callback;
        public IntPtr UserData;

        public DebugReportCallbackCreateInfoExt(
            VkDebugReportFlagsEXT flags,
            Func<DebugReportCallbackInfo, bool> callback,
            IntPtr userData = default(IntPtr))
        {
            Flags = flags;
            Callback = callback;
            UserData = userData;
        }

        internal unsafe void ToNative(ref VkDebugReportCallbackCreateInfoEXT native, IntPtr callback)
        {
            native.flags = Flags;
            native.pfnCallback = callback;
            native.pUserData = (void*)UserData;
        }
    }

    public struct DebugReportCallbackInfo
    {
        public DebugReportFlagsExt Flags;
        public DebugReportObjectTypeExt ObjectType;
        public long Object;
        public IntPtr Location;
        public int MessageCode;
        public string LayerPrefix;
        public string Message;
        public IntPtr UserData;
    }

    /// <summary>
    /// Bitmask specifying events which cause a debug report callback.
    /// </summary>
    [Flags]
    public enum DebugReportFlagsExt
    {
        /// <summary>
        /// Specifies an informational message such as resource details that may be handy when
        /// debugging an application.
        /// </summary>
        Information = 1 << 0,
        /// <summary>
        /// Specifies use of Vulkan that may expose an app bug. Such cases may not be immediately
        /// harmful, such as a fragment shader outputting to a location with no attachment. Other
        /// cases may point to behavior that is almost certainly bad when unintended such as using an
        /// image whose memory has not been filled. In general if you see a warning but you know that
        /// the behavior is intended/desired, then simply ignore the warning.
        /// </summary>
        Warning = 1 << 1,
        /// <summary>
        /// Specifies a potentially non-optimal use of Vulkan. E.g. using <see
        /// cref="CommandBuffer.CmdClearColorImage"/> when a <see cref="FrameGraphPass"/> load op would
        /// have worked.
        /// </summary>
        PerformanceWarning = 1 << 2,
        /// <summary>
        /// Specifies that an error that may cause undefined results, including an application crash.
        /// </summary>
        Error = 1 << 3,
        /// <summary>
        /// Specifies diagnostic information from the loader and layers.
        /// </summary>
        Debug = 1 << 4,
        /// <summary>
        /// All flags.
        /// </summary>
        All = Information | Warning | PerformanceWarning | Error | Debug
    }

    /// <summary>
    /// Specify the type of an object handle.
    /// </summary>
    public enum DebugReportObjectTypeExt
    {
        /// <summary>
        /// Specifies an unknown object.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Specifies an <see cref="Vulkan.Instance"/>.
        /// </summary>
        Instance = 1,
        /// <summary>
        /// Specifies a <see cref="Vulkan.PhysicalDevice"/>.
        /// </summary>
        PhysicalDevice = 2,
        /// <summary>
        /// Specifies a <see cref="Vulkan.Device"/>.
        /// </summary>
        Device = 3,
        /// <summary>
        /// Specifies a <see cref="Vulkan.Queue"/>.
        /// </summary>
        Queue = 4,
        /// <summary>
        /// Specifies a <see cref="Vulkan.Semaphore"/>.
        /// </summary>
        Semaphore = 5,
        /// <summary>
        /// Specifies a <see cref="Vulkan.CommandBuffer"/>.
        /// </summary>
        CommandBuffer = 6,
        /// <summary>
        /// Specifies a <see cref="Vulkan.Fence"/>.
        /// </summary>
        Fence = 7,
        /// <summary>
        /// Specifies a <see cref="Vulkan.DeviceMemory"/>.
        /// </summary>
        DeviceMemory = 8,
        /// <summary>
        /// Specifies a <see cref="Vulkan.Buffer"/>.
        /// </summary>
        Buffer = 9,
        /// <summary>
        /// Specifies an <see cref="Vulkan.Image"/>.
        /// </summary>
        Image = 10,
        /// <summary>
        /// Specifies an <see cref="Vulkan.Event"/>.
        /// </summary>
        Event = 11,
        /// <summary>
        /// Specifies a <see cref="Vulkan.QueryPool"/>.
        /// </summary>
        QueryPool = 12,
        /// <summary>
        /// Specifies an <see cref="Vulkan.BufferView"/>.
        /// </summary>
        BufferView = 13,
        /// <summary>
        /// Specifies an <see cref="Vulkan.ImageView"/>.
        /// </summary>
        ImageView = 14,
        /// <summary>
        /// Specifies a <see cref="Vulkan.ShaderModule"/>.
        /// </summary>
        ShaderModule = 15,
        /// <summary>
        /// Specifies a <see cref="Vulkan.PipelineCache"/>.
        /// </summary>
        PipelineCache = 16,
        /// <summary>
        /// Specifies a <see cref="Vulkan.PipelineLayout"/>.
        /// </summary>
        PipelineLayout = 17,
        /// <summary>
        /// Specifies a <see cref="Vulkan.RenderPass"/>.
        /// </summary>
        RenderPass = 18,
        /// <summary>
        /// Specifies a <see cref="Vulkan.Pipeline"/>.
        /// </summary>
        Pipeline = 19,
        /// <summary>
        /// Specifies a <see cref="Vulkan.DescriptorSetLayout"/>.
        /// </summary>
        DescriptorSetLayout = 20,
        /// <summary>
        /// Specifies a <see cref="Vulkan.Sampler"/>.
        /// </summary>
        Sampler = 21,
        /// <summary>
        /// Specifies a <see cref="Vulkan.DescriptorPool"/>.
        /// </summary>
        DescriptorPool = 22,
        /// <summary>
        /// Specifies a <see cref="Vulkan.DescriptorSet"/>.
        /// </summary>
        DescriptorSet = 23,
        /// <summary>
        /// Specifies a <see cref="Vulkan.Framebuffer"/>.
        /// </summary>
        Framebuffer = 24,
        /// <summary>
        /// Specifies a <see cref="Vulkan.CommandPool"/>.
        /// </summary>
        CommandPool = 25,
        /// <summary>
        /// Specifies a <see cref="Khr.SurfaceKhr"/>.
        /// </summary>
        SurfaceKhr = 26,
        /// <summary>
        /// Specifies a <see cref="Khr.SwapchainKhr"/>.
        /// </summary>
        SwapchainKhr = 27,
        /// <summary>
        /// Specifies a <see cref="DebugReportCallbackExt"/>.
        /// </summary>
        DebugReportCallback = 28,
        /// <summary>
        /// Specifies a <see cref="Khr.DisplayKhr"/>.
        /// </summary>
        DisplayKhr = 29,
        /// <summary>
        /// Specifies a <see cref="Khr.DisplayModeKhr"/>.
        /// </summary>
        DisplayModeKhr = 30,
        /// <summary>
        /// Specifies a <see cref="Nvx.ObjectTableNvx"/>.
        /// </summary>
        ObjectTableNvx = 31,
        /// <summary>
        /// Specifies a <see cref="Nvx.IndirectCommandsLayoutNvx"/>.
        /// </summary>
        IndirectCommandsLayoutNvx = 32,
        /// <summary>
        /// Specifies a <see cref="ValidationCacheExt"/>.
        /// </summary>
        ValidationCache = 33,
        /// <summary>
        /// Specifies a <see cref="Khr.DescriptorUpdateTemplateKhr"/>.
        /// </summary>
        DescriptorUpdateTemplateKhrExt = 1000085000,
        SamplerYcbcrConversionKhrExt = 1000156000
    }
}
