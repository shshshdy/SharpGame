Shader "IndirectDraw"
{
	Pass "main"
	{
        CullMode = Back
		FrontFace = CounterClockwise

		@VertexShader
		{
            #include "Environment/indirectdraw.vert"
		}
		
		@PixelShader
		{
            #include "Environment/indirectdraw.frag"
		}

	}

}
