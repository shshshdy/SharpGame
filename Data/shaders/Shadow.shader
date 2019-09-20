Shader "Shadow"
{
	Properties
    {
        DiffMap  = "White"
    }

	Pass
	{
		CullMode = Front
		FrontFace = CounterClockwise

		@VertexShader
        {
            #define TEX_LOCATION 1
            #include "shadow.vert"
        }

        @PixelShader
        {
            #define TEX_LOCATION 1
            #include "shadow.frag"
        }

		
	}

}
