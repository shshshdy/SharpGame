Shader "ClusterDeferred"
{
	Pass
	{
		CullMode = None
		DepthTest = false
		DepthWrite = false

		@VertexShader
        {
            #include "post/fullscreen.vert"
        }

        @PixelShader
        {
            #include "cluster_deferred.frag"
        }

		
	}

}
