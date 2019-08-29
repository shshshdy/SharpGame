Shader "LitPbr"
{
    Properties = {}

    Pass
    {
        CullMode = None
        FrontFace = CounterClockwise

        @VertexShader
        {
            #include "pbr.vert"
        }

        @PixelShader
        {
            #include "pbr.frag"
        }

    }

}
