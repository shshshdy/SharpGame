
mat4 GetSkinMatrix(vec4 blendWeights, ivec4 idx)
{
    return SkinMatrices[idx.x] * blendWeights.x +
        SkinMatrices[idx.y] * blendWeights.y +
        SkinMatrices[idx.z] * blendWeights.z +
        SkinMatrices[idx.w] * blendWeights.w;
}