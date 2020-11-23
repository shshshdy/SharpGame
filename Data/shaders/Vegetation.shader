Shader "IndirectDraw"
{
	Pass "main"
	{
        CullMode = Back
		FrontFace = CounterClockwise

		@VertexShader
		{
            #include "Natural/Vegetation.vert"
		}
		
		@PixelShader
		{
            #include "Natural/Vegetation.frag"
		}

	}

}
