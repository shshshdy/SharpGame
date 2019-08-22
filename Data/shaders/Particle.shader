Shader "Particle"
{
	Pass "main"
	{
		CullMode = None
		FrontFace = CounterClockwise

		@VertexShader
		{
			#version 450
			
            layout(location = 0) in vec2 inPos;
            layout(location = 1) in vec4 inGradientPos;

            layout(location = 0) out vec4 outColor;
            layout(location = 1) out float outGradientPos;

            out gl_PerVertex
            {
                vec4 gl_Position;
                float gl_PointSize;
            };

            void main()
            {
                gl_PointSize = 8.0;
                outColor = vec4(0.035);
                outGradientPos = inGradientPos.x;
                gl_Position = vec4(inPos.xy, 1.0, 1.0);
            }

		}
		
		@PixelShader
		{
            #version 450

            layout(binding = 0) uniform sampler2D samplerColorMap;
            layout(binding = 1) uniform sampler2D samplerGradientRamp;

            layout(location = 0) in vec4 inColor;
            layout(location = 1) in float inGradientPos;

            layout(location = 0) out vec4 outFragColor;

            void main()
            {
                vec3 color = texture(samplerGradientRamp, vec2(inGradientPos, 0.0)).rgb;
                outFragColor.rgb = texture(samplerColorMap, gl_PointCoord).rgb * color;
            }

		}
		
	}

    Pass "compute"
    {
        @ComputeShader
        {
            #version 450

            struct Particle
            {
                vec2 pos;
                vec2 vel;
                vec4 gradientPos;
            };

            // Binding 0 : Position storage buffer
            layout(std140, binding = 0) buffer Pos
            {
                Particle particles[];
            };

            layout(local_size_x = 256) in;

            layout(binding = 1) uniform UBO
            {
                float deltaT;
                float destX;
                float destY;
                int particleCount;
            } ubo;

            vec2 attraction(vec2 pos, vec2 attractPos)
            {
                vec2 delta = attractPos - pos;
                const float damp = 0.5;
                float dDampedDot = dot(delta, delta) + damp;
                float invDist = 1.0f / sqrt(dDampedDot);
                float invDistCubed = invDist * invDist*invDist;
                return delta * invDistCubed * 0.0035;
            }

            vec2 repulsion(vec2 pos, vec2 attractPos)
            {
                vec2 delta = attractPos - pos;
                float targetDistance = sqrt(dot(delta, delta));
                return delta * (1.0 / (targetDistance * targetDistance * targetDistance)) * -0.000035;
            }

            void main()
            {
                // Current SSBO index
                uint index = gl_GlobalInvocationID.x;
                // Don't try to write beyond particle count
                if (index >= ubo.particleCount)
                    return;

                // Read position and velocity
                vec2 vVel = particles[index].vel.xy;
                vec2 vPos = particles[index].pos.xy;

                vec2 destPos = vec2(ubo.destX, ubo.destY);

                vec2 delta = destPos - vPos;
                float targetDistance = sqrt(dot(delta, delta));
                vVel += repulsion(vPos, destPos.xy) * 0.05;

                // Move by velocity
                vPos += vVel * ubo.deltaT;

                // collide with boundary
                if ((vPos.x < -1.0) || (vPos.x > 1.0) || (vPos.y < -1.0) || (vPos.y > 1.0))
                    vVel = (-vVel * 0.1) + attraction(vPos, destPos) * 12;
                else
                    particles[index].pos.xy = vPos;

                // Write back
                particles[index].vel.xy = vVel;
                particles[index].gradientPos.x += 0.02 * ubo.deltaT;
                if (particles[index].gradientPos.x > 1.0)
                    particles[index].gradientPos.x -= 1.0;
            }


        }


    }
}
