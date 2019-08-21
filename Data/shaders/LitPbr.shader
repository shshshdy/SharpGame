Shader "LitPbr"
{
    Properties = {}

    Pass
    {
        CullMode = None
        FrontFace = CounterClockwise

        @VertexShader
        {
            #include "pbr_vs.glsl"
        }

        @PixelShader
        {
            #include "pbr_fs.glsl"
        }

    }

}
