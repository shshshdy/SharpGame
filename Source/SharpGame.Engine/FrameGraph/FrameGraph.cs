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

    public class FrameGraph : Resource
    {
        public RenderTarget[] RenderTargets { get; set; }

        public List<PassHandler> RenderPassList { get; set; } = new List<PassHandler>();

        public FrameGraph()
        {
        }

        public void AddRenderPass(PassHandler renderPass)
        {
            renderPass.FrameGraph = this;
            RenderPassList.Add(renderPass);
        }

        public void Draw(RenderView view)
        {
            foreach (var renderPass in RenderPassList)
            {
                renderPass.Draw(view);
            }
            
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
    public struct MaterialVS
    {
        public vec4 UOffset;
        public vec4 VOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectVS
    {
        public mat4 Model;
        //mat3 cBillboardRot;
        //vec4 cSkinMatrices [64*3];
    };


    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialPS
    {
        public vec4 cMatDiffColor;
        public vec4 cMatEmissiveColor;
        public vec4 cMatEnvMapColor;
        public vec4 cMatSpecColor;
        public float cRoughness;
        public float cMetallic;
    }

}
