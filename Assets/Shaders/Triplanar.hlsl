
// Triplanar sampling function
float4 SampleTriplanar(Texture2D tex, SamplerState samp,
                       float3 worldPos, float3 worldNormal,
                       float tiling, float4 texST, float4 texelSize)
{
     worldPos *= tiling;

    float2 uv_front = worldPos.xy * texST.xy + texST.zw;
    float2 uv_side = worldPos.zy * texST.xy + texST.zw;
    float2 uv_top = worldPos.xz * texST.xy + texST.zw;

    float4 col_front = SAMPLE_TEXTURE2D(tex, samp, uv_front);
    float4 col_side = SAMPLE_TEXTURE2D(tex, samp, uv_side);
    float4 col_top = SAMPLE_TEXTURE2D(tex, samp, uv_top);

    float3 blendWeights = abs(worldNormal);
    blendWeights /= blendWeights.x + blendWeights.y + blendWeights.z;

    return col_front * blendWeights.z + col_side * blendWeights.x + col_top * blendWeights.y;
}

float4 SampleTriplanar_LimitedRes(Texture2D tex, SamplerState samp,
                                  float3 worldPos, float3 worldNormal,
                                  float tiling, float4 texST, float4 texelSize,
                                  float enableTexRes, float texRes)
{
    worldPos *= tiling;

    float2 uv_front = worldPos.xy * texST.xy + texST.zw;
    float2 uv_side = worldPos.zy * texST.xy + texST.zw;
    float2 uv_top = worldPos.xz * texST.xy + texST.zw;

    if (enableTexRes > 0.5)
    {
        uv_front = floor(uv_front * texRes) / texRes;
        uv_side = floor(uv_side * texRes) / texRes;
        uv_top = floor(uv_top * texRes) / texRes;
    }

    float4 col_front = SAMPLE_TEXTURE2D(tex, samp, uv_front);
    float4 col_side = SAMPLE_TEXTURE2D(tex, samp, uv_side);
    float4 col_top = SAMPLE_TEXTURE2D(tex, samp, uv_top);

    float3 blendWeights = abs(worldNormal);
    blendWeights /= blendWeights.x + blendWeights.y + blendWeights.z;

    return col_front * blendWeights.z + col_side * blendWeights.x + col_top * blendWeights.y;
}