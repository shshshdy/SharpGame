﻿Shader "LitPbr"
{
    Properties = {}

    Pass
    {
        CullMode = None
        FrontFace = CounterClockwise

        @VertexShader
        {
            #include "pbr_lighting.vert"
        }

        @PixelShader
        {
            #include "pbr_lighting.frag"
        }

    }

}
