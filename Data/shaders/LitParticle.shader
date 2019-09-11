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
