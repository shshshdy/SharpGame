Shader "Scene"
{
	Properties
    {
        DiffMap  = "White"
    }

	Pass "main"
	{
		CullMode = Back
		FrontFace = CounterClockwise

		@VertexShader
		{		
            #include "scene.vert"
		}
		
		@PixelShader
		{
            #include "scene.frag"
		}
		
	}

}
