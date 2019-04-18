using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VulkanCore;


namespace SharpGame
{
    public enum Platform
    {
        Android, Win32, MacOS
    }

    public interface IPlatform : IDisposable
    {
        IntPtr WindowHandle { get; }
        IntPtr InstanceHandle { get; }
        int Width { get; }
        int Height { get; }
        Platform Platform { get; }

        Stream Open(string path);
    }

    public abstract class Application : IDisposable
    {
        public IPlatform Host { get; set; }
        public Graphics Context { get; private set; }
        public ResourceCache Content { get; private set; }

        public Application()
        {

        }

        public void Initialize(IPlatform host)
        {
            Host = host;
            Context = new Graphics();
            Content = new ResourceCache(Host, Context, "Content");

            Context.Initialize(Host);

            // Allow concrete samples to initialize their resources.
            InitializePermanent();
            //_initializingPermanent = false;
            InitializeFrame();

            // Record commands for execution by Vulkan.
            RecordCommandBuffers();
        }

        /// <summary>
        /// Allows derived classes to initializes resources the will stay alive for the duration of
        /// the application.
        /// </summary>
        protected virtual void InitializePermanent() { }

        /// <summary>
        /// Allows derived classes to initializes resources that need to be recreated on events such
        /// as window resize.
        /// </summary>
        protected virtual void InitializeFrame() { }

        public void Resize()
        {
            Context.Resize();

            InitializeFrame();

            // Re-record command buffers.
            RecordCommandBuffers();
        }

        public void Tick(Timer timer)
        {
            Update(timer);
            Context.Draw(timer);
        }

        protected virtual void Update(Timer timer) { }

        void RecordCommandBuffers()
        {
            var subresourceRange = new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1);
            for (int i = 0; i < Context.CommandBuffers.Length; i++)
            {
                CommandBuffer cmdBuffer = Context.CommandBuffers[i];
                cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.SimultaneousUse));

                if (Context.PresentQueue != Context.GraphicsQueue)
                {
                    var barrierFromPresentToDraw = new ImageMemoryBarrier(
                        Context.SwapchainImages[i], subresourceRange,
                        Accesses.MemoryRead, Accesses.ColorAttachmentWrite,
                        ImageLayout.Undefined, ImageLayout.PresentSrcKhr,
                        Context.PresentQueue.FamilyIndex, Context.GraphicsQueue.FamilyIndex);

                    cmdBuffer.CmdPipelineBarrier(
                        PipelineStages.ColorAttachmentOutput,
                        PipelineStages.ColorAttachmentOutput,
                        imageMemoryBarriers: new[] { barrierFromPresentToDraw });
                }

                RecordCommandBuffer(cmdBuffer, i);

                if (Context.PresentQueue != Context.GraphicsQueue)
                {
                    var barrierFromDrawToPresent = new ImageMemoryBarrier(
                        Context.SwapchainImages[i], subresourceRange,
                        Accesses.ColorAttachmentWrite, Accesses.MemoryRead,
                        ImageLayout.PresentSrcKhr, ImageLayout.PresentSrcKhr,
                        Context.GraphicsQueue.FamilyIndex, Context.PresentQueue.FamilyIndex);

                    cmdBuffer.CmdPipelineBarrier(
                        PipelineStages.ColorAttachmentOutput,
                        PipelineStages.BottomOfPipe,
                        imageMemoryBarriers: new[] { barrierFromDrawToPresent });
                }

                cmdBuffer.End();
            }
        }


        protected abstract void RecordCommandBuffer(CommandBuffer cmdBuffer, int imageIndex);

        protected T ToDispose<T>(T disposable) => Context.ToDispose(disposable);
        protected T ToDisposeFrame<T>(T disposable) => Context.ToDisposeFrame(disposable);

        public void Dispose()
        {
            Context.Dispose();
        }
    }

}
