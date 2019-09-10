
float GetDiffuse(vec3 normal, vec3 worldPos, vec3 lightDir)
{
    #ifdef TRANSLUCENT
        return abs(dot(normal, lightDir));
    #else
        return max(dot(normal, lightDir), 0.0);
    #endif

}

float GetPointLightDiffuse(vec3 normal, vec3 worldPos, vec4 lightPos, out vec3 lightDir)
{
    vec3 lightVec = (lightPos.xyz - worldPos) * lightPos.w;
    float lightDist = length(lightVec);
    lightDir = lightVec / lightDist;
	return max(dot(normal, lightDir), 0.0);
}

float GetAtten(vec3 normal, vec3 worldPos, vec3 lightDir)
{
    return clamp(dot(normal, lightDir), 0.0, 1.0);
}

float GetAttenPoint(vec3 normal, vec3 worldPos, vec4 lightPos, out vec3 lightDir)
{
    vec3 lightVec = (lightPos.xyz - worldPos) * lightPos.w;
    float lightDist = length(lightVec);
    float falloff = pow(clamp(1.0 - pow(lightDist / 1.0, 4.0), 0.0, 1.0), 2.0) * 3.14159265358979323846 / (4.0 * 3.14159265358979323846)*(pow(lightDist, 2.0) + 1.0);
    lightDir = lightVec / lightDist;
    return clamp(dot(normal, lightDir), 0.0, 1.0) * falloff;
}

float GetAttenSpot(vec3 normal, vec3 worldPos, vec4 lightPos, out vec3 lightDir)
{
    vec3 lightVec = (lightPos.xyz - worldPos) * lightPos.w;
    float lightDist = length(lightVec);
    float falloff = pow(clamp(1.0 - pow(lightDist / 1.0, 4.0), 0.0, 1.0), 2.0) / (pow(lightDist, 2.0) + 1.0);

    lightDir = lightVec / lightDist;
    return clamp(dot(normal, lightDir), 0.0, 1.0) * falloff;

}

float BlinnPhong(vec3 normal, vec3 eyeVec, vec3 lightDir, float specularPower)
{
    vec3 halfVec = normalize(normalize(eyeVec) + lightDir);  
    return pow(max(dot(normal, halfVec), 0.0), specularPower);
}

float GetIntensity(vec3 color)
{
    return dot(color, vec3(0.299, 0.587, 0.114));
}

#if defined SHADOW

#define NUMCASCADES 4

float GetShadow(vec4 shadowPos)
{
    #if defined(PCF_SHADOW)
        // Take four samples and average them
        // Note: in case of sampling a point light cube shadow, we optimize out the w divide as it has already been performed
        #ifndef POINTLIGHT
            vec2 offsets = ShadowMapInvSize * shadowPos.w;
        #else
            vec2 offsets = ShadowMapInvSize;
        #endif
      
        return ShadowIntensity.y + ShadowIntensity.x * (textureProj(ShadowMap, shadowPos) +
            textureProj(ShadowMap, vec4(shadowPos.x + offsets.x, shadowPos.yzw)) +
            textureProj(ShadowMap, vec4(shadowPos.x, shadowPos.y + offsets.y, shadowPos.zw)) +
            textureProj(ShadowMap, vec4(shadowPos.xy + offsets.xy, shadowPos.zw)));
       
    #elif defined(VSM_SHADOW)
        vec2 samples = texture2D(ShadowMap, shadowPos.xy / shadowPos.w).rg; 
        return ShadowIntensity.y + ShadowIntensity.x * Chebyshev(samples, shadowPos.z / shadowPos.w);
    #else    
        float inLight = textureProj(ShadowMap, shadowPos);
        return ShadowIntensity.y + ShadowIntensity.x * inLight;
    #endif
}

float GetPointShadow(vec3 lightVec)
{
    vec3 axis = textureCube(FaceSelectCubeMap, lightVec).rgb;
    float depth = abs(dot(lightVec, axis));

    // Expand the maximum component of the light vector to get full 0.0 - 1.0 UV range from the cube map,
    // and to avoid sampling across faces. Some GPU's filter across faces, while others do not, and in this
    // case filtering across faces is wrong
    const vec3 factor = vec3(1.0 / 256.0);
    lightVec += factor * axis * lightVec;

    // Read the 2D UV coordinates, adjust according to shadow map size and add face offset
    vec4 indirectPos = textureCube(IndirectionCubeMap, lightVec);
    indirectPos.xy *= ShadowCubeAdjust.xy;
    indirectPos.xy += vec2(ShadowCubeAdjust.z + indirectPos.z * 0.5, ShadowCubeAdjust.w + indirectPos.w);

    vec4 shadowPos = vec4(indirectPos.xy, ShadowDepthFade.x + ShadowDepthFade.y / depth, 1.0);
    return GetShadow(shadowPos);
}

float GetDirShadowFade(float inLight, float depth)
{
    return min(inLight + max((depth - ShadowDepthFade.z) * ShadowDepthFade.w, 0.0), 1.0);
}

float GetDirShadow(const vec4 iShadowPos[NUMCASCADES], float depth)
{
    vec4 shadowPos;

    if (depth < cShadowSplits.x)
        shadowPos = iShadowPos[0];
    else if (depth < cShadowSplits.y)
        shadowPos = iShadowPos[1];
    else if (depth < cShadowSplits.z)
        shadowPos = iShadowPos[2];
    else
        shadowPos = iShadowPos[3];
        
    return GetDirShadowFade(GetShadow(shadowPos), depth);
}


#endif
