Shader "LitSolid"
{
	Properties
	{
		DiffMap = "White"
	}

	Pass "main"
	{
        CullMode = None
				
		@VertexShader
		{
			#version 450

			#extension GL_ARB_separate_shader_objects : enable
			#extension GL_ARB_shading_language_420pack : enable
			
			#include "UniformsVS.glsl"

			layout(location = 0) in vec3 inPos;
			layout(location = 1) in vec3 inNormal;
			layout(location = 2) in vec3 inTangent;
			layout(location = 3) in vec3 inBitangent;
			layout(location = 4) in vec2 inUV;

			layout(location = 0) out vec4 outWorldPos;
			layout(location = 1) out vec2 outUV;
			layout(location = 2) out mat3 outNormal;

			out gl_PerVertex
			{
				vec4 gl_Position;
			};

			void main() 
			{
				vec4 worldPos = Model * vec4(inPos, 1.0);

				gl_Position = ViewProj * worldPos;
				outWorldPos = worldPos;

				outUV = inUV;
				
				outNormal = mat3(Model) * mat3(inTangent, inBitangent, inNormal);
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
			layout (location = 1) in vec2 inUV;
			layout (location = 2) in mat3 inNormal;

			layout (location = 0) out vec4 outFragColor;

			void main() 
			{
				vec4 diffColor = texture(DiffMap, inUV);
                if(diffColor.a < 0.5) {
                    discard;
                }

				vec3 N = normalize(inNormal[2]);
				vec3 L = -SunlightDir;

                vec3 viewVec = CameraPos.xyz - inWorldPos.xyz;
				vec3 diffuse = diffColor.rgb * max(dot(N, L), 0.0);
				vec3 specular = vec3(0.75) * BlinnPhong(N, viewVec, L, 16.0);
				outFragColor = vec4(diffColor.rgb * AmbientColor.xyz + diffuse + specular, 1.0);
			}


		}
		
	}

    Pass "shadow"
    {

        CullMode = Back
        FrontFace = Clockwise

        PushConstant model
        {
            StageFlags = Vertex
            Offset = 0
            Size = 68
        }

        @VertexShader
        {
            #version 450

            layout(location = 0) in vec3 inPos;
            layout(location = 1) in vec2 inUV;

            // todo: pass via specialization constant
#define SHADOW_MAP_CASCADE_COUNT 4

            layout(push_constant) uniform PushConsts{
                mat4 model;
                uint cascadeIndex;
            } pushConsts;

            layout(binding = 0) uniform UBO{
                mat4[SHADOW_MAP_CASCADE_COUNT] cascadeViewProjMat;
            } ubo;

            layout(location = 0) out vec2 outUV;

            out gl_PerVertex{
                vec4 gl_Position;
            };

            void main()
            {
                outUV = inUV;
                vec4 pos = pushConsts.model * vec4(inPos, 1);
                gl_Position = ubo.cascadeViewProjMat[pushConsts.cascadeIndex] * pos;
            }
        }

        @PixelShader
        {
            #version 450

                layout(set = 1, binding = 0) uniform sampler2D DiffMap;
            layout(location = 0) in vec2 inUV;

            void main()
            {
                float alpha = texture(DiffMap, inUV).a;
                if (alpha < 0.5) {
                    discard;
                }
            }
        }

    }

}
