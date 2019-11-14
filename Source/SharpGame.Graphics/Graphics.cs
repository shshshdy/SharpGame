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

        public static VkDevice device { get; protected set; }
        public static Queue GraphicsQueue { get; protected set; }
        public static Queue ComputeQueue { get; protected set; }

        public Format ColorFormat => Swapchain.ColorFormat;
        public Format DepthFormat { get; protected set; }
        public Swapchain Swapchain { get; private set; }

        public uint Width { get; private set; }
        public uint Height { get; private set; }

        public uint ImageCount => Swapchain.ImageCount;

        private Framebuffer[] framebuffers;
        public Framebuffer[] Framebuffers => framebuffers;

        internal static DescriptorPoolManager DescriptorPoolManager { get; private set; }

        private CommandBufferPool workCmdPool;

        private static CommandBufferPool commandPool;

        private RenderPass renderPass;
        public RenderPass RenderPass => renderPass;

        private uint currentImage = (uint)0xffffffff;
        private uint nextImage = 0;

        public uint RenderImage => currentImage;
        public uint WorkImage => nextImage;


        public Semaphore PresentComplete { get; }
        public Semaphore RenderComplete { get; }

        private RenderTarget depthStencil;
        public RenderTarget DepthRT => depthStencil;

        private TransientBufferManager transientVB = new TransientBufferManager(BufferUsageFlags.VertexBuffer, 1024 * 1024);
        private TransientBufferManager transientIB = new TransientBufferManager(BufferUsageFlags.IndexBuffer, 1024 * 1024);

        public class BackBuffer
        {
            public uint imageIndex;

            public Semaphore acquireSemaphore;
            public Semaphore preRenderSemaphore;
            public Semaphore computeSemaphore;
            public Semaphore renderSemaphore;
            public Fence presentFence;
        }

        Queue<BackBuffer> backBuffers = new Queue<BackBuffer>();
        public BackBuffer currentBuffer;

        public Graphics(Settings settings)
        {
#if DEBUG
            //settings.Validation = true;
#else
            settings.Validation = false;
#endif
            Settings = settings;

            enabledFeatures.samplerAnisotropy = True;
            enabledFeatures.depthClamp = True;
            enabledFeatures.shaderStorageImageExtendedFormats = True;

            device = Device.Create(settings, enabledFeatures, EnabledExtensions);

            // Get a graphics queue from the Device
            GraphicsQueue = Queue.GetDeviceQueue(Device.QFGraphics, 0);
            ComputeQueue = Queue.GetDeviceQueue(Device.QFCompute, 0);
            DepthFormat = Device.GetSupportedDepthFormat();

            // Create synchronization objects
            PresentComplete = new Semaphore();
            RenderComplete = new Semaphore();

            commandPool = new CommandBufferPool(Device.QFGraphics, CommandPoolCreateFlags.ResetCommandBuffer);

            DescriptorPoolManager = new DescriptorPoolManager();

            Texture.Init();

            Sampler.Init();

        }

        public void Init(IntPtr wnd)
        {
            Swapchain = new Swapchain(wnd);

            CreateSwapChain();
            CreateDepthStencil();
            CreateDefaultRenderPass();
            CreateFrameBuffer();
            CreateCommandPool();
            CreateCommandBuffers();

            for (int i = 0; i < Swapchain.ImageCount; i++)
            {
                backBuffers.Enqueue(new BackBuffer
                {
                    acquireSemaphore = new Semaphore(),
                    preRenderSemaphore = new Semaphore(),
                    computeSemaphore = new Semaphore(),
                    renderSemaphore = new Semaphore(),
                    //presentFence = new Fence(FenceCreateFlags.None)
                });
            }
        }

        protected override void Destroy()
        {
            Device.Shutdown();

            base.Destroy();
        }

        public void Resize(int w, int h)
        {
            Width = (uint)w;
            Height = (uint)h;

            // Ensure all operations on the device have been finished before destroying resources
            WaitIdle();

            CreateSwapChain();
            // Recreate the frame buffers
            CreateDepthStencil();

            CreateFrameBuffer();

            // Command buffers need to be recreated as they may store
            // references to the recreated frame buffer
            CreateCommandBuffers();

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
                new AttachmentDescription
                {
                    format = ColorFormat,
                    finalLayout = ImageLayout.PresentSrcKHR
                },

                new AttachmentDescription
                {
                    format = DepthFormat,
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
                new AttachmentDescription
                {
                    format = ColorFormat,
                    loadOp = clearColor? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
                    storeOp = AttachmentStoreOp.Store,
                    finalLayout = ImageLayout.PresentSrcKHR
                },

                // Depth attachment
                new AttachmentDescription
                {
                    format = DepthFormat,
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
            attachments[1] = depthStencil.view.handle;

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
            depthStencil = new RenderTarget((uint)Width, (uint)Height, 1, DepthFormat, ImageUsageFlags.DepthStencilAttachment, ImageAspectFlags.Depth | ImageAspectFlags.Stencil);
        }
        
        private void CreateCommandPool()
        {
            workCmdPool = new CommandBufferPool(Device.QFGraphics, CommandPoolCreateFlags.ResetCommandBuffer);
        }

        protected void CreateCommandBuffers()
        {
            workCmdPool.Allocate(CommandBufferLevel.Primary, (uint)Swapchain.ImageCount);
        }

        public static CommandBuffer CreateCommandBuffer(CommandBufferLevel level, bool begin = false)
        {
            var cmdBuffer = commandPool.AllocateCommandBuffer(level);

            // If requested, also start recording for the new command buffer
            if (begin)
            {
                cmdBuffer.Begin(CommandBufferUsageFlags.None);
            }

            return cmdBuffer;
        }

        public static void FlushCommandBuffer(CommandBuffer commandBuffer, Queue queue, bool free = true)
        {
            commandBuffer.End();

            using (Fence fence = new Fence(FenceCreateFlags.None))
            {
                queue.Submit(null, PipelineStageFlags.None, commandBuffer, null, fence);
                fence.Wait();
            }

            if (free)
            {
                commandPool.FreeCommandBuffer(commandBuffer);
            }

        }

        public CommandBuffer BeginWorkCommandBuffer()
        {
            workCmdPool[WorkContext].Begin(CommandBufferUsageFlags.OneTimeSubmit);
            return workCmdPool[WorkContext];
        }

        public void EndWorkCommandBuffer(CommandBuffer commandBuffer)
        {
            commandBuffer.End();

            GraphicsQueue.Submit(null, PipelineStageFlags.None, commandBuffer, null);
            GraphicsQueue.WaitIdle();

            commandBuffer.Reset(true);
        }

        public TransientBuffer AllocVertexBuffer(uint count)
        {
            return transientVB.Alloc(count);
        }

        public TransientBuffer AllocIndexBuffer(uint count)
        {
            return transientIB.Alloc(count);
        }
        
        public void WaitIdle()
        {
            device.WaitIdle();
        }

        public void BeginRender()
        {
            Profiler.BeginSample("Acquire");

#if NEW_BACK_BUFF
            var frame = backBuffers.Peek();
            if(frame.presentFence == null)
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

#endif

            uint curImage = currentImage;

#if NEW_BACK_BUFF
            Swapchain.AcquireNextImage(frame.acquireSemaphore, ref currentImage);
            currentBuffer = frame;
            currentBuffer.imageIndex = currentImage;
            backBuffers.Dequeue();
#else
            Swapchain.AcquireNextImage(PresentComplete, ref currentImage);            
#endif

            //Debug.Assert(currentImage == (curImage + 1) % ImageCount);


            Profiler.EndSample();
            
            Profiler.BeginSample("MainSemWait");

            MainSemWait();

            nextImage = (currentImage + 1) % ImageCount;

            Profiler.EndSample();

            transientVB.Flush();
            transientIB.Flush();
        }
      
        public void EndRender()
        {            
            Profiler.BeginSample("Present");

#if NEW_BACK_BUFF
            Swapchain.QueuePresent(GraphicsQueue, currentImage, currentBuffer.renderSemaphore);
            GraphicsQueue.Submit(null, currentBuffer.presentFence);
#else
            Swapchain.QueuePresent(GraphicsQueue, currentImage, RenderComplete);
#endif
            GraphicsQueue.WaitIdle();

#if NEW_BACK_BUFF
            backBuffers.Enqueue(currentBuffer);
#endif

            Profiler.EndSample();

            Profiler.BeginSample("RenderSemPost");

            RenderSemPost();

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

        private int currentContext;
        public int WorkContext => SingleLoop ? 0 : currentContext;
        public int RenderContext => SingleLoop ? 0 : 1 - currentContext;

        private int currentFrame;
        public int CurrentFrame => currentFrame;

        public uint NextImage { get => nextImage; set => nextImage = value; }

        private System.Threading.Semaphore renderSem = new System.Threading.Semaphore(1, 1);
        private System.Threading.Semaphore mainSem = new System.Threading.Semaphore(0, 1);

        List<System.Action> actions = new List<Action>();

        public void Execute(System.Action action)
        {
            actions.Add(action);
        }

        public void Frame()
        {
            Profiler.BeginSample("RenderSemWait");
            WaitRender();
            WakeRender();

            transientVB.Reset();
            transientIB.Reset();

            Profiler.EndSample();
        }

        void SwapContext()
        {
            if(actions.Count > 0)
            {
                foreach(var action in actions)
                {
                    action.Invoke();
                }

                actions.Clear();
            }

            currentFrame++;

            if(!SingleLoop)
            {
                currentContext = 1 - currentContext;
            }

        }

        public void WakeRender()
        {
            SwapContext();
            // release render thread
            MainSemPost();
        }

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
                stats.LogicWait = Stopwatch.GetTimestamp() - curTime;
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

        void WaitRender()
        {
            if (!SingleLoop)
            {
                long curTime = Stopwatch.GetTimestamp();
                bool ok = renderSem.WaitOne();
                stats.RenderWait = Stopwatch.GetTimestamp() - curTime;
            }
        }

        public void Close()
        {
            MainSemWait();
            RenderSemPost();
        }

#endregion

    }

    public struct Stats
    {
        public long LogicWait;
        public long RenderWait;
        public static int drawCall;
        public static int triCount;
        
        public static void Tick(float timeStep)
        {
            drawCall = 0;
            triCount = 0;
        }
    }
}
