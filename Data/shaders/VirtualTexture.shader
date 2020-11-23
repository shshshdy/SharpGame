Shader "VirtualTexture"
{
	Pass "main"
	{
        CullMode = Back
		FrontFace = CounterClockwise

		PushConstant lodBias
		{
			StageFlags = Vertex
			Offset = 0
			Size = 4
		}

		@VertexShader
		{
            #include "Natural/sparseresidency.vert"
		}
		
		@PixelShader
		{
            #include "Natural/sparseresidency.frag"
		}

	}

}
