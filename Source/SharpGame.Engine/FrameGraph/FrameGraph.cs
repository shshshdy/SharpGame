using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;


namespace SharpGame
{
    using vec2 = Vector2;
    using vec3 = Vector3;
    using vec4 = Vector4;
    using mat4 = Matrix;

    public class FrameGraph : Object
    {
        public RenderTarget[] RenderTargets { get; set; }

        public List<FrameGraphPass> RenderPassList { get; set; } = new List<FrameGraphPass>();

        public FrameGraph()
        {
        }

        public void AddRenderPass(FrameGraphPass renderPass)
        {
            renderPass.FrameGraph = this;
            RenderPassList.Add(renderPass);
        }
      
        public void Draw(RenderView view)
        {
            Profiler.BeginSample("FrameGraph.Draw");
            foreach (var renderPass in RenderPassList)
            {
                renderPass.Draw(view);
            }

            Profiler.EndSample();
        }

        public void Summit(int imageIndex)
        {
            foreach (var renderPass in RenderPassList)
            {
                renderPass.Summit(imageIndex);
            }

        }

    }


    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectVS
    {
        public mat4 Model;
        public vec4 UOffset;
        public vec4 VOffset;
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SkinVS
    {
        fixed float SkinMatrices[16*64];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialPS
    {
        public vec4 MatDiffColor;
        public vec4 MatEmissiveColor;
        public vec4 MatEnvMapColor;
        public vec4 MatSpecColor;
        public float cRoughness;
        public float cMetallic;
    }

}
