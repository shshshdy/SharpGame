Shader "IndirectDraw"
{
	Pass "main"
	{
        CullMode = Back
		FrontFace = CounterClockwise

		@VertexShader
		{
            #include "vegetation/indirectdraw.vert"
		}
		
		@PixelShader
		{
            #include "vegetation/indirectdraw.frag"
		}

	}

}
