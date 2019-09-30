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

        public static ref bool DrawDebug => ref drawDebug;
        static bool drawDebug;

        public static ref bool DebugDepthTest => ref debugDepthTest;
        static bool debugDepthTest;

        public static ref bool DebugScene => ref debugOctree;
        static bool debugOctree;

        public static bool debugImage = false;
        protected float debugImageHeight = 200.0f;
        List<ImageView> debugImages = new List<ImageView>();

        public DynamicBuffer TransformBuffer { get; }        
        public DynamicBuffer InstanceBuffer { get; }
        public DynamicBuffer MaterialBuffer { get; }

        public static bool EarlyZ { get; set; }

        struct Command_buffer_block
        {
            public CommandBuffer cmd_buffer;
            public Fence submit_fence;
        };

        CommandBufferPool preComputeCmdPool;
        CommandBufferPool computeCmdPool;
        CommandBufferPool graphicsCmdPool;

        Command_buffer_block[] offscreen_cmd_buf_blk = new Command_buffer_block[3];
        Command_buffer_block[] compute_cmd_buf_blk = new Command_buffer_block[3];
        Command_buffer_block[] onscreen_cmd_buf_blk = new Command_buffer_block[3];

        public Renderer()
        {
            this.Subscribe<GUIEvent>(e => OnDebugImage());

            preComputeCmdPool = new CommandBufferPool(Device.QFGraphics, CommandPoolCreateFlags.ResetCommandBuffer);
            computeCmdPool = new CommandBufferPool(Device.QFCompute, CommandPoolCreateFlags.ResetCommandBuffer);
            graphicsCmdPool = new CommandBufferPool(Device.QFGraphics, CommandPoolCreateFlags.ResetCommandBuffer);

            uint size = Graphics.Settings.Validation ? 64 * 1000u : 64 * 1000 * 100u;

            TransformBuffer = new DynamicBuffer(BufferUsageFlags.UniformBuffer, size);
        }

        public void Initialize()
        {
            MainView = CreateRenderView();

            preComputeCmdPool.Allocate(CommandBufferLevel.Primary, 3);
            for(int i = 0; i < 3; i++)
            {
                offscreen_cmd_buf_blk[i].cmd_buffer = preComputeCmdPool.CommandBuffers[i];
                offscreen_cmd_buf_blk[i].submit_fence = new Fence(FenceCreateFlags.None);
            }            

            computeCmdPool.Allocate(CommandBufferLevel.Primary, 3);
            for (int i = 0; i < 3; i++)
            {
                compute_cmd_buf_blk[i].cmd_buffer = computeCmdPool.CommandBuffers[i];
                compute_cmd_buf_blk[i].submit_fence = new Fence(FenceCreateFlags.None);
            }

            graphicsCmdPool.Allocate(CommandBufferLevel.Primary, 3);
            for (int i = 0; i < 3; i++)
            {
                onscreen_cmd_buf_blk[i].cmd_buffer = graphicsCmdPool.CommandBuffers[i];
                onscreen_cmd_buf_blk[i].submit_fence = new Fence(FenceCreateFlags.None);
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

        }

        public void Submit()
        {
            Profiler.BeginSample("Submit");

            Graphics.BeginRender();

            int imageIndex = (int)Graphics.RenderImage;

            {
                var fence = offscreen_cmd_buf_blk[imageIndex].submit_fence;
                //fence.Wait();
                //fence.Reset();

                CommandBuffer cmdBuffer = offscreen_cmd_buf_blk[imageIndex].cmd_buffer;

                cmdBuffer.Begin();

                foreach (var viewport in views)
                {
                    viewport.EarlySubmit(cmdBuffer, imageIndex);
                }

                cmdBuffer.End();
            }

            {
                var fence = compute_cmd_buf_blk[imageIndex].submit_fence;
                //fence.Wait();
                //fence.Reset();

            }

            {
                var fence = onscreen_cmd_buf_blk[imageIndex].submit_fence;
                //fence.Wait();
                //fence.Reset();

                CommandBuffer cmdBuffer = Graphics.RenderCmdBuffer;

                cmdBuffer.Begin();

                foreach (var viewport in views)
                {
                    viewport.Submit(cmdBuffer, imageIndex);
                }

                cmdBuffer.End();

            }

            Graphics.Submit();

            Graphics.EndRender();

            Profiler.EndSample();

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
