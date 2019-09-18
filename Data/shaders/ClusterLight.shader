Shader "ClusterLight"
{
    Pass
    {
        @ComputeShader
        {
            #include "calc_light_grids.comp"
        }
    }

	Pass
	{
		@ComputeShader
		{
			#include "calc_grid_offsets.comp"
		}
	}

    Pass
    {
        @ComputeShader
        {
            #include "calc_light_list.comp"
        }
    }
}
