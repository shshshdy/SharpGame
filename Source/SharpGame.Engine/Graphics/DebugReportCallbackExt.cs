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
        protected override void Destroy()
        {
            VulkanNative.vkDestroyDebugReportCallbackEXT(Parent, handle, null);
            _callback = null;
         
        }

        public static IntPtr GetProcAddr(VkInstance instance, string name)
        {
            int byteCount = CString.GetMaxByteCount(name);
            var dstPtr = stackalloc byte[byteCount];
            CString.ToPointer(name, dstPtr, byteCount);
            var addr = VulkanNative.vkGetInstanceProcAddr(instance, dstPtr);
            return addr;
        }

        public unsafe static TDelegate GetProc<TDelegate>(VkInstance instance, string name) where TDelegate : class
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            IntPtr ptr = GetProcAddr(instance, name);
            TDelegate proc = ptr != IntPtr.Zero
                ? Marshal.GetDelegateForFunctionPointer<TDelegate>(ptr)
                : null;

            return proc;
        }

        private delegate VkBool32 DebugReportCallback(
            DebugReportFlagsExt flags, DebugReportObjectTypeExt objectType, long @object,
            IntPtr location, int messageCode, byte* layerPrefix, byte* message, IntPtr userData);

        private delegate VkResult vkCreateDebugReportCallbackEXTDelegate(IntPtr instance, VkDebugReportCallbackCreateInfoEXT* createInfo, VkAllocationCallbacks* allocator, long* callback);
        private static vkCreateDebugReportCallbackEXTDelegate vkCreateDebugReportCallbackEXT(VkInstance instance) => GetProc<vkCreateDebugReportCallbackEXTDelegate>(instance, nameof(vkCreateDebugReportCallbackEXT));
    }

    /// <summary>
    /// Structure specifying parameters of a newly created debug report callback.
    /// <para>
    /// For each <see cref="DebugReportCallbackCreateInfoExt"/> that is created the flags determine
    /// when that function is called.
    /// </para>
    /// <para>
    /// A callback will be made for issues that match any bit set in its flags. The callback will
    /// come directly from the component that detected the event, unless some other layer intercepts
    /// the calls for its own purposes (filter them in different way, log to system error log, etc.)
    /// An application may receive multiple callbacks if multiple <see
    /// cref="DebugReportCallbackCreateInfoExt"/> objects were created.
    /// </para>
    /// <para>A callback will always be executed in the same thread as the originating Vulkan call.</para>
    /// <para>
    /// A callback may be called from multiple threads simultaneously (if the application is making
    /// Vulkan calls from multiple threads).
    /// </para>
    /// </summary>
    public struct DebugReportCallbackCreateInfoExt
    {
        /// <summary>
        /// A bitmask specifying which event(s) will cause this callback to be called. Flags are
        /// interpreted as bitmasks and multiple can be set.
        /// </summary>
        public VkDebugReportFlagsEXT Flags;
        /// <summary>
        /// The application callback function to call.
        /// </summary>
        public Func<DebugReportCallbackInfo, bool> Callback;
        /// <summary>
        /// User data to be passed to the callback.
        /// </summary>
        public IntPtr UserData;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugReportCallbackCreateInfoExt"/> structure.
        /// </summary>
        /// <param name="flags">
        /// A bitmask specifying which event(s) will cause this callback to be called. Flags are
        /// interpreted as bitmasks and multiple can be set.
        /// </param>
        /// <param name="callback">The application callback function to call.</param>
        /// <param name="userData">User data to be passed to the callback.</param>
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

    /// <summary>
    /// Structure specifying arguments for a debug report callback function.
    /// </summary>
    public struct DebugReportCallbackInfo
    {
        /// <summary>
        /// The <see cref="DebugReportFlagsExt"/> that triggered this callback.
        /// </summary>
        public DebugReportFlagsExt Flags;
        /// <summary>
        /// The <see cref="DebugReportObjectTypeExt"/> specifying the type of object being used or
        /// created at the time the event was triggered.
        /// </summary>
        public DebugReportObjectTypeExt ObjectType;
        /// <summary>
        /// The object where the issue was detected. <see cref="Object"/> may be 0 if there is no
        /// object associated with the event.
        /// </summary>
        public long Object;
        /// <summary>
        /// The component (layer, driver, loader) defined value that indicates the location of the
        /// trigger. This is an optional value.
        /// </summary>
        public IntPtr Location;
        /// <summary>
        /// The layer-defined value indicating what test triggered this callback.
        /// </summary>
        public int MessageCode;
        /// <summary>
        /// The abbreviation of the component making the callback.
        /// </summary>
        public string LayerPrefix;
        /// <summary>
        /// The string detailing the trigger conditions.
        /// </summary>
        public string Message;
        /// <summary>
        /// The user data given when the <see cref="DebugReportCallbackCreateInfoExt"/> was created.
        /// </summary>
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
        /// cref="CommandBuffer.CmdClearColorImage"/> when a <see cref="FGPass"/> load op would
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
