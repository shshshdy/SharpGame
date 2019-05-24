using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;


namespace SharpGame
{
    public class ScenePassHandler : PassHandler
    {
        protected Pipeline pipeline;
        private ResourceLayout resourceLayout;
        private ResourceSet resourceSet;

        public ScenePassHandler(string name = "main")
        {
            Name = name;

            Recreate();

            resourceLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
                //new ResourceLayoutBinding(1, DescriptorType.CombinedImageSampler, ShaderStage.Fragment, 1)
            };

            pipeline = new Pipeline
            {
                CullMode = CullMode.None,
                FrontFace = FrontFace.Clockwise,
                DynamicState = new DynamicStateInfo(DynamicState.Viewport, DynamicState.Scissor),
                VertexLayout = VertexPosNormTex.Layout,

                ResourceLayout = new[]{ resourceLayout}
            };

        }

        protected void Recreate()
        {
            var renderer = Renderer.Instance;
           
        }

        protected override void OnBeginDraw(RenderView view)
        {
            if(resourceSet == null)
            {
                resourceSet = new ResourceSet(resourceLayout, view.ubCameraVS);
            }

            //cmdBuffer.SetViewport(ref view.Viewport);
            //cmdBuffer.SetScissor(new Rect2D(0, 0, (int)view.Viewport.width, (int)view.Viewport.height));

        }

        protected override void OnDraw(RenderView view)
        {
            foreach (var drawable in view.drawables)
            {
                for(int i = 0; i < drawable.Batches.Length; i++)
                {
                    SourceBatch batch = drawable.Batches[i];
                    DrawBatch(batch, pipeline, resourceSet);
                }
            }
        }
    }


}
