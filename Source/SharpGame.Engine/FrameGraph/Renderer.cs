//#define SIMPLE_RENDER
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class Renderer : System<Renderer>
    {
        public RenderView MainView { get; private set; }
        private List<RenderView> views = new List<RenderView>();
        public Graphics Graphics => Graphics.Instance;

        public static bool simpleRender = false;
        public static bool drawDebug;
        public static bool debugDepthTest;
        public static bool debugOctree;
        public static bool debugImage = false;

        protected float debugImageHeight = 200.0f;
        List<ImageView> debugImages = new List<ImageView>();

        public DynamicBuffer TransformBuffer { get; }        
        public DynamicBuffer InstanceBuffer { get; }
        public DynamicBuffer MaterialBuffer { get; }

        public static bool EarlyZ { get; set; }

        struct CmdBufferBlock
        {
            public CommandBuffer cmd_buffer;
            public Fence submit_fence;
        };

        CommandBufferPool preRenderCmdPool;
        CommandBufferPool computeCmdPool;
        CommandBufferPool renderCmdPool;

        CmdBufferBlock[] preRenderCmdBlk = new CmdBufferBlock[3];
        CmdBufferBlock[] computeCmdBlk = new CmdBufferBlock[3];
        CmdBufferBlock[] renderCmdBlk = new CmdBufferBlock[3];

        public Renderer()
        {
            this.Subscribe<GUIEvent>(e => OnDebugImage());

            preRenderCmdPool = new CommandBufferPool(Device.QFGraphics, CommandPoolCreateFlags.ResetCommandBuffer);
            computeCmdPool = new CommandBufferPool(Device.QFCompute, CommandPoolCreateFlags.ResetCommandBuffer);
            renderCmdPool = new CommandBufferPool(Device.QFGraphics, CommandPoolCreateFlags.ResetCommandBuffer);

            uint size = Graphics.Settings.Validation ? 64 * 1000u : 64 * 1000 * 100u;

            TransformBuffer = new DynamicBuffer(BufferUsageFlags.UniformBuffer, size);
        }

        public void Initialize()
        {
            MainView = CreateRenderView();

            preRenderCmdPool.Allocate(CommandBufferLevel.Primary, 3);
            for(int i = 0; i < 3; i++)
            {
                preRenderCmdBlk[i].cmd_buffer = preRenderCmdPool.CommandBuffers[i];
                preRenderCmdBlk[i].submit_fence = new Fence(FenceCreateFlags.Signaled);
            }            

            computeCmdPool.Allocate(CommandBufferLevel.Primary, 3);
            for (int i = 0; i < 3; i++)
            {
                computeCmdBlk[i].cmd_buffer = computeCmdPool.CommandBuffers[i];
                computeCmdBlk[i].submit_fence = new Fence(FenceCreateFlags.Signaled);
            }

            renderCmdPool.Allocate(CommandBufferLevel.Primary, 3);
            for (int i = 0; i < 3; i++)
            {
                renderCmdBlk[i].cmd_buffer = renderCmdPool.CommandBuffers[i];
                renderCmdBlk[i].submit_fence = new Fence(FenceCreateFlags.Signaled);
            }
        }

        public RenderView CreateRenderView(Camera camera = null, Scene scene = null, FrameGraph frameGraph = null)
        {
            var view = new RenderView();           
            views.Add(view);
            view.Attach(camera, scene, frameGraph);
            return view;
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe int GetTransform(IntPtr pos, uint count)
        {
            uint sz = count << 6;// (uint)Utilities.SizeOf<mat4>() * count;
            return (int)TransformBuffer.Alloc(sz, pos);
        }

        public void Update()
        {
            var frameInfo = new FrameInfo
            {
                timeStep = Time.Delta,
                frameNumber = Time.FrameNum
            };

            TransformBuffer.Clear();

            mat4 m = mat4.Identity;
            GetTransform(Utilities.AsPointer(ref m), 1);

            this.SendGlobalEvent(new RenderUpdate());

            foreach (var viewport in views)
            {
                viewport.Scene?.RenderUpdate(frameInfo);

                viewport.Update(ref frameInfo);
            }

            if(drawDebug)
            {
                DrawDebugGeometry();
            }

            this.SendGlobalEvent(new PostRenderUpdate());

            TransformBuffer.Flush();
        }

        public void Render()
        {
            this.SendGlobalEvent(new BeginRender());

            foreach (var viewport in views)
            {
                viewport.Render();
            }

            this.SendGlobalEvent(new EndRender());

            //Log.Info("Render frame " + Graphics.CurrentFrame);
        }

        public void Submit()
        {
            Profiler.BeginSample("Submit");

            Graphics.BeginRender();

            int imageIndex = (int)Graphics.RenderImage;

#if SIMPLE_RENDER

            //Log.Info("Summit frame " + Graphics.CurrentFrame);
            /*
            CommandBuffer cmdBuffer = Graphics.RenderCmdBuffer;

            cmdBuffer.Begin();

            foreach (var viewport in views)
            {
                viewport.Submit(cmdBuffer, PassQueue.All, imageIndex);
            }

            cmdBuffer.End();

            Graphics.Submit();*/

#else
           
            if(simpleRender)
            {
                SimpleSubmit();
                Graphics.EndRender();
                Profiler.EndSample();
                return;
            }

            var currentBuffer = Graphics.currentBuffer;

            {
                var fence = preRenderCmdBlk[imageIndex].submit_fence;
                fence.Wait();
                fence.Reset();

                CommandBuffer cmdBuffer = preRenderCmdBlk[imageIndex].cmd_buffer;

                cmdBuffer.Begin();

                foreach (var viewport in views)
                {
                    viewport.FrameGraph.Submit(cmdBuffer, PassQueue.EarlyGraphics, imageIndex);
                }

                cmdBuffer.End();

                Graphics.GraphicsQueue.Submit(currentBuffer.acquireSemaphore, PipelineStageFlags.FragmentShader,
                    cmdBuffer, currentBuffer.preRenderSemaphore, fence);
            }

            {
                var fence = computeCmdBlk[imageIndex].submit_fence;

                fence.Wait();
                fence.Reset();

                foreach (var viewport in views)
                {
                    viewport.FrameGraph.Submit(null, PassQueue.Compute, imageIndex);
                }

                if (Graphics.submitComputeCmdBuffers.Count > 0)
                {
                    var cmdBuffer = Graphics.submitComputeCmdBuffers[0];
                    Graphics.ComputeQueue.Submit(currentBuffer.preRenderSemaphore, PipelineStageFlags.ComputeShader,
                    cmdBuffer, currentBuffer.computeSemaphore, fence);
                    Graphics.submitComputeCmdBuffers.Clear();
                }
                else
                {
                    Graphics.ComputeQueue.Submit(currentBuffer.preRenderSemaphore, PipelineStageFlags.ComputeShader,
                    null, currentBuffer.computeSemaphore, fence);
                }
            }

            {
                var fence = renderCmdBlk[imageIndex].submit_fence;
                fence.Wait();
                fence.Reset();

                CommandBuffer cmdBuffer = renderCmdBlk[imageIndex].cmd_buffer;
                
                cmdBuffer.Begin();

                foreach (var viewport in views)
                {
                    viewport.Submit(cmdBuffer, PassQueue.Graphics, imageIndex);
                }

                cmdBuffer.End();

                Graphics.GraphicsQueue.Submit(currentBuffer.computeSemaphore, PipelineStageFlags.ColorAttachmentOutput,
                    cmdBuffer, currentBuffer.renderSemaphore, fence);
            }
#endif
            Graphics.EndRender();

            Profiler.EndSample();

        }

        void SimpleSubmit()
        {
            int imageIndex = (int)Graphics.RenderImage;
            var currentBuffer = Graphics.currentBuffer;
            var fence = renderCmdBlk[imageIndex].submit_fence;
            fence.Wait();
            fence.Reset();

            CommandBuffer cmdBuffer = renderCmdBlk[imageIndex].cmd_buffer;

            cmdBuffer.Begin();

            foreach (var viewport in views)
            {
                viewport.Submit(cmdBuffer, PassQueue.All, imageIndex);
            }

            cmdBuffer.End();

            Graphics.GraphicsQueue.Submit(currentBuffer.acquireSemaphore, PipelineStageFlags.ColorAttachmentOutput,
                cmdBuffer, currentBuffer.renderSemaphore, fence);

        }

        private void DrawDebugGeometry()
        {
            foreach (var viewport in views)
            {
                if (viewport.Scene == null)
                {
                    continue;
                }

                var debug = viewport.Scene.GetComponent<DebugRenderer>();
                if (debug == null || !debug.IsEnabledEffective())
                {
                    continue;
                }

                var spacePartitioner = viewport.Scene.SpacePartitioner;
                if(debugOctree && spacePartitioner != null)
                {
                    spacePartitioner.DrawDebugGeometry(debug, debugDepthTest);
                }

                foreach (var drawable in viewport.drawables)
                {
                    drawable.DrawDebugGeometry(debug, debugDepthTest);
                }

                foreach (var light in viewport.lights)
                {
                    light.DrawDebugGeometry(debug, debugDepthTest);
                }

            }
        }

        public void DebugImage(bool enable, float height = 200.0f)
        {
            debugImage = enable;
            debugImageHeight = height;
        }

        public void AddDebugImage(params Texture[] textures)
        {
            foreach (var tex in textures)
            {
                ImGUI.Instance.GetOrCreateImGuiBinding(tex);
                debugImages.Add(tex.imageView);
            }
        }

        public void AddDebugImage(params ImageView[] imageViews)
        {
            foreach (var tex in imageViews)
            {
                ImGUI.Instance.GetOrCreateImGuiBinding(tex);
                debugImages.Add(tex);
            }
        }

        void OnDebugImage()
        {
            if (!debugImage || debugImages.Count == 0)
            {
                return;
            }

            var io = ImGui.GetIO();
            {
                vec2 window_pos = new vec2(10, io.DisplaySize.Y - 10);
                vec2 window_pos_pivot = new vec2(0.0f, 1.0f);
                ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
                ImGui.SetNextWindowBgAlpha(0.5f); // Transparent background
            }

            if (ImGui.Begin("DebugImage", ref debugImage, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav))
            {
                foreach (var tex in debugImages)
                {
                    float scale = tex.Width / (float)tex.Height;
                    if (scale > 1)
                    {
                        ImGUI.Image(tex, new vec2(debugImageHeight, debugImageHeight / scale)); ImGui.SameLine();
                    }
                    else
                    {
                        ImGUI.Image(tex, new vec2(scale * debugImageHeight, debugImageHeight)); ImGui.SameLine();
                    }

                }
                ImGui.Spacing();
                ImGui.SliderFloat("Image Height", ref debugImageHeight, 100, 1000);
            }

            ImGui.End();

        }

        protected override void Destroy()
        {
            base.Destroy();

            debugImages.Clear();
        }

    }
}
