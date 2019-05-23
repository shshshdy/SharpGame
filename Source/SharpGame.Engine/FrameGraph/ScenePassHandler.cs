using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;


namespace SharpGame
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WorldViewProjection
    {
        public Matrix World;
        public Matrix View;
        public Matrix ViewInv;
        public Matrix ViewProj;
    }

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
                new ResourceLayoutBinding(1, DescriptorType.CombinedImageSampler, ShaderStage.Fragment, 1)
            };

            resourceSet = new ResourceSet(resourceLayout);
            pipeline = new Pipeline();

        }

        protected void Recreate()
        {
            var renderer = Renderer.Instance;
           
        }

        protected override void OnDraw(RenderView view)
        {/*
            if(view.Camera)
            {
                _wvp.World = Matrix.Identity;
                _wvp.View = view.Camera.View;
                Matrix.Invert(ref _wvp.View, out _wvp.ViewInv);
                _wvp.ViewProj = _wvp.View * view.Camera.Projection;

                IntPtr ptr = _uniformBuffer.Map(0, Interop.SizeOf<WorldViewProjection>());
                Interop.Write(ptr, ref _wvp);
                _uniformBuffer.Unmap();
            }
            */

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
