Shader "SkySphere"
{
	Pass "main"
	{
		CullMode = Front
		FrontFace = CounterClockwise
        DepthTest = false
        DepthWrite = false

		@VertexShader
		{
            #include "natural/skysphere.vert"
		}
		
		@PixelShader
		{
            #include "natural/skysphere.frag"
		}
		
	}

}
