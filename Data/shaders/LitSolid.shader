Shader "LitSolid"
{
	Properties = {}

	Pass "main"
	{
				
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
			ResourceLayoutBinding CameraPS
			{
				DescriptorType = UniformBuffer
				StageFlags = Fragment
			}

			ResourceLayoutBinding LightPS
			{
				DescriptorType = UniformBuffer
				StageFlags = Fragment
			}
		}
	
		ResourceLayout PerMaterial
		{
			ResourceLayoutBinding DiffMap
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
			layout (location = 1) in vec3 inNormal;
			layout (location = 2) in vec2 inUV;
			layout (location = 3) in vec4 inColor;
			
			layout (location = 0) out vec4 outWorldPos;
			layout (location = 1) out vec3 outNormal;
			layout (location = 2) out vec2 outUV;
			layout (location = 3) out vec3 outViewVec;

			out gl_PerVertex
			{
				vec4 gl_Position;
			};

			void main() 
			{
				outNormal = inNormal;
				outUV = inUV;
				
				vec4 worldPos = Model * vec4(inPos, 1.0);

				gl_Position = ViewProj * worldPos;

				outWorldPos = worldPos;
				outNormal = mat3(Model) * inNormal;
				outViewVec = CameraPos.xyz - worldPos.xyz;
			}

		}
		
		@PixelShader
		{
			#version 450

			#extension GL_ARB_separate_shader_objects : enable
			#extension GL_ARB_shading_language_420pack : enable

			#include "UniformsPS.glsl"		
			#include "Lighting.glsl"


			layout (set = 2, binding = 0) uniform sampler2D DiffMap;

			layout (location = 0) in vec4 inWorldPos;
			layout (location = 1) in vec3 inNormal;
			layout (location = 2) in vec2 inUV;
			layout (location = 3) in vec3 inViewVec;

			layout (location = 0) out vec4 outFragColor;

			void main() 
			{
				vec4 diffColor = texture(DiffMap, inUV);
				
				vec3 N = normalize(inNormal);
				vec3 L = -SunlightDir;

				vec3 diffuse = diffColor.rgb * max(dot(N, L), 0.0);
				vec3 specular = vec3(0.75) * BlinnPhong(N, inViewVec, L, 16.0);
				outFragColor = vec4(diffColor.rgb * AmbientColor.xyz + diffuse + specular, 1.0);
			}


		}
		
	}

}
