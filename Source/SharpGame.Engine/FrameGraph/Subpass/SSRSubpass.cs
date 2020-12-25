using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct PlaneInfo
    {
        public mat4 rotMat;
        public vec4 centerPoint;
        public vec4 size;
    }

    public struct PlaneInfoPack
    {
        public FixedArray4<PlaneInfo> planeInfo;
        public uint numPlanes;
        uint pad00;
        uint pad01;
        uint pad02;
    }

    public struct SSRDepthInfo
    {
        public vec4 depth;
    }

    public class SimpleSSRSubpass : FullScreenSubpass
    {
        Buffer ssrBuffer;
        Buffer planeInfo;

        Texture albedoMap;
        Texture specularMap;
        Texture normalMap;

        PlaneInfoPack planeInfoPack;
        public SimpleSSRSubpass() : base("post/ssr.frag")
        {
            this[0, 0] = "global";
            this[1, 0] = "albedo";
            this[1, 4] = "depth";
        }

        protected override void CreateResources()
        {
            albedoMap = Texture.White;
            specularMap = Texture.White;
            normalMap = Texture.Blue;

            ssrBuffer = Buffer.CreateUniformBuffer<SSRDepthInfo>();
            planeInfo = Buffer.CreateUniformBuffer<PlaneInfoPack>();

            SSRDepthInfo sSRDepthInfo = new SSRDepthInfo
            {
                depth = new vec4(0.0f, 0.3f, 1.0f, 1.0f)
            };
            ssrBuffer.SetData(ref sSRDepthInfo);


            planeInfoPack.numPlanes = 1;

            planeInfoPack.planeInfo.item1.centerPoint = new vec4(0.0f, 0.0f, 0.0f, 0.0f);
            planeInfoPack.planeInfo.item1.size = new vec4(100.0f);
            planeInfoPack.planeInfo.item2.centerPoint = new vec4(4.5f, 0.3f, 1.0f, 0.0f);
            planeInfoPack.planeInfo.item2.size = new vec4(7.0f, 1.5f, 0.0f, 0.0f);
            planeInfoPack.planeInfo.item3.centerPoint = new vec4(4.5f, 0.3f, -2.0f, 0.0f);
            planeInfoPack.planeInfo.item3.size = new vec4(7.0f, 1.5f, 0.0f, 0.0f);

            mat4 basicMat = new mat4();
            basicMat[0] = new vec4(1.0f, 0.0f, 0.0f, 0.0f); //tan
            basicMat[1] = new vec4(0.0f, 1.0f, 0.0f, 0.0f); //bitan
            basicMat[2] = new vec4(0.0f, 0.0f, 1.0f, 0.0f); //normal
            basicMat[3] = new vec4(0.0f, 0.0f, 0.0f, 1.0f);

            planeInfoPack.planeInfo.item1.rotMat = glm.rotate(basicMat, glm.radians(-90.0f), glm.vec3(1.0f, 0.0f, 0.0f));

            planeInfoPack.planeInfo.item2.rotMat = glm.rotate(basicMat, glm.radians(-110.0f), glm.vec3(1.0f, 0.0f, 0.0f));

            planeInfoPack.planeInfo.item3.rotMat = glm.rotate(basicMat, glm.radians(-70.0f), glm.vec3(1.0f, 0.0f, 0.0f));

            planeInfo.SetData(ref planeInfoPack);
        }

        protected override void OnBindResources()
        {
            SetResource(1, 1, albedoMap);
            SetResource(1, 2, specularMap);
            SetResource(1, 3, normalMap);

            SetResource(1, 5, ssrBuffer);
            SetResource(1, 6, planeInfo);
        }
    }

    public class SSR_ProjectionSubpass : FullScreenSubpass
    {
        public SSR_ProjectionSubpass() : base("post/ssr_proj.frag")
        {
            this[0, 0] = "global";
        }

        protected override void CreateResources()
        {
        }

        protected override void OnBindResources()
        {
        }
    }

    public class SSRRSubpass : FullScreenSubpass
    {
        public SSRRSubpass() : base("post/ssrr.frag")
        {
            this[0, 0] = "global";
        }

        protected override void CreateResources()
        {
        }

        protected override void OnBindResources()
        {
        }
    }
}
