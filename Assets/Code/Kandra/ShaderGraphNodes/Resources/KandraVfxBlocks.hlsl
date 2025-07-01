#ifndef KANDRA_VFX_BLOCKS_INCLUDED
#define KANDRA_VFX_BLOCKS_INCLUDED

#include "../KandraDebug.hlsl"
#include "../Matrices.hlsl"

#if (defined(UNITY_DOTS_INSTANCING_ENABLED) && defined(KANDRA_SKINNING))
#include "../KandraSkinBuffers.hlsl"
#else
#include "../KandraStructs.hlsl"
uniform StructuredBuffer<CompressedVertex> _GlobalSkinnedVertices;
uniform StructuredBuffer<AdditionalVertexData> _GlobalAdditionalVerticesData;
#endif

float4 PackKandraData(in uint vertexStart, in uint additionalDataStart, in uint maxVertex, in uint trianglesCount)
{
    return float4(asfloat(vertexStart), asfloat(additionalDataStart), asfloat(maxVertex), asfloat(trianglesCount));
}

void UnpackKandraData(in float4 kandraData, out uint vertexStart, out uint additionalDataStart, out uint maxVertex, out uint trianglesCount)
{
    vertexStart = asuint(kandraData.x);
    additionalDataStart = asuint(kandraData.y);
    maxVertex = asuint(kandraData.z);
    trianglesCount = asuint(kandraData.w);
}

void GetTriangleVertices(in uint wrappedTriangle, in StructuredBuffer<uint> indices, out uint vert0, out uint vert1, out uint vert2)
{
    if(wrappedTriangle % 2 == 0)
    {
        const uint sampleIndex = wrappedTriangle / 2 * 3;
        const uint verts01 = indices[sampleIndex];
        const uint verts2X = indices[sampleIndex + 1];
        vert0 = verts01 & 0xFFFF;
        vert1 = verts01 >> 16;
        vert2 = verts2X & 0xFFFF;
    }
    else
    {
        const uint sampleIndex = wrappedTriangle / 2 * 3 + 1;
        const uint vertsX0 = indices[sampleIndex];
        const uint verts12 = indices[sampleIndex + 1];
        vert0 = vertsX0 >> 16;
        vert1 = verts12 & 0xFFFF;
        vert2 = verts12 >> 16;
    }
}

void GetEdgeVertices(in uint wrappedEdge, in StructuredBuffer<uint> indices, out uint2 edges)
{
    const uint wrappedTriangle = wrappedEdge / 3;

    uint vert0;
    uint vert1;
    uint vert2;

    if(wrappedTriangle % 2 == 0)
    {
        const uint sampleIndex = wrappedTriangle / 2 * 3;
        const uint verts01 = indices[sampleIndex];
        const uint verts2X = indices[sampleIndex + 1];
        vert0 = verts01 & 0xFFFF;
        vert1 = verts01 >> 16;
        vert2 = verts2X & 0xFFFF;
    }
    else
    {
        const uint sampleIndex = wrappedTriangle / 2 * 3 + 1;
        const uint vertsX0 = indices[sampleIndex];
        const uint verts12 = indices[sampleIndex + 1];
        vert0 = vertsX0 >> 16;
        vert1 = verts12 & 0xFFFF;
        vert2 = verts12 >> 16;
    }

    const uint edge = wrappedEdge % 3;
    if (edge == 0)
    {
        edges = uint2(vert0, vert1);
    }
    else if(edge == 1)
    {
        edges = uint2(vert1, vert2);
    }
    else
    {
        edges = uint2(vert2, vert0);
    }
}

float2 DecompressUV(in uint uvCompressed)
{
    float2 uv;
    uv.x = f16tof32(uvCompressed & 0x0000FFFF);
    uv.y = f16tof32(uvCompressed >> 16);
    return uv;
}

float3 GetVertexPosition(in float4 kandraData, in uint vertexId)
{
    uint vertexStart;
    uint additionalDataStart;
    uint maxVertex;
    uint trianglesCount;

    UnpackKandraData(kandraData, vertexStart, additionalDataStart, maxVertex, trianglesCount);

    uint wrapIndex = vertexId % maxVertex;
    const CompressedVertex vertex = _GlobalSkinnedVertices[wrapIndex + vertexStart];
    const float3 position = vertex.position;

    return position;
}

float3 GetPositionFromBarycentric(in float4 kandraData, in uint triangleIndex, in float2 coords, in StructuredBuffer<uint> indices)
{
    uint vertexStart;
    uint additionalDataStart;
    uint maxVertex;
    uint trianglesCount;

    UnpackKandraData(kandraData, vertexStart, additionalDataStart, maxVertex, trianglesCount);

    uint vert0;
    uint vert1;
    uint vert2;

    const uint wrappedTriangle = triangleIndex % trianglesCount;

    GetTriangleVertices(wrappedTriangle, indices, vert0, vert1, vert2);

    const float3 v0 = _GlobalSkinnedVertices[vert0 + vertexStart].position;
    const float3 v1 = _GlobalSkinnedVertices[vert1 + vertexStart].position;
    const float3 v2 = _GlobalSkinnedVertices[vert2 + vertexStart].position;

    const float3 fullCoords = float3(coords.x, coords.y, 1.0 - coords.x - coords.y);
    return v0 * fullCoords.x + v1 * fullCoords.y + v2 * fullCoords.z;
}

float3 GetPositionFromEdge(in float4 kandraData, in uint edgeIndex, in float edgeProgress, in StructuredBuffer<uint> indices)
{
    uint vertexStart;
    uint additionalDataStart;
    uint maxVertex;
    uint trianglesCount;

    UnpackKandraData(kandraData, vertexStart, additionalDataStart, maxVertex, trianglesCount);

    uint2 edges;

    const uint wrappedEdge = edgeIndex % (trianglesCount * 3);

    GetEdgeVertices(wrappedEdge, indices, edges);

    const float3 v0 = _GlobalSkinnedVertices[edges.x + vertexStart].position;
    const float3 v1 = _GlobalSkinnedVertices[edges.y + vertexStart].position;

    return lerp(v0, v1, edgeProgress);
}

float2 GetVertexUV(in float4 kandraData, in uint vertexId)
{
    uint vertexStart;
    uint additionalDataStart;
    uint maxVertex;
    uint trianglesCount;

    UnpackKandraData(kandraData, vertexStart, additionalDataStart, maxVertex, trianglesCount);

    uint wrapIndex = vertexId % maxVertex;
    const uint uvCompressed = _GlobalAdditionalVerticesData[wrapIndex + additionalDataStart].uv;
    return DecompressUV(uvCompressed);
}

float2 GetUVFromBarycentric(in float4 kandraData, in uint triangleIndex, in float2 coords, in StructuredBuffer<uint> indices)
{
    uint vertexStart;
    uint additionalDataStart;
    uint maxVertex;
    uint trianglesCount;

    UnpackKandraData(kandraData, vertexStart, additionalDataStart, maxVertex, trianglesCount);

    uint vert0;
    uint vert1;
    uint vert2;

    const uint wrappedTriangle = triangleIndex % trianglesCount;

    GetTriangleVertices(wrappedTriangle, indices, vert0, vert1, vert2);

    const float2 uv0 = DecompressUV(_GlobalAdditionalVerticesData[vert0 + additionalDataStart].uv);
    const float2 uv1 = DecompressUV(_GlobalAdditionalVerticesData[vert1 + additionalDataStart].uv);
    const float2 uv2 = DecompressUV(_GlobalAdditionalVerticesData[vert2 + additionalDataStart].uv);

    const float3 fullCoords = float3(coords.x, coords.y, 1.0 - coords.x - coords.y);
    return uv0 * fullCoords.x + uv1 * fullCoords.y + uv2 * fullCoords.z;
}

float3 GetVertexNormal(in float4 kandraData, in uint vertexId)
{
    uint vertexStart;
    uint additionalDataStart;
    uint maxVertex;
    uint trianglesCount;

    UnpackKandraData(kandraData, vertexStart, additionalDataStart, maxVertex, trianglesCount);

    uint wrapIndex = vertexId % maxVertex;
    const CompressedVertex vertex = _GlobalSkinnedVertices[wrapIndex + vertexStart];

    float3 normal = 0;
    float3 tangent = 0;
    DecodeNormalAndTangent(vertex.normalAndTangent, normal, tangent);

    return normal;
}

float3 GetNormalFromBarycentric(in float4 kandraData, in uint triangleIndex, in float2 coords, in StructuredBuffer<uint> indices)
{
    uint vertexStart;
    uint additionalDataStart;
    uint maxVertex;
    uint trianglesCount;

    UnpackKandraData(kandraData, vertexStart, additionalDataStart, maxVertex, trianglesCount);

    uint vert0;
    uint vert1;
    uint vert2;

    const uint wrappedTriangle = triangleIndex % trianglesCount;

    GetTriangleVertices(wrappedTriangle, indices, vert0, vert1, vert2);

    const uint2 nt0 = _GlobalSkinnedVertices[vert0 + vertexStart].normalAndTangent;
    const uint2 nt1 = _GlobalSkinnedVertices[vert1 + vertexStart].normalAndTangent;
    const uint2 nt2 = _GlobalSkinnedVertices[vert2 + vertexStart].normalAndTangent;

    float3 tanget = 0;
    float3 n0 = 0;
    float3 n1 = 0;
    float3 n2 = 0;

    DecodeNormalAndTangent(nt0, n0, tanget);
    DecodeNormalAndTangent(nt1, n1, tanget);
    DecodeNormalAndTangent(nt2, n2, tanget);

    const float3 fullCoords = float3(coords.x, coords.y, 1.0 - coords.x - coords.y);
    return normalize(n0 * fullCoords.x + n1 * fullCoords.y + n2 * fullCoords.z);
}

float3 GetNormalFromEdge(in float4 kandraData, in uint edgeIndex, in float edgeProgress, in StructuredBuffer<uint> indices)
{
    uint vertexStart;
    uint additionalDataStart;
    uint maxVertex;
    uint trianglesCount;

    UnpackKandraData(kandraData, vertexStart, additionalDataStart, maxVertex, trianglesCount);

    uint2 edges;

    const uint wrappedEdge = edgeIndex % (trianglesCount * 3);

    GetEdgeVertices(wrappedEdge, indices, edges);

    const uint2 nt0 = _GlobalSkinnedVertices[edges.x + vertexStart].normalAndTangent;
    const uint2 nt1 = _GlobalSkinnedVertices[edges.y + vertexStart].normalAndTangent;

    float3 tanget = 0;
    float3 n0 = 0;
    float3 n1 = 0;

    DecodeNormalAndTangent(nt0, n0, tanget);
    DecodeNormalAndTangent(nt1, n1, tanget);

    return normalize(lerp(n0, n1, edgeProgress));
}

#if defined(VFX_USE_POSITION_CURRENT)
void SetVertexPosition(inout VFXAttributes attributes, in float4 kandraData, in uint vertexId)
{
    attributes.position = GetVertexPosition(kandraData, vertexId);
}
#endif

#if defined(VFX_USE_POSITION_CURRENT)
void SetPositionFromBarycentric(inout VFXAttributes attributes, in float4 kandraData, in uint triangleIndex, in float2 coords, in StructuredBuffer<uint> indices)
{
    attributes.position = GetPositionFromBarycentric(kandraData, triangleIndex, coords, indices);
}
#endif

#endif