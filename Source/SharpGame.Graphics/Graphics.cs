#define NEW_BACK_BUFF
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SharpGame
{
    using static Vulkan;
    public class Settings
    {
        public UTF8String ApplicationName { get; set; }
        public bool Validation { get; set; } = false;
        public bool Fullscreen { get; set; } = false;
        public bool VSync { get; set; } = false;
        public bool SingleLoop { get; set; }
    }

    public unsafe partial class Graphics : System<Graphics>
    {
        public Settings Settings { get; } = new Settings();

        public Stats stats = new Stats();

        public VkPhysicalDeviceFeatures enabledFeatures;
        public Vector<IntPtr> EnabledExtensions { get; } = new Vector<IntPtr>();

        public static Queue GraphicsQueue { get; protected set; }
        public static Queue WorkQueue { get; protected set; }
        public static Queue ComputeQueue { get; protected set; }
        public static Queue TransferQueue { get; protected set; }

        public VkFormat ColorFormat => Swapchain.ColorFormat;
        public VkFormat DepthFormat { get; protected set; }
        public Swapchain Swapchain { get; private set; }

        public uint Width { get; private set; }
        public uint Height { get; private set; }

        public int ImageCount => (int)Swapchain.ImageCount;

        private Framebuffer[] framebuffers;
        public Framebuffer[] Framebuffers => framebuffers;

        internal static DescriptorPoolManager DescriptorPoolManager { get; private set; }

        private static CommandBufferPool primaryCmdPool;

        private RenderPass renderPass;
        public RenderPass RenderPass => renderPass;

        private RenderTexture depthStencil;
        public RenderTexture DepthRT => depthStencil;

        List<RenderContext> renderFrames = new List<RenderContext>();
        public RenderContext WorkFrame => renderFrames[WorkContext];
        public RenderContext renderFrame;

        public VkSemaphore acquireSemaphore;

        public Graphics(Settings settings)
        {
#if DEBUG
            //settings.Validation = true;
#else
            settings.Validation = false;
#endif
            Settings = settings;

            enabledFeatures.samplerAnisotropy = true;
            enabledFeatures.depthClamp = true;
            enabledFeatures.shaderStorageImageExtendedFormats = true;

            Device.Create(settings, enabledFeatures, EnabledExtensions);

            // Get a graphics queue from the Device
            GraphicsQueue = Queue.GetDeviceQueue(Device.QFGraphics, 0);
            WorkQueue = Queue.GetDeviceQueue(Device.QFGraphics, 0);
            ComputeQueue = Queue.GetDeviceQueue(Device.QFCompute, 0);
            TransferQueue = Queue.GetDeviceQueue(Device.QFTransfer, 0);

            DepthFormat = Device.GetSupportedDepthFormat();

            primaryCmdPool = new CommandBufferPool(Device.QFGraphics, VkCommandPoolCreateFlags.ResetCommandBuffer);

            DescriptorPoolManager = new DescriptorPoolManager();

            acquireSemaphore = new VkSemaphore(VkSemaphoreCreateFlags.None);

        }

        public void Init(IntPtr wnd)
        {
            Swapchain = new Swapchain(wnd);

            CreateSwapChain();

            primaryCmdPool = new CommandBufferPool(Device.QFGraphics, VkCommandPoolCreateFlags.ResetCommandBuffer);

            Texture.Init();
            //Sampler.Init();

            RenderContext.Init();

            for (int i = 0; i < Swapchain.ImageCount; i++)
            {
                renderFrames.Add(new RenderContext(i));
            }


        }

        protected override void Destroy(bool disposing)
        {
            RenderContext.Shutdown();

            Device.Shutdown();

            base.Destroy(disposing);
        }

        public void Resize(int w, int h)
        {
            Width = (uint)w;
            Height = (uint)h;

            WaitIdle();

            foreach(var rf in renderFrames)
            {
                rf.DeviceLost();
            }

            CreateSwapChain();

            foreach (var rf in renderFrames)
            {
                rf.DeviceReset();
            }

        }

        protected void CreateFrameBuffer()
        {
            if (Framebuffers != null)
            {
                for (int i = 0; i < Framebuffers.Length; i++)
                {
                    Framebuffers[i].Dispose();
                }
            }

            framebuffers = CreateSwapChainFramebuffers(renderPass);
        }

        private void CreateSwapChain()
        {
            uint width = Width, height = Height;
            Swapchain.Create(ref width, ref height, Settings.VSync);

            Width = width;
            Height = height;

            CreateDepthStencil();
            CreateDefaultRenderPass();
            CreateFrameBuffer();
        }

        public void CreateDefaultRenderPass()
        {
            VkAttachmentDescription[] attachments =
            {
                new VkAttachmentDescription(ColorFormat)
                {
                    finalLayout = VkImageLayout.PresentSrcKHR
                },

                new VkAttachmentDescription(DepthFormat)
                {
                    finalLayout = VkImageLayout.DepthStencilAttachmentOptimal
                }
            };

            SubpassDescription[] subpassDescription =
            {
                new SubpassDescription
                {
                    pipelineBindPoint = VkPipelineBindPoint.Graphics,
                    pColorAttachments = new []
                    {
                        new VkAttachmentReference(0, VkImageLayout.ColorAttachmentOptimal)
                    },
                    pDepthStencilAttachment = new []
                    {
                        new VkAttachmentReference(1, VkImageLayout.DepthStencilAttachmentOptimal)
                    },

                }
            };

            VkSubpassDependency[] dependencies =
            {
                new VkSubpassDependency
                {
                    srcSubpass = SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = VkPipelineStageFlags.BottomOfPipe,
                    dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                    srcAccessMask = VkAccessFlags.MemoryRead,
                    dstAccessMask = (VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite),
                    dependencyFlags = VkDependencyFlags.ByRegion
                },

                new VkSubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = SubpassExternal,
                    srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = VkPipelineStageFlags.BottomOfPipe,
                    srcAccessMask = (VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite),
                    dstAccessMask = VkAccessFlags.MemoryRead,
                    dependencyFlags = VkDependencyFlags.ByRegion
                },
            };

            renderPass = new RenderPass(attachments, subpassDescription, dependencies);
        }

        public RenderPass CreateRenderPass(bool clearColor = false, bool clearDepth = false)
        {
            VkAttachmentDescription[] attachments =
            {
                // Color attachment
                new VkAttachmentDescription(ColorFormat)
                {
                    loadOp = clearColor? VkAttachmentLoadOp.Clear : VkAttachmentLoadOp.DontCare, // AttachmentLoadOp.Load,
                    storeOp = VkAttachmentStoreOp.Store,
                    finalLayout = VkImageLayout.PresentSrcKHR
                },

                // Depth attachment
                new VkAttachmentDescription(DepthFormat)
                {
                    loadOp = clearDepth? VkAttachmentLoadOp.Clear: VkAttachmentLoadOp.DontCare,
                    storeOp = VkAttachmentStoreOp.DontCare,
                    finalLayout = VkImageLayout.DepthStencilAttachmentOptimal
                }
            };

            SubpassDescription[] subpassDescription =
            {
                new SubpassDescription
                {
                    pipelineBindPoint = VkPipelineBindPoint.Graphics,

                    pColorAttachments = new []
                    {
                        new VkAttachmentReference(0, VkImageLayout.ColorAttachmentOptimal)
                    },

                    pDepthStencilAttachment = new []
                    {
                        new VkAttachmentReference(1, VkImageLayout.DepthStencilAttachmentOptimal)
                    },
                }
            };

            // Subpass dependencies for layout transitions
            VkSubpassDependency[] dependencies =
            {
                new VkSubpassDependency
                {
                    srcSubpass = Vulkan.SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = VkPipelineStageFlags.BottomOfPipe,
                    dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                    srcAccessMask = VkAccessFlags.MemoryRead,
                    dstAccessMask = (VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite),
                    dependencyFlags = VkDependencyFlags.ByRegion
                },

                new VkSubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = Vulkan.SubpassExternal,
                    srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = VkPipelineStageFlags.BottomOfPipe,
                    srcAccessMask = (VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite),
                    dstAccessMask = VkAccessFlags.MemoryRead,
                    dependencyFlags = VkDependencyFlags.ByRegion
                },
            };

            renderPass = new RenderPass(attachments, subpassDescription, dependencies);
            return renderPass;
        }

        public Framebuffer[] CreateSwapChainFramebuffers(RenderPass vkRenderPass)
        {
            Span<VkImageView> attachments = stackalloc VkImageView[2];
            // Depth/Stencil attachment is the same for all frame buffers
            attachments[1] = depthStencil.imageView.handle;

            // Create frame buffers for every swap chain image
            var framebuffers = new Framebuffer[Swapchain.ImageCount];
            for (uint i = 0; i < framebuffers.Length; i++)
            {
                attachments[0] = Swapchain.ImageViews[i].handle;
                framebuffers[i] = new Framebuffer(vkRenderPass, Width, Height, 1, attachments);
            }

            return framebuffers;
        }

        protected void CreateDepthStencil()
        {
            depthStencil?.Dispose();
            depthStencil = new RenderTexture((uint)Width, (uint)Height, 1, DepthFormat, VkImageUsageFlags.DepthStencilAttachment
                /*,ImageAspectFlags.Depth | ImageAspectFlags.Stencil*/);
        }

        public static void WithCommandBuffer(Action<CommandBuffer> action)
        {
            var cmdBuffer = BeginPrimaryCmd();
            action(cmdBuffer);
            EndPrimaryCmd(cmdBuffer);
        }

        public static CommandBuffer BeginPrimaryCmd()
        {
            var cmdBuffer = primaryCmdPool.AllocateCommandBuffer(VkCommandBufferLevel.Primary);
            cmdBuffer.Begin(VkCommandBufferUsageFlags.None);
            return cmdBuffer;
        }

        public static void EndPrimaryCmd(CommandBuffer cmdBuffer)
        {
            cmdBuffer.End();

            WorkQueue.Submit(VkSemaphore.Null, VkPipelineStageFlags.None, cmdBuffer, VkSemaphore.Null);
            WorkQueue.WaitIdle();

            primaryCmdPool.FreeCommandBuffer(cmdBuffer);

        }

        public void WaitIdle()
        {
            Device.WaitIdle();
        }

        public bool BeginRender()
        {
            MainSemWait();

            Profiler.BeginSample("Acquire");

            renderContext = workContext;
            var sem = contextToImage[renderContext] == -1 ? acquireSemaphore : renderFrames[workContext].acquireSemaphore;
            Swapchain.AcquireNextImage(sem, out int imageIndex);

            workContext = (workContext + 1) % ImageCount;
            contextToImage[workContext] = imageIndex;

            //Log.Info("-------acquire next image :" + imageIndex + "  context: " + workContext);

            RenderSemPost();

            if (contextToImage[renderContext] == -1)
            {
                Profiler.EndSample();
                return false;
            }

            var frame = renderFrames[renderContext];
            if (!frame.presentFence)
            {
                frame.presentFence = new VkFence(VkFenceCreateFlags.None);
            }
            else
            {
                VkResult fenceStatus = frame.presentFence.GetStatus();
                if (VkResult.NotReady == fenceStatus)
                {
                    frame.presentFence.Wait();
                }

                // reset the fence
                if (VkResult.Success == frame.presentFence.GetStatus())
                {
                    frame.presentFence.Reset();
                }
            }

            renderFrame = frame;
            renderFrame.imageIndex = contextToImage[renderContext];

            Profiler.EndSample();


            return true;
        }

        public void EndRender()
        {
            Profiler.BeginSample("Present");

            Swapchain.QueuePresent(GraphicsQueue, (uint)renderFrame.imageIndex, renderFrame.renderSemaphore);

            GraphicsQueue.Submit(null, renderFrame.presentFence);

            GraphicsQueue.WaitIdle();

            foreach (var action in renderFrame.postActions)
            {
                action.Invoke();
            }

            Profiler.EndSample();

        }

        #region MULTITHREADED   

        static int mainThreadID;
        static int renderThreadID;

        public static bool IsMainThread => mainThreadID == Thread.CurrentThread.ManagedThreadId;
        public static bool IsRenderThread => renderThreadID == Thread.CurrentThread.ManagedThreadId;
        public static void SetMainThread() => mainThreadID = Thread.CurrentThread.ManagedThreadId;
        public static void SetRenderThread() => renderThreadID = Thread.CurrentThread.ManagedThreadId;

        public bool SingleLoop => Settings.SingleLoop;

        private System.Threading.Semaphore renderSem = new System.Threading.Semaphore(0, 1);
        private System.Threading.Semaphore mainSem = new System.Threading.Semaphore(1, 1);

        private int workContext = 0;
        private int renderContext = -1;
        int[] contextToImage = new int[3] { -1, -1, -1 };
        public int WorkContext => workContext;
        //public int RenderContext => renderContext;
        public int WorkImage => contextToImage[workContext];
        public int RenderImage => contextToImage[renderContext];

        public void MainSemPost()
        {
            if (!SingleLoop)
            {
                mainSem.Release();
            }
        }

        bool MainSemWait()
        {
            if (SingleLoop)
            {
                return true;
            }

            long curTime = Stopwatch.GetTimestamp();
            bool ok = mainSem.WaitOne(-1);
            if (ok)
            {
                stats.logicWait = Stopwatch.GetTimestamp() - curTime;
                return true;
            }

            return false;
        }

        void RenderSemPost()
        {
            if (!SingleLoop)
            {
                renderSem.Release();
            }
        }

        public void WaitRender()
        {
            if (!SingleLoop)
            {
                long curTime = Stopwatch.GetTimestamp();
                bool ok = renderSem.WaitOne();
                stats.renderWait = Stopwatch.GetTimestamp() - curTime;
            }

        }

        #endregion

    }

    public struct Stats
    {
        public long logicWait;
        public long renderWait;

        public static long triCount;
        public static long indirectTriCount;
        public static int drawCall;
        public static int drawIndirect;
        public static int dispatch;
        public static int dispatchIndirect;

        public static void Tick(float timeStep)
        {
            triCount = 0; 
            indirectTriCount = 0;
            drawCall = 0;
            drawIndirect = 0;
            dispatch = 0;
            dispatchIndirect = 0;
        }
    }
}
