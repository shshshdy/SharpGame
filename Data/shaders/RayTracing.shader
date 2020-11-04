Shader "RayTracing"
{
	Pass
	{
		CullMode = None
		FrontFace = CounterClockwise

		@VertexShader
		{
			#version 450

			#extension GL_ARB_separate_shader_objects : enable
			#extension GL_ARB_shading_language_420pack : enable

			layout(location = 0) in vec3 inPos;
			layout(location = 1) in vec2 inUV;

			layout(location = 0) out vec2 outUV;

			out gl_PerVertex
			{
				vec4 gl_Position;
			};

			void main()
			{
				outUV = inUV;
				gl_Position = vec4(inPos.xyz, 1.0);
			}

		}

		@PixelShader
		{
			#version 450

			#extension GL_ARB_separate_shader_objects : enable
			#extension GL_ARB_shading_language_420pack : enable

			layout(binding = 0) uniform sampler2D samplerColor;

			layout(location = 0) in vec2 inUV;

			layout(location = 0) out vec4 outFragColor;

			void main()
			{
				outFragColor = texture(samplerColor, vec2(inUV.s, 1.0 - inUV.t));
			}
		}
	}


	Pass "Compute"
	{		

		@ComputeShader
		{
			#include "raytracing/raytracing.comp"
		}

	}

}
