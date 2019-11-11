
#version 450
#ifdef ALPHA_MAP
layout(set = 1, binding = 0) uniform sampler2D AlphaMap;
#else
layout(set = 1, binding = 0) uniform sampler2D DiffMap;
#endif

layout(location = 0) in vec2 inUV;

void main()
{
#ifdef ALPHA_TEST
    #ifdef ALPHA_MAP
    if( texture(AlphaMap, inUV).r < 0.5) {
        discard;
    }
    #else
    float alpha = texture(DiffMap, inUV).a;
    if (alpha < 0.5) {
        discard;
    }
    #endif
#endif
}
