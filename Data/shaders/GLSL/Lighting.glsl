
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

float GetSpecular(vec3 normal, vec3 eyeVec, vec3 lightDir, float specularPower)
{
    vec3 halfVec = normalize(normalize(eyeVec) + lightDir);  
    return pow(max(dot(normal, halfVec), 0.0), specularPower);
}

float GetIntensity(vec3 color)
{
    return dot(color, vec3(0.299, 0.587, 0.114));
}

#ifdef SHADOW

#if defined(DIRLIGHT) && (!defined(GL_ES) || defined(WEBGL))
    #define NUMCASCADES 4
#else
    #define NUMCASCADES 1
#endif

#ifndef GL_ES
float GetShadow(vec4 shadowPos)
{
    #if defined(SIMPLE_SHADOW)
        // Take one sample
        #ifndef GL3
            float inLight = shadow2DProj(sShadowMap, shadowPos).r;
        #else
            float inLight = textureProj(sShadowMap, shadowPos);
        #endif
        return cShadowIntensity.y + cShadowIntensity.x * inLight;
    #elif defined(PCF_SHADOW)
        // Take four samples and average them
        // Note: in case of sampling a point light cube shadow, we optimize out the w divide as it has already been performed
        #ifndef POINTLIGHT
            vec2 offsets = cShadowMapInvSize * shadowPos.w;
        #else
            vec2 offsets = cShadowMapInvSize;
        #endif
        #ifndef GL3
            return cShadowIntensity.y + cShadowIntensity.x * (shadow2DProj(sShadowMap, shadowPos).r +
                shadow2DProj(sShadowMap, vec4(shadowPos.x + offsets.x, shadowPos.yzw)).r +
                shadow2DProj(sShadowMap, vec4(shadowPos.x, shadowPos.y + offsets.y, shadowPos.zw)).r +
                shadow2DProj(sShadowMap, vec4(shadowPos.xy + offsets.xy, shadowPos.zw)).r);
        #else
            return cShadowIntensity.y + cShadowIntensity.x * (textureProj(sShadowMap, shadowPos) +
                textureProj(sShadowMap, vec4(shadowPos.x + offsets.x, shadowPos.yzw)) +
                textureProj(sShadowMap, vec4(shadowPos.x, shadowPos.y + offsets.y, shadowPos.zw)) +
                textureProj(sShadowMap, vec4(shadowPos.xy + offsets.xy, shadowPos.zw)));
        #endif
    #elif defined(VSM_SHADOW)
        vec2 samples = texture2D(sShadowMap, shadowPos.xy / shadowPos.w).rg; 
        return cShadowIntensity.y + cShadowIntensity.x * Chebyshev(samples, shadowPos.z / shadowPos.w);
    #endif
}
#else
float GetShadow(highp vec4 shadowPos)
{
    #if defined(SIMPLE_SHADOW)
        // Take one sample
        return cShadowIntensity.y + (texture2DProj(sShadowMap, shadowPos).r * shadowPos.w > shadowPos.z ? cShadowIntensity.x : 0.0);
    #elif defined(PCF_SHADOW)
        // Take four samples and average them
        vec2 offsets = cShadowMapInvSize * shadowPos.w;
        vec4 inLight = vec4(
            texture2DProj(sShadowMap, shadowPos).r * shadowPos.w > shadowPos.z,
            texture2DProj(sShadowMap, vec4(shadowPos.x + offsets.x, shadowPos.yzw)).r * shadowPos.w > shadowPos.z,
            texture2DProj(sShadowMap, vec4(shadowPos.x, shadowPos.y + offsets.y, shadowPos.zw)).r * shadowPos.w > shadowPos.z,
            texture2DProj(sShadowMap, vec4(shadowPos.xy + offsets.xy, shadowPos.zw)).r * shadowPos.w > shadowPos.z
        );
        return cShadowIntensity.y + dot(inLight, vec4(cShadowIntensity.x));
    #elif defined(VSM_SHADOW)
        vec2 samples = texture2D(sShadowMap, shadowPos.xy / shadowPos.w).rg; 
        return cShadowIntensity.y + cShadowIntensity.x * Chebyshev(samples, shadowPos.z / shadowPos.w);
    #endif
}
#endif

#ifdef POINTLIGHT
float GetPointShadow(vec3 lightVec)
{
    vec3 axis = textureCube(sFaceSelectCubeMap, lightVec).rgb;
    float depth = abs(dot(lightVec, axis));

    // Expand the maximum component of the light vector to get full 0.0 - 1.0 UV range from the cube map,
    // and to avoid sampling across faces. Some GPU's filter across faces, while others do not, and in this
    // case filtering across faces is wrong
    const vec3 factor = vec3(1.0 / 256.0);
    lightVec += factor * axis * lightVec;

    // Read the 2D UV coordinates, adjust according to shadow map size and add face offset
    vec4 indirectPos = textureCube(sIndirectionCubeMap, lightVec);
    indirectPos.xy *= cShadowCubeAdjust.xy;
    indirectPos.xy += vec2(cShadowCubeAdjust.z + indirectPos.z * 0.5, cShadowCubeAdjust.w + indirectPos.w);

    vec4 shadowPos = vec4(indirectPos.xy, cShadowDepthFade.x + cShadowDepthFade.y / depth, 1.0);
    return GetShadow(shadowPos);
}
#endif

#ifdef DIRLIGHT
float GetDirShadowFade(float inLight, float depth)
{
    return min(inLight + max((depth - cShadowDepthFade.z) * cShadowDepthFade.w, 0.0), 1.0);
}

#if !defined(GL_ES) || defined(WEBGL)
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
#else
float GetDirShadow(const highp vec4 iShadowPos[NUMCASCADES], float depth)
{
    return GetDirShadowFade(GetShadow(iShadowPos[0]), depth);
}
#endif

#ifndef GL_ES
float GetDirShadowDeferred(vec4 projWorldPos, vec3 normal, float depth)
{
    vec4 shadowPos;

    #ifdef NORMALOFFSET
        float cosAngle = clamp(1.0 - dot(normal, cLightDirPS), 0.0, 1.0);
        if (depth < cShadowSplits.x)
            shadowPos = vec4(projWorldPos.xyz + cosAngle * cNormalOffsetScalePS.x * normal, 1.0) * cLightMatricesPS[0];
        else if (depth < cShadowSplits.y)
            shadowPos = vec4(projWorldPos.xyz + cosAngle * cNormalOffsetScalePS.y * normal, 1.0) * cLightMatricesPS[1];
        else if (depth < cShadowSplits.z)
            shadowPos = vec4(projWorldPos.xyz + cosAngle * cNormalOffsetScalePS.z * normal, 1.0) * cLightMatricesPS[2];
        else
            shadowPos = vec4(projWorldPos.xyz + cosAngle * cNormalOffsetScalePS.w * normal, 1.0) * cLightMatricesPS[3];
    #else
        if (depth < cShadowSplits.x)
            shadowPos = projWorldPos * cLightMatricesPS[0];
        else if (depth < cShadowSplits.y)
            shadowPos = projWorldPos * cLightMatricesPS[1];
        else if (depth < cShadowSplits.z)
            shadowPos = projWorldPos * cLightMatricesPS[2];
        else
            shadowPos = projWorldPos * cLightMatricesPS[3];
    #endif

    return GetDirShadowFade(GetShadow(shadowPos), depth);
}
#endif
#endif

#ifndef GL_ES
float GetShadow(const vec4 iShadowPos[NUMCASCADES], float depth)
#else
float GetShadow(const highp vec4 iShadowPos[NUMCASCADES], float depth)
#endif
{
    #if defined(DIRLIGHT)
        return GetDirShadow(iShadowPos, depth);
    #elif defined(SPOTLIGHT)
        return GetShadow(iShadowPos[0]);
    #else
        return GetPointShadow(iShadowPos[0].xyz);
    #endif
}

#ifndef GL_ES
float GetShadowDeferred(vec4 projWorldPos, vec3 normal, float depth)
{
    #ifdef DIRLIGHT
        return GetDirShadowDeferred(projWorldPos, normal, depth);
    #else
        #ifdef NORMALOFFSET
            float cosAngle = clamp(1.0 - dot(normal, normalize(cLightPosPS.xyz - projWorldPos.xyz)), 0.0, 1.0);
            projWorldPos.xyz += cosAngle * cNormalOffsetScalePS.x * normal;
        #endif

        #ifdef SPOTLIGHT
            vec4 shadowPos = projWorldPos * cLightMatricesPS[1];
            return GetShadow(shadowPos);
        #else
            vec3 shadowPos = projWorldPos.xyz - cLightPosPS.xyz;
            return GetPointShadow(shadowPos);
        #endif
    #endif
}
#endif
#endif

