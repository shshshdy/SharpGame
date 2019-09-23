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

        PushConstant g_cascadeIndex
        {
            StageFlags = Vertex
            Offset = 0
            Size = 4
        }

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
