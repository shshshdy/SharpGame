Shader "Brdf"
{
    Pass "SpMap"
    {
        PushConstant level
        {
            StageFlags = Compute
            Offset = 0
            Size = 4
        }

        PushConstant roughness
        {
            StageFlags = Compute
            Offset = 4
            Size = 4
        }

        @ComputeShader
        {
            #include "spmap_cs.glsl"
        }

    }

    Pass "IrMap"
    {
        @ComputeShader
        {
            #include "irmap_cs.glsl"
        }
    }

    Pass "BrdfLUT"
    {
        @ComputeShader
        {
            #include "spbrdf_cs.glsl"
        }
    }

    
}
