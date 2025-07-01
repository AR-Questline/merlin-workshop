#ifndef SAMPLE_SKIN_BUFFER_INCLUDED
#define SAMPLE_SKIN_BUFFER_INCLUDED

#include "KandraDebug.hlsl"
#include "Matrices.hlsl"
#include "KandraSkinBuffers.hlsl"

uint GetSkinnedVertexIndex(uint vertexId, uint2 instanceData)
{
    return vertexId + instanceData.x;
}

uint GetOriginalVertexIndex(uint vertexId, uint2 instanceData)
{
    #if (defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(KANDRA_SKINNING))
    uint skinnedIndex = vertexId = GetSkinnedVertexIndex(vertexId, instanceData);
    const SkinningVerticesDatum skinningDatum = KANDRA_SKINNING_DATA_BUFFER[skinnedIndex];
    const uint vertexIndex = skinningDatum.vertexIndexAndRendererIndex & 0xFFFF;
    const uint rendererIndex = skinningDatum.vertexIndexAndRendererIndex >> 16;

    const RendererDatum renderDatum = KANDRA_RENDERERS_DATA_BUFFER[rendererIndex];
    const uint meshStart = renderDatum.meshStart;

    return (meshStart + vertexIndex);
    #endif
    return 0;
}

void sampleDeform(uint vertexId, uint2 instanceData, out float3 position, out float3 normal, out float3 tangent)
{
    #if (defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(KANDRA_SKINNING))
    const uint index = GetSkinnedVertexIndex(vertexId, instanceData);
    const CompressedVertex vertex = KANDRA_SKIN_VERTICES_BUFFER[index];
    position = vertex.position;
    DecodeNormalAndTangent(vertex.normalAndTangent, normal, tangent);
    #else
    position = 0;
    normal = 0;
    tangent = 0;
    #endif
}

void SamplePositionWorld(uint vertexId, uint2 instanceData, out float3 position)
{
    #if (defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(KANDRA_SKINNING))
    const uint index = GetSkinnedVertexIndex(vertexId, instanceData.x);
    const CompressedVertex vertex = KANDRA_SKIN_VERTICES_BUFFER[index];
    position = vertex.position;
    #else
    position = 0;
    #endif
}

void SampleNormalAndTangentWorld(uint vertexId, uint2 instanceData, out float3 normal, out float3 tangent)
{
    #if (defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(KANDRA_SKINNING))
    const uint index = GetSkinnedVertexIndex(vertexId, instanceData.x);
    const CompressedVertex vertex = KANDRA_SKIN_VERTICES_BUFFER[index];
    DecodeNormalAndTangent(vertex.normalAndTangent, normal, tangent);
    #else
    normal = 0;
    tangent = 0;
    #endif
}

void SamplePositionObject(uint vertexId, uint2 instanceData, out float3 position)
{
    #if (defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(KANDRA_SKINNING))
    const uint index = GetOriginalVertexIndex(vertexId, instanceData.x);
    const CompressedVertex vertex = KANDRA_ORIGINAL_VERTICES_BUFFER[index];
    position = vertex.position;
    #else
    position = 0;
    #endif
}

void SampleNormalAndTangentObject(uint vertexId, uint2 instanceData, out float3 normal, out float3 tangent)
{
    #if (defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(KANDRA_SKINNING))
    const uint index = GetOriginalVertexIndex(vertexId, instanceData.x);
    const CompressedVertex vertex = KANDRA_ORIGINAL_VERTICES_BUFFER[index];
    DecodeNormalAndTangent(vertex.normalAndTangent, normal, tangent);
    #else
    normal = 0;
    tangent = 0;
    #endif
}

void SamplePreviousPosition(uint vertexId, uint2 instanceData, out float3 previousPosition)
{
    #if (defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(KANDRA_SKINNING))
    const uint index = GetSkinnedVertexIndex(vertexId, instanceData.x);
    previousPosition = KANDRA_PREVIOUS_POSITIONS_BUFFER[index];
    #else
    previousPosition = 0;
    #endif
}

#endif