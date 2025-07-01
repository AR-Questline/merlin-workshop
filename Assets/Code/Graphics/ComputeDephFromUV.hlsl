void ComputeDepthFromUV_float(float2 uv, float texturesUVInvScale,
                              float2 texBottomLeftUVOffset, float2 texBottomRightUVOffset,
                              float2 texTopLeftUVOffset, float2 texTopRightUVOffset,
                              out float2 texBottomLeftUV, out float texBottomLeftMult,
                              out float2 texBottomRightUV, out float texBottomRightMult,
                              out float2 texTopLeftUV, out float texTopLeftMult,
                              out float2 texTopRightUV, out float texTopRightMult)
{
    {
        float2 uvInTex = uv - texBottomLeftUVOffset;
        texBottomLeftUV = uvInTex * texturesUVInvScale;
        texBottomLeftMult = ((texBottomLeftUV.x >= 0 && texBottomLeftUV.x < 1) ? 1 : 0) * ((texBottomLeftUV.y >= 0 && texBottomLeftUV.y < 1) ? 1 : 0);
    }
    {
        float2 uvInTex = uv - texBottomRightUVOffset;
        texBottomRightUV = uvInTex * texturesUVInvScale;
        texBottomRightMult = ((texBottomRightUV.x >= 0 && texBottomRightUV.x < 1) ? 1 : 0) * ((texBottomRightUV.y >= 0 && texBottomRightUV.y < 1) ? 1 : 0);
    }
    {
        float2 uvInTex = uv - texTopLeftUVOffset;
        texTopLeftUV = uvInTex * texturesUVInvScale;
        texTopLeftMult = ((texTopLeftUV.x >= 0 && texTopLeftUV.x < 1) ? 1 : 0) * ((texTopLeftUV.y >= 0 && texTopLeftUV.y < 1) ? 1 : 0);
    }
    {
        float2 uvInTex = uv - texTopRightUVOffset;
        texTopRightUV = uvInTex * texturesUVInvScale;
        texTopRightMult = ((texTopRightUV.x >= 0 && texTopRightUV.x < 1) ? 1 : 0) * ((texTopRightUV.y >= 0 && texTopRightUV.y < 1) ? 1 : 0);
    }
}
