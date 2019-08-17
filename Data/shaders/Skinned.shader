Shader "Skinned"
{
	Properties = {}

	Pass "main"
	{
        CullMode = None
				
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

		ResourceLayout PerMaterial
		{
			ResourceLayoutBinding NormalMap
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
			#include "Transform.glsl"
				
			layout(location = 0) in vec3 inPos;
			layout(location = 1) in vec3 inNormal;
			layout(location = 2) in vec2 inUV;
			layout(location = 3) in vec2 inUV1;
			layout(location = 4) in vec2 inUV2;
			layout(location = 5) in vec4 inTagent;
			layout(location = 6) in vec4 inBoneWeights;
			layout(location = 7) in ivec4 inBlendIndices;

			layout(location = 0) out vec4 outWorldPos;
			layout(location = 1) out vec3 outNormal;
			layout(location = 2) out vec2 outUV;
			layout(location = 3) out vec3 outTangent;
			layout(location = 4) out vec3 outBitangent;

			layout(location = 5) out vec3 outViewVec;

			out gl_PerVertex
			{
				vec4 gl_Position;
			};

			void main() 
			{
				outNormal = inNormal;
				outUV = inUV;
				
				mat4 model = GetSkinMatrix(inBoneWeights, inBlendIndices);
				vec4 worldPos = model * vec4(inPos, 1.0);

				gl_Position = ViewProj * worldPos;

				outWorldPos = worldPos;

				outNormal = mat3(model) * inNormal;
				outTangent = normalize(mat3(model) * inTagent.xyz);
				outBitangent = cross(outTangent, outNormal) * inTagent.w;
					
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


			layout(set = 2, binding = 0) uniform sampler2D DiffMap;
			layout(set = 3, binding = 0) uniform sampler2D NormalMap;
			layout(set = 4, binding = 0) uniform sampler2D SpecMap;
			layout(set = 5, binding = 0) uniform sampler2D EmissiveMap;

			layout(location = 0) in vec4 inWorldPos;
			layout(location = 1) in vec3 inNormal;
			layout(location = 2) in vec2 inUV;
			layout(location = 3) in vec3 inTangent;
			layout(location = 4) in vec3 inBitangent;

			layout(location = 5) in vec3 inViewVec;

			layout(location = 0) out vec4 outFragColor;

			vec3 DecodeNormal(vec4 normalInput)
			{
				return normalize(normalInput.rgb * 2.0 - 1.0);
			}

			void main() 
			{
				vec4 diffColor = texture(DiffMap, inUV);

				mat3 tbn = mat3(inTangent, inBitangent, inNormal);

				//vec3 N = normalize(inNormal);
				vec3 N = normalize(tbn * DecodeNormal(texture(NormalMap, inUV)));
				vec3 L = -SunlightDir;

				vec3 diffuse = diffColor.rgb * max(dot(N, L), 0.0);
				vec3 specular = vec3(0.75) * BlinnPhong(N, inViewVec, L, 16.0);
				outFragColor = vec4(diffColor.rgb * AmbientColor.xyz + diffuse + specular, 1.0);
			}


		}
		
	}

}