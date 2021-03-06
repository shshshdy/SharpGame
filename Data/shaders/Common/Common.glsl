
#include "global.glsl"

layout(constant_id = 0) const int TransformMode = 0;
layout(constant_id = 1) const int MATRICES_COUNT = 64;
layout(constant_id = 2) const int ShadingMode = 1;
layout(constant_id = 3) const int LightCount = 2;
layout(constant_id = 4) const int HasNormalMap = 0;

#ifdef SKINNED

layout (binding = 1) uniform SkinVS_dynamic
{
    mat4 SkinMatrices[MATRICES_COUNT];
};

#else

layout (binding = 1) uniform ObjectVS_dynamic
{
    mat4 Model;	
    vec4 UOffset1;
    vec4 VOffset1;
    vec4 UOffset2;
    vec4 VOffset2;
};

#endif
