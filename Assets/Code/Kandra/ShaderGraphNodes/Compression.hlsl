#ifndef __COMPRESSION_INCLUDED__
#define __COMPRESSION_INCLUDED__

#define StereographicScale 1.7777

uint EncodeNormalVectorSpheremap(float3 input)
{
    float p = sqrt(input.z*8+8);

    float2 encodedFloatVector = input.xy / p + 0.5;

    uint2 encodedHalfVector = f32tof16(encodedFloatVector);

    return encodedHalfVector.x | (encodedHalfVector.y << 16);
}

float3 DecodeNormalVectorSpheremap(uint encodedVector)
{
    uint halfX = encodedVector & 0xFFFF;
    uint halfY = encodedVector >> 16;

    float2 encodedFloat = float2(f16tof32(uint2(halfX, halfY)));

    float2 fenc = encodedFloat*4-2;

    float f = dot(fenc.xy, fenc.xy);
    float g = sqrt(1-f/4);
    float3 decoded;
    decoded.xy = fenc.xy*g;
    decoded.z = 1-f/2;
    return normalize(decoded);
}

uint EncodeNormalVectorStereographic(float3 input)
{
    float2 enc = input.xy / (input.z+1);
    enc /= StereographicScale;
    float2 encodedFloatVector = enc*0.5+0.5;

    uint2 encodedHalfVector = f32tof16(encodedFloatVector);

    return encodedHalfVector.x | (encodedHalfVector.y << 16);
}

float3 DecodeNormalVectorStereographic(uint encodedVector)
{
    uint halfX = encodedVector & 0xFFFF;
    uint halfY = encodedVector >> 16;

    float4 encodedFloat = float4(f16tof32(uint2(halfX, halfY)), 0, 0);

    float3 nn = encodedFloat.xyz * float3(2*StereographicScale, 2*StereographicScale, 0) + float3(-StereographicScale, -StereographicScale, 1);
    float g = 2.0 / dot(nn.xyz,nn.xyz);
    float3 n;
    n.xy = g*nn.xy;
    n.z = g-1;
    return n;
}

float2 OctWrap(float2 v)
{
    float2 x;
    x.x = v.x >= 0.0 ? 1.0 : -1.0;
    x.y = v.y >= 0.0 ? 1.0 : -1.0;
    return (1.0 - abs(v.yx)) * x;
}

uint EncodeNormalVectorOctahedron(float3 input)
{
    input /= (abs(input.x) + abs(input.y) + abs(input.z));
    input.xy = input.z >= 0.0 ? input.xy : OctWrap(input.xy);
    input.xy = input.xy * 0.5 + 0.5;

    uint2 encodedHalfVector = f32tof16(input).xy;

    return encodedHalfVector.x | (encodedHalfVector.y << 16);
}

float3 DecodeNormalVectorOctahedron(uint encodedVector)
{
    uint halfX = encodedVector & 0xFFFF;
    uint halfY = encodedVector >> 16;

    float2 f = f16tof32(uint2(halfX, halfY));

    f = f * 2.0 - 1.0;

    // https://twitter.com/Stubbesaurus/status/937994790553227264
    float3 n = float3(f.x, f.y, 1.0 - abs(f.x) - abs(f.y));
    float t = saturate(-n.z);
    n.x += n.x >= 0.0 ? -t : t;
    n.y += n.y >= 0.0 ? -t : t;
    return normalize(n);
}

#endif