Shader "Brdf"
{
    Pass "SpMap"
    {
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
