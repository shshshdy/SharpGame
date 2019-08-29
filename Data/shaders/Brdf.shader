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
            #include "spmap.comp"
        }

    }

    Pass "IrMap"
    {
        @ComputeShader
        {
            #include "irmap.comp"
        }
    }

    Pass "BrdfLUT"
    {
        @ComputeShader
        {
            #include "spbrdf.comp"
        }
    }

    
}
