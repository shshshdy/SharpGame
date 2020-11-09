Shader "Basic"
{
	Pass "main"
	{
        CullMode = Back
		FrontFace = CounterClockwise

		@VertexShader
		{
            #include "indirectdraw.vert"
		}
		
		@PixelShader
		{
            #include "indirectdraw.frag"
		}

	}

}
