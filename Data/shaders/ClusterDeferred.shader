Shader "ClusterDeferred"
{
	Pass
	{
		CullMode = None
		DepthTest = false
		DepthWrite = false

		@VertexShader
        {
            #include "cluster_deferred.vert"
        }

        @PixelShader
        {
            #include "cluster_deferred.frag"
        }

		
	}

}
