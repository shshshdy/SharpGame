Shader "LitParticle"
{
	Properties
	{
		DiffMap = "White"
	}

	Pass "main"
	{
        CullMode = None
		BlendMode = Alpha

        DepthWrite = false

        @VertexShader
        {
            #include "general.vert"
        }

        @PixelShader
        {
            #include "general.frag"
        }
		
	}

}
