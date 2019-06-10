﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;


namespace SharpGame
{
    public class FrameGraphScenePass : FrameGraphPass
    {
        private ResourceLayout perFrameResLayout;
        private ResourceSet perFrameSet;

        private ResourceSet perObjectSet;

        public FrameGraphScenePass(string name = "main")
        {
            Name = name;

            perFrameResLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
            };

        }

        protected override void OnBegin(RenderView view)
        {
            if(perFrameSet == null)
            {
                perFrameSet = new ResourceSet(perFrameResLayout, view.ubCameraVS);
            }

            cmdBuffer.SetViewport(ref view.Viewport);
            cmdBuffer.SetScissor(new Rect2D(0, 0, (int)view.Viewport.width, (int)view.Viewport.height));
        }

        protected override void OnDraw(RenderView view)
        {
            foreach (var batch in view.batches)
            {
                DrawBatch(batch, perFrameSet);               
            }
        }
    }


}
