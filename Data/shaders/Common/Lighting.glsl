
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

vec3 DecodeNormal(vec4 normalInput)
{
    #ifdef PACKEDNORMAL
        vec3 normal;
        normal.xy = normalInput.rg * 2.0 - 1.0;
        normal.z = sqrt(max(1.0 - dot(normal.xy, normal.xy), 0.0));
        return normal;
    #else
        return normalize(normalInput.rgb * 2.0 - 1.0);
    #endif
}
