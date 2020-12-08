Shader "LitPbr"
{
    Properties = {}

    Pass
    {
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
