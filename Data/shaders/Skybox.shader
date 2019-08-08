Shader "Skybox"
{
	Properties = {}

	Pass "main"
	{
		CullMode = Front
		FrontFace = CounterClockwise

		ResourceLayout
		{
			ResourceLayoutBinding "CameraVS"
			{
				DescriptorType = UniformBuffer
				StageFlags = Vertex
			}

			ResourceLayoutBinding "ObjectVS"
			{
				DescriptorType = UniformBufferDynamic
				StageFlags = Vertex
			}
		}

		ResourceLayout
		{
			Dynamic = true
			
			ResourceLayoutBinding "samplerCubeMap"
			{
				DescriptorType = CombinedImageSampler
				StageFlags = Fragment
			}
		}
		
		@VertexShader
		{
			#version 450

			#extension GL_ARB_separate_shader_objects : enable
			#extension GL_ARB_shading_language_420pack : enable

            #include "UniformsVS.glsl"

			layout (location = 0) in vec3 inPos;

			layout (location = 0) out vec3 outUVW;

			out gl_PerVertex 
			{
				vec4 gl_Position;
			};

			void main() 
			{
				outUVW = inPos;
				outUVW.y *= -1.0;
				gl_Position = ViewProj * Model *vec4(inPos.xyz, 1.0);
			}


		}
		
		@PixelShader
		{
			#version 450

			#extension GL_ARB_separate_shader_objects : enable
			#extension GL_ARB_shading_language_420pack : enable

			layout (set = 1, binding = 0) uniform samplerCube samplerCubeMap;

			layout (location = 0) in vec3 inUVW;

			layout (location = 0) out vec4 outFragColor;

			void main() 
			{
				outFragColor = texture(samplerCubeMap, inUVW);
			}

		}
		
	}

}
