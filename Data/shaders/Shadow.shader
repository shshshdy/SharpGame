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
