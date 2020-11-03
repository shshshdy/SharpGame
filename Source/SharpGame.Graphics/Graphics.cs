#define NEW_BACK_BUFF
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Vulkan;

using static Vulkan.VulkanNative;

namespace SharpGame
{
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
        public NativeList<IntPtr> EnabledExtensions { get; } = new NativeList<IntPtr>();

        public static Queue GraphicsQueue { get; protected set; }
        public static Queue WorkQueue { get; protected set; }
        public static Queue ComputeQueue { get; protected set; }
        public static Queue TransferQueue { get; protected set; }

        public Format ColorFormat => Swapchain.ColorFormat;
        public Format DepthFormat { get; protected set; }
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

        public Semaphore acquireSemaphore;

        public Graphics(Settings settings)
        {
#if DEBUG
            settings.Validation = true;
#else
            settings.Validation = false;
#endif
            Settings = settings;

            enabledFeatures.samplerAnisotropy = True;
            enabledFeatures.depthClamp = True;
            enabledFeatures.shaderStorageImageExtendedFormats = True;

            Device.Create(settings, enabledFeatures, EnabledExtensions);

            // Get a graphics queue from the Device
            GraphicsQueue = Queue.GetDeviceQueue(Device.QFGraphics, 0);
            WorkQueue = Queue.GetDeviceQueue(Device.QFGraphics, 0);
            ComputeQueue = Queue.GetDeviceQueue(Device.QFCompute, 0);
            TransferQueue = Queue.GetDeviceQueue(Device.QFTransfer, 0);

            DepthFormat = Device.GetSupportedDepthFormat();

            primaryCmdPool = new CommandBufferPool(Device.QFGraphics, CommandPoolCreateFlags.ResetCommandBuffer);

            DescriptorPoolManager = new DescriptorPoolManager();

            acquireSemaphore = new Semaphore();

        }

        public void Init(IntPtr wnd)
        {
            Swapchain = new Swapchain(wnd);

            CreateSwapChain();
            CreateDepthStencil();
            CreateDefaultRenderPass();
            CreateFrameBuffer();
            CreateCommandPool();

            Texture.Init();
            Sampler.Init();

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

            // Ensure all operations on the device have been finished before destroying resources
            WaitIdle();

            foreach(var rf in renderFrames)
            {
                rf.DeviceLost();
            }

            CreateSwapChain();
            // Recreate the frame buffers
            CreateDepthStencil();

            CreateDefaultRenderPass();
            CreateFrameBuffer();

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
        }

        public void CreateDefaultRenderPass()
        {
            AttachmentDescription[] attachments =
            {
                new AttachmentDescription(ColorFormat)
                {
                    finalLayout = ImageLayout.PresentSrcKHR
                },

                new AttachmentDescription(DepthFormat)
                {
                    finalLayout = ImageLayout.DepthStencilAttachmentOptimal
                }
            };

            SubpassDescription[] subpassDescription =
            {
                new SubpassDescription
                {
                    pipelineBindPoint = PipelineBindPoint.Graphics,
                    pColorAttachments = new []
                    {
                        new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                    },
                    pDepthStencilAttachment = new []
                    {
                        new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal)
                    },

                }
            };

            SubpassDependency[] dependencies =
            {
                new SubpassDependency
                {
                    srcSubpass = SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = PipelineStageFlags.BottomOfPipe,
                    dstStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    srcAccessMask = AccessFlags.MemoryRead,
                    dstAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dependencyFlags = DependencyFlags.ByRegion
                },

                new SubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = SubpassExternal,
                    srcStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = PipelineStageFlags.BottomOfPipe,
                    srcAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dstAccessMask = AccessFlags.MemoryRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },
            };

            RenderPassCreateInfo renderPassInfo = new RenderPassCreateInfo(attachments, subpassDescription, dependencies);
            renderPass = new RenderPass(ref renderPassInfo);
        }

        public RenderPass CreateRenderPass(bool clearColor = false, bool clearDepth = false)
        {
            AttachmentDescription[] attachments =
            {
                // Color attachment
                new AttachmentDescription(ColorFormat)
                {
                    loadOp = clearColor? AttachmentLoadOp.Clear : AttachmentLoadOp.DontCare, // AttachmentLoadOp.Load,
                    storeOp = AttachmentStoreOp.Store,
                    finalLayout = ImageLayout.PresentSrcKHR
                },

                // Depth attachment
                new AttachmentDescription(DepthFormat)
                {
                    loadOp = clearDepth? AttachmentLoadOp.Clear: AttachmentLoadOp.DontCare,
                    storeOp = AttachmentStoreOp.DontCare,
                    finalLayout = ImageLayout.DepthStencilAttachmentOptimal
                }
            };

            SubpassDescription[] subpassDescription =
            {
                new SubpassDescription
                {
                    pipelineBindPoint = PipelineBindPoint.Graphics,

                    pColorAttachments = new []
                    {
                        new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                    },

                    pDepthStencilAttachment = new []
                    {
                        new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal)
                    },
                }
            };

            // Subpass dependencies for layout transitions
            SubpassDependency[] dependencies =
            {
                new SubpassDependency
                {
                    srcSubpass = VulkanNative.SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = PipelineStageFlags.BottomOfPipe,
                    dstStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    srcAccessMask = AccessFlags.MemoryRead,
                    dstAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dependencyFlags = DependencyFlags.ByRegion
                },

                new SubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = VulkanNative.SubpassExternal,
                    srcStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = PipelineStageFlags.BottomOfPipe,
                    srcAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dstAccessMask = AccessFlags.MemoryRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },
            };

            var renderPassInfo = new RenderPassCreateInfo(attachments, subpassDescription, dependencies);
            renderPass = new RenderPass(ref renderPassInfo);
            return renderPass;
        }

        public Framebuffer[] CreateSwapChainFramebuffers(RenderPass vkRenderPass)
        {
            Span<VkImageView> attachments = stackalloc VkImageView[2];
            // Depth/Stencil attachment is the same for all frame buffers
            attachments[1] = depthStencil.imageView.handle;

            var frameBufferCreateInfo = new FramebufferCreateInfo
            {
                renderPass = vkRenderPass,
                attachments = attachments,
                width = (uint)Width,
                height = (uint)Height,
                layers = 1
            };

            // Create frame buffers for every swap chain image
            var framebuffers = new Framebuffer[Swapchain.ImageCount];
            for (uint i = 0; i < framebuffers.Length; i++)
            {
                attachments[0] = Swapchain.Buffers[i].View.handle;
                framebuffers[i] = new Framebuffer(ref frameBufferCreateInfo);
            }

            return framebuffers;
        }

        protected void CreateDepthStencil()
        {
            depthStencil?.Dispose();
            depthStencil = new RenderTexture((uint)Width, (uint)Height, 1, DepthFormat, ImageUsageFlags.DepthStencilAttachment
                /*,ImageAspectFlags.Depth | ImageAspectFlags.Stencil*/);
        }

        private void CreateCommandPool()
        {
            primaryCmdPool = new CommandBufferPool(Device.QFGraphics, CommandPoolCreateFlags.ResetCommandBuffer);
        }
        
        public static void WithCommandBuffer(Action<CommandBuffer> action)
        {
            var cmdBuffer = BeginPrimaryCmd();
            action(cmdBuffer);
            EndPrimaryCmd(cmdBuffer);
        }

        public static CommandBuffer BeginPrimaryCmd()
        {
            var cmdBuffer = primaryCmdPool.AllocateCommandBuffer(CommandBufferLevel.Primary);
            cmdBuffer.Begin(CommandBufferUsageFlags.None);
            return cmdBuffer;
        }

        public static void EndPrimaryCmd(CommandBuffer cmdBuffer)
        {
            cmdBuffer.End();

            WorkQueue.Submit(null, PipelineStageFlags.None, cmdBuffer, null);
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
            if (frame.presentFence == null)
            {
                frame.presentFence = new Fence(FenceCreateFlags.None);
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
        public static int drawCall;
        public static int triCount;

        public static void Tick(float timeStep)
        {
            drawCall = 0;
            triCount = 0;
        }
    }
}
