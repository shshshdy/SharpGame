using NuklearSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using VulkanCore;

using static NuklearSharp.NuklearNative;

namespace SharpGame
{
    public unsafe partial class ImGUI : Object
    {
        private GraphicsBuffer _vertexBuffer;
        private GraphicsBuffer _indexBuffer;
        private GraphicsBuffer _projMatrixBuffer;


        private ResourceSet resourceSet_;
        private Shader uiShader_;
        private Pipeline pipeline_;
        private Texture fontTex_;

        List<Texture> textures = new List<Texture>();

        public ImGUI()
        {
            Init();

            var resourceLayout = uiShader_.Main.ResourceLayout;
            resourceSet_ = new ResourceSet(resourceLayout);

            resourceSet_.Bind(0, _projMatrixBuffer)
            .Bind(1, fontTex_)
            .UpdateSets();

            this.SubscribeToEvent((ref BeginFrame e) => UpdateGUI());

            this.SubscribeToEvent((EndRenderPass e) => RenderGUI(e.renderPass));
        }

        void CreateGraphicsResource()
        {

            var graphics = Get<Graphics>();
            var cache = Get<ResourceCache>();

            uiShader_ = new Shader(
                "UI",
                new Pass("ImGui.vert.spv", "ImGui.frag.spv")
                {
                    ResourceLayout = new ResourceLayout(
                        new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                        new DescriptorSetLayoutBinding(1, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment)
                    )
                }
            );

            _projMatrixBuffer = GraphicsBuffer.CreateUniform<Matrix>();

            pipeline_ = new Pipeline
            {
                VertexInputState = Pos2dTexColorVertex.Layout,
                DepthTestEnable = false,
                DepthWriteEnable = false,
                CullMode = CullModes.None,
                BlendMode = BlendMode.Alpha,
                DynamicStateCreateInfo = new PipelineDynamicStateCreateInfo(DynamicState.Scissor)
            };

            _vertexBuffer = GraphicsBuffer.CreateDynamic<Pos2dTexColorVertex>(BufferUsages.VertexBuffer, 4046);
            _indexBuffer = GraphicsBuffer.CreateDynamic<ushort>(BufferUsages.IndexBuffer, 4046);

 
        }

        void FontStash(IntPtr Atlas)
        {
        }

        int CreateTextureHandle(int w, int h, IntPtr image)
        {
            var tex = Texture.Create2D(w, h, 4, image);
            textures.Add(tex);

            fontTex_ = tex;
            return textures.Count - 1;
        }

        static void TestWindow(float X, float Y)
        {
            const nk_panel_flags Flags = nk_panel_flags.NK_WINDOW_BORDER | nk_panel_flags.NK_WINDOW_MOVABLE | nk_panel_flags.NK_WINDOW_SCALABLE |
                nk_panel_flags.NK_WINDOW_MINIMIZABLE | nk_panel_flags.NK_WINDOW_SCROLL_AUTO_HIDE;

            ImGUI.Window("Test Window", X, Y, 200, 200, Flags, () =>
            {
                ImGUI.LayoutRowDynamic(35);

                for (int i = 0; i < 5; i++)
                    if (ImGUI.ButtonLabel("Some Button " + i))
                        Console.WriteLine("You pressed button " + i);

                if (ImGUI.ButtonLabel("Exit"))
                    Environment.Exit(0);
            });
        }

        private void UpdateGUI()
        {
            SetDeltaTime(Time.Delta);

            HandleInput();

            TestWindow(100, 100);

           // SendEvent(GUIEvent.Ref);
        }

        

        void RenderGUI(RenderPass renderPass)
        {
            nk_convert_result R = (nk_convert_result)NuklearNative.nk_convert(Ctx, Commands, Vertices, Indices, ConvertCfg);
            if (R != nk_convert_result.NK_CONVERT_SUCCESS)
                throw new Exception(R.ToString());

            NkVertex* VertsPtr = (NkVertex*)Vertices->memory.ptr;
            ushort* IndicesPtr = (ushort*)Indices->memory.ptr;

            if (Ctx->draw_list.cmd_count == 0)
            {
                return;
            }

            if (Ctx->draw_list.vertex_count > _vertexBuffer.Count)
            {
                _vertexBuffer.Dispose();
                _vertexBuffer = GraphicsBuffer.CreateDynamic<Pos2dTexColorVertex>(BufferUsages.VertexBuffer,
                    (int)(1.5f * Ctx->draw_list.vertex_count));
            }

            if (Ctx->draw_list.element_count > _indexBuffer.Count)
            {
                _indexBuffer.Dispose();
                _indexBuffer = GraphicsBuffer.CreateDynamic<ushort>(BufferUsages.IndexBuffer, 
                    (int)(1.5f * Ctx->draw_list.element_count));
            }

            _vertexBuffer.SetData((IntPtr)VertsPtr, 0, (int)Ctx->draw_list.vertex_count * Interop.SizeOf<Pos2dTexColorVertex>());
            _indexBuffer.SetData((IntPtr)IndicesPtr, 0, (int)Ctx->draw_list.element_count * sizeof(ushort));

            var graphics = Get<Graphics>();
            Matrix proj = Matrix.OrthoOffCenterLH(
                     0f,
                     graphics.Width,
                     graphics.Height,
                     0.0f,
                     -1.0f,
                     1.0f, false);

            _projMatrixBuffer.SetData(ref proj);

            renderPass.BindVertexBuffer(_vertexBuffer, 0);
            renderPass.BindIndexBuffer(_indexBuffer, 0, IndexType.UInt16);
                renderPass.BindGraphicsPipeline(pipeline_, uiShader_, resourceSet_);

            uint Offset = 0;

            nk_draw_foreach(Ctx, Commands, (Cmd) =>
            {
                if (Cmd->elem_count == 0)
                    return;

                int xMin = (int)Cmd->clip_rect.x;
                if (xMin < 0)
                {
                    xMin = 0;
                }
                int xMax = (int)(Cmd->clip_rect.x + Cmd->clip_rect.w);
                if (xMax > graphics.Width)
                {
                    xMax = graphics.Width;
                }

                int yMin = (int)Cmd->clip_rect.y;
                if (yMin < 0)
                {
                    yMin = 0;
                }
                int yMax = (int)(Cmd->clip_rect.y + Cmd->clip_rect.h);
                if (yMax > graphics.Height)
                {
                    yMax = graphics.Height;
                }

                renderPass.SetScissor(new Rect2D(xMin, yMin, xMax - xMin, yMax - yMin));
                renderPass.DrawIndexed((int)Cmd->elem_count, 1, (int)Offset, 0, 0);

                Offset += Cmd->elem_count;
            });


            nk_draw_list* list = &Ctx->draw_list;
            if (list != null)
            {
                if (list->buffer != null)
                    NuklearNative.nk_buffer_clear(list->buffer);

                if (list->vertices != null)
                    NuklearNative.nk_buffer_clear(list->vertices);

                if (list->elements != null)
                    NuklearNative.nk_buffer_clear(list->elements);
            }

            NuklearNative.nk_clear(Ctx);
        }

    }
}
