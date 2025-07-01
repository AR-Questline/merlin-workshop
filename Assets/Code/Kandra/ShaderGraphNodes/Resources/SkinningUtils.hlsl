#ifndef __SKINNING_UTILS__
#define __SKINNING_UTILS__

#define BYTE_MAX_FLOAT 255.0

struct Bone
{
    float3x4 boneTransform;
};

uint BoneIndex0(uint2 indices)
{
    return indices.x & 0x0000FFFF;
}

uint BoneIndex1(uint2 indices)
{
    return indices.x >> 16;
}

uint BoneIndex2(uint2 indices)
{
    return indices.y & 0x0000FFFF;
}

uint BoneIndex3(uint2 indices)
{
    return indices.y >> 16;
}

float BoneWeight0(uint packedWeights)
{
    return ((packedWeights >> 0) & 0xFF) / BYTE_MAX_FLOAT;
}

float BoneWeight1(uint packedWeights)
{
    return  ((packedWeights >> 8) & 0xFF) / BYTE_MAX_FLOAT;
}

float BoneWeight2(uint packedWeights)
{
    return ((packedWeights >> 16) & 0xFF) / BYTE_MAX_FLOAT;
}

float BoneWeight3(uint packedWeights)
{
    return ((packedWeights >> 24) & 0xFF) / BYTE_MAX_FLOAT;
}

float3x4 mul3x4(float3x4 a, float3x4 b)
{
    float4x4 x = 0.;
    x._m00 = a._m00;
    x._m10 = a._m10;
    x._m20 = a._m20;
    x._m30 = 0.;
    x._m01 = a._m01;
    x._m11 = a._m11;
    x._m21 = a._m21;
    x._m31 = 0.;
    x._m02 = a._m02;
    x._m12 = a._m12;
    x._m22 = a._m22;
    x._m32 = 0.;
    x._m03 = a._m03;
    x._m13 = a._m13;
    x._m23 = a._m23;
    x._m33 = 1.;

    float4x4 y = 0.;
    y._m00 = b._m00;
    y._m10 = b._m10;
    y._m20 = b._m20;
    y._m30 = 0.;
    y._m01 = b._m01;
    y._m11 = b._m11;
    y._m21 = b._m21;
    y._m31 = 0.;
    y._m02 = b._m02;
    y._m12 = b._m12;
    y._m22 = b._m22;
    y._m32 = 0.;
    y._m03 = b._m03;
    y._m13 = b._m13;
    y._m23 = b._m23;
    y._m33 = 1.;

    float4x4 r = mul(x, y);

    float3x4 result = 0.;
    result._m00 = r._m00;
    result._m10 = r._m10;
    result._m20 = r._m20;

    result._m01 = r._m01;
    result._m11 = r._m11;
    result._m21 = r._m21;

    result._m02 = r._m02;
    result._m12 = r._m12;
    result._m22 = r._m22;

    result._m03 = r._m03;
    result._m13 = r._m13;
    result._m23 = r._m23;

    return result;
}

#endif