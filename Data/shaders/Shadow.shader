Shader "Shadow"
{
	Properties
    {
        DiffMap  = "White"
    }

	Pass
	{
		CullMode = Back
		FrontFace = Clockwise

        PushConstant model
        {
            StageFlags = Vertex
            Offset = 0
            Size = 68
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
