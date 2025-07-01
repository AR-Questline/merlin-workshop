#ifndef __KANDRA_STRUCTS_INCLUDED__
#define __KANDRA_STRUCTS_INCLUDED__

struct Vertex
{
    float3 position;
    float3 normal;
    float3 tangent;
};

struct CompressedVertex
{
    float3 position;
    uint2 normalAndTangent;
};

struct AdditionalVertexData
{
    uint uv;
    float tangentW;
};

struct PackedBlendshapeDatum
{
    uint2 packedPositionDelta;
    uint2 packedFinalNormalAndTangent;
};

struct PackedBonesWeights
{
    uint2 boneIndices;
    uint packedWeights;
};

struct SkinningVerticesDatum
{
    uint vertexIndexAndRendererIndex;
};

struct RendererDatum
{
    uint meshStart;
    uint bonesStart;
};

struct BlendshapesInstanceDatum
{
    uint startAndLengthOfWeights;
};

struct BlendshapeIndexAndWeight
{
    uint index;
    float weight;
};

struct BindPose
{
    float3x4 bindPose;
};

struct SkinningBoneData
{
    uint boneIndex;
    uint bindPoseIndex;
};

#endif