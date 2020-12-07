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
            #define ALPHA_TEST
            #include "shadow.vert"
        }

        @PixelShader
        {
            #define ALPHA_TEST
            #include "shadow.frag"
        }

		
	}

}
