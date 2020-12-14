﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class SSAOSubpass : FullScreenSubpass
    {
        const int SSAO_KERNEL_SIZE = 32;
        const float SSAO_RADIUS = 2.0f;
        const int SSAO_NOISE_DIM = 4;

        Buffer m_ssaoKernel;
        Texture m_ssaoNoiseTex;
        public SSAOSubpass() : base("post/ssao.frag")
        {
        }

        protected override void CreateResources()
        {
            // Sample kernel
            vec4[] ssaoKernel = new vec4[SSAO_KERNEL_SIZE];
            for (uint i = 0; i < SSAO_KERNEL_SIZE; ++i)
            {
                vec3 sample = new vec3(glm.random() * 2.0f - 1.0f, glm.random() * 2.0f - 1.0f, glm.random());
                sample = glm.normalize(sample);
                sample *= glm.random();
                float scale = i / (float)(SSAO_KERNEL_SIZE);
                scale = glm.mix(0.1f, 1.0f, scale * scale);
                ssaoKernel[i] = new vec4(sample * scale, 0.0f);
            }

            m_ssaoKernel = Buffer.Create(VkBufferUsageFlags.UniformBuffer, ssaoKernel);


            // Random noise
            uint count = SSAO_NOISE_DIM * SSAO_NOISE_DIM;
            Vector<vec4> ssaoNoise = new Vector<vec4>(count);
            for (int i = 0; i < count; i++)
            {
                ssaoNoise.Add(new vec4(glm.random() * 2.0f - 1.0f, glm.random() * 2.0f - 1.0f, 0.0f, 1.0f));
            }

            m_ssaoNoiseTex = Texture.Create2D(SSAO_NOISE_DIM, SSAO_NOISE_DIM, VkFormat.R32G32B32A32SFloat, ssaoNoise.Data);
        }
        

        protected override void BindResources()
        {
            //var rt = this.Renderer.RenderTarget;

            //PipelineResourceSet.SetResourceSet(0, rt[1], rt[3], m_ssaoNoiseTex, m_ssaoKernel, View.ubGlobal);            
        }
    }

    public class SSAOBlurSubpass : FullScreenSubpass
    {
        public SSAOBlurSubpass() : base("post/blur.frag")
        {
        }

        protected override void CreateResources()
        {
        }

        protected override void BindResources()
        {
            //PipelineResourceSet.SetResourceSet(0, )
        }
    }
}
