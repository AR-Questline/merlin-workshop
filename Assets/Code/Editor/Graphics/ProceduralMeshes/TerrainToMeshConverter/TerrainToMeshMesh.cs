using System;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Awaken.TG.Editor.Graphics.ProceduralMeshes.TerrainToMeshConverter {
    [CreateAssetMenu(fileName = "TerrainToMeshMesh", menuName = "TG/Terrain/TerrainToMesh Mesh")]
    [BurstCompile]
    public unsafe class TerrainToMeshMesh : ScriptableObject {
        [FormerlySerializedAs("depth")] [SerializeField] int quadTreeDepth;
        [SerializeField] int collisionLod = -1;
        [SerializeField] LODInput[] lods = Array.Empty<LODInput>();

        public int CollisionLod => collisionLod;
        
        public LodOutput[] Create(List<TerrainToMesh.AssetToCreate> toCreate, TerrainData data, in TerrainToMesh.PersistenceInfo persistenceInfo) {
            var size = data.size;
            var quadResolution = 1 << quadTreeDepth;
            var vertResolution = 2 * quadResolution + 1;
            var vertInterval = 1f / (vertResolution - 1);
            var holesResolution = data.holesTexture.width;
            var heights = data.GetInterpolatedHeights(0, 0, vertResolution, vertResolution, vertInterval, vertInterval);
            var holes = data.GetHoles(0, 0, holesResolution, holesResolution);

            var quadCount = GetQuadCountByDepth(quadTreeDepth);
            var quads = new UnsafeArray<Quad>(quadCount, ARAlloc.Temp);
            var deviations = new UnsafeArray<float>(quadCount, ARAlloc.Temp);
            var interpolatedHoles = new Holes(vertResolution, ARAlloc.Temp);
            fixed (bool* holesPtr = &holes[0, 0]) {
                FillInterpolatedHoles(ref interpolatedHoles, holesPtr, holesResolution, vertResolution);
            }
            fixed (float* heightsPtr = &heights[0, 0]) {
                BuildQuadData(ref quads, ref deviations, interpolatedHoles, heightsPtr, vertResolution);
            }

            var horizontalPositionScale = size.x / (vertResolution - 1);
            var horizontalInterpolatedNormalScale = 1f / (vertResolution - 1);
            var outputs = new LodOutput[lods.Length];
            for (int i = 0; i < lods.Length; i++) {
                var lodInput = lods[i];
                var meshes = CreateMesh(quads, deviations, interpolatedHoles, quadResolution, horizontalPositionScale, horizontalInterpolatedNormalScale, data, lodInput);
                for (int j = 0; j < meshes.Length; j++) {
                    persistenceInfo.RequestMeshAssetCreation(toCreate, meshes[j], i, j);
                }
                outputs[i] = new LodOutput(meshes, lodInput.screenRelativeTransitionHeight);
            }

            quads.Dispose();
            deviations.Dispose();
            interpolatedHoles.Dispose();

            return outputs;
        }

        static uint GetQuadCountByDepth(int depth) {
            // 1 + 4 + 4^2 + 4^3 + ... + 4^depth
            return (uint)(((1ul << (2 * depth + 2)) - 1) / 3);
        }
        
        [BurstCompile]
        static void FillInterpolatedHoles(ref Holes interpolatedHoles, bool* holes, int holesResolution, int vertResolution) {
            for (int x = 0; x < vertResolution; x++) {
                for (int z = 0; z < vertResolution; z++) {
                    interpolatedHoles[x, z] = GetInterpolatedHole(x, z);
                }
            }

            bool GetInterpolatedHole(int xVert, int zVert) {
                float xPercent = (float)xVert / (vertResolution - 1);
                float zPercent = (float)zVert / (vertResolution - 1);
                    
                float xHoleSmooth = xPercent * (holesResolution - 1);
                float zHoleSmooth = zPercent * (holesResolution - 1);

                int xHoleMin = (int) math.floor(xHoleSmooth);
                int zHoleMin = (int) math.floor(zHoleSmooth);
                int xHoleMax = (int) math.ceil(xHoleSmooth);
                int zHoleMax = (int) math.ceil(zHoleSmooth);
                
                if (xHoleMin == xHoleMax) {
                    if (zHoleMin == zHoleMax) {
                        return Hole(xHoleMin, zHoleMin);
                    }
                    return zHoleSmooth - zHoleMin < 0.5f 
                        ? Hole(xHoleMin, zHoleMin) 
                        : Hole(xHoleMin, zHoleMax);
                }
                if (zHoleMin == zHoleMax) {
                    return xHoleSmooth - xHoleMin < 0.5f 
                        ? Hole(xHoleMin, zHoleMin) 
                        : Hole(xHoleMax, zHoleMin);
                }
                
                float w11 = (xHoleMax - xHoleSmooth) * (zHoleMax - zHoleSmooth);
                float w21 = (xHoleSmooth - xHoleMin) * (zHoleMax - zHoleSmooth);
                float w12 = (xHoleMax - xHoleSmooth) * (zHoleSmooth - zHoleMin);
                float w22 = (xHoleSmooth - xHoleMin) * (zHoleSmooth - zHoleMin);
                
                float h11 = Hole(xHoleMin, zHoleMin) ? 1 : 0;
                float h21 = Hole(xHoleMax, zHoleMin) ? 1 : 0;
                float h12 = Hole(xHoleMin, zHoleMax) ? 1 : 0;
                float h22 = Hole(xHoleMax, zHoleMax) ? 1 : 0;
                
                return w11 * h11 + w21 * h21 + w12 * h12 + w22 * h22 > 0.5f;
            }

            bool Hole(int x, int z) {
                return holes[x * holesResolution + z];
            }
        }

        [BurstCompile]
        static void BuildQuadData(ref UnsafeArray<Quad> quads, ref UnsafeArray<float> deviations, in Holes holes, float* heights, int vertResolution) {
            quads[0] = new Quad(vertResolution, 0, 0);
            for (var i = 0u; i * 4 + 1 < quads.Length; i++) {
                quads[i].Split(out var nw, out var ne, out var sw, out var se);
                quads[i * 4 + 1] = nw;
                quads[i * 4 + 2] = ne;
                quads[i * 4 + 3] = sw;
                quads[i * 4 + 4] = se;
            }

            for (var i = 0u; i < deviations.Length; i++) {
                deviations[i] = quads[i].Deviation(heights, holes, vertResolution);
            }
        }

        static Mesh[] CreateMesh(in UnsafeArray<Quad> quads, in UnsafeArray<float> deviations, in Holes holes, int quadResolution, float horizontalVerticalScale, float interpolatedScale, TerrainData data, in LODInput lodInput) {
            var quadCount = GetQuadCountByDepth(lodInput.maxDepth);
            var rootQuadStart = GetQuadCountByDepth(lodInput.finalSubdivision - 1);
            var rootQuadEnd = GetQuadCountByDepth(lodInput.finalSubdivision);

            var count = rootQuadEnd - rootQuadStart;
            var meshes = new Mesh[count];
            for (var i = rootQuadStart; i < rootQuadEnd; i++) {
                var meshData = new MeshData(quadResolution);
                FillMeshData(quads, deviations, holes, quadCount, lodInput.deviation, i, ref meshData);
                meshes[i - rootQuadStart] = meshData.CreateMesh(data, horizontalVerticalScale, interpolatedScale);
                meshData.Dispose();
            }

            return meshes;
        }

        [BurstCompile]
        static void FillMeshData(in UnsafeArray<Quad> quads, in UnsafeArray<float> deviations, in Holes holes, uint quadCount, float maxDeviation, uint quadIndex, ref MeshData meshData) {
            FillMeshWithQuad(quads, deviations, holes, quadCount, maxDeviation, quadIndex, ref meshData, out var edges);
            FillEdgeWest(edges, quads[quadIndex], holes, ref meshData, 2);
            FillEdgeEast(edges, quads[quadIndex], holes, ref meshData, 2);
            FillEdgeSouth(edges, quads[quadIndex], holes, ref meshData, 2);
            FillEdgeNorth(edges, quads[quadIndex], holes, ref meshData, 2);
            edges.Dispose();
        }
        
        [BurstCompile]
        static void FillMeshWithQuad(in UnsafeArray<Quad> quads, in UnsafeArray<float> deviations, in Holes holes, uint quadCount, float maxDeviation, uint quadIndex, ref MeshData mesh, out QuadEdges edges) {
            if (deviations[quadIndex] < maxDeviation || quadIndex * 4 + 1 >= quadCount) {
                FillMeshWithQuadLeaf(quads[quadIndex], holes, ref mesh, out edges);
                return;
            }
                    
            FillMeshWithQuad(quads, deviations, holes, quadCount, maxDeviation, quadIndex * 4 + 1, ref mesh, out var nw);
            FillMeshWithQuad(quads, deviations, holes, quadCount, maxDeviation, quadIndex * 4 + 2, ref mesh, out var ne);
            FillMeshWithQuad(quads, deviations, holes, quadCount, maxDeviation, quadIndex * 4 + 3, ref mesh, out var sw);
            FillMeshWithQuad(quads, deviations, holes, quadCount, maxDeviation, quadIndex * 4 + 4, ref mesh, out var se);
            MergeQuadsInMesh(nw, ne, sw, se, ref mesh, out edges);
                    
            nw.Dispose();
            ne.Dispose();
            sw.Dispose();
            se.Dispose();
        }
        
        [BurstCompile]
        static void FillMeshWithQuadLeaf(in Quad quad, in Holes holes, ref MeshData mesh, out QuadEdges edges) {
            int halfSize = quad.size / 2;
            
            bool hasVertex0 = holes[quad.xStart, quad.zStart];
            bool hasVertex1 = holes[quad.xStart + quad.size - 1, quad.zStart];
            bool hasVertex2 = holes[quad.xStart, quad.zStart + quad.size - 1];
            bool hasVertex3 = holes[quad.xStart + quad.size - 1, quad.zStart + quad.size - 1];
            bool hasVertex4 = holes[quad.xStart + halfSize, quad.zStart + halfSize];
            
            var vertex0 = hasVertex0 ? mesh.GetOrAddVertex(quad.xStart, quad.zStart) : -1;
            var vertex1 = hasVertex1 ? mesh.GetOrAddVertex(quad.xStart + quad.size - 1, quad.zStart) : -1;
            var vertex2 = hasVertex2 ? mesh.GetOrAddVertex(quad.xStart, quad.zStart + quad.size - 1) : -1;
            var vertex3 = hasVertex3 ? mesh.GetOrAddVertex(quad.xStart + quad.size - 1, quad.zStart + quad.size - 1) : -1;
            var vertex4 = hasVertex4 ? mesh.GetOrAddVertex(quad.xStart + halfSize, quad.zStart + halfSize) : -1;

            edges = new QuadEdges();
            
            if (hasVertex0 & hasVertex1 & hasVertex4) {
                var triangle = mesh.AddTriangle(vertex0, vertex1, vertex4);
                edges.sTriangles = new UnsafeArray<int>(1u, ARAlloc.Temp) { [0] = triangle };
            } else {
                edges.sTriangles = new UnsafeArray<int>(0u, ARAlloc.Temp);
            }
            
            if (hasVertex1 & hasVertex3 & hasVertex4) {
                var triangle = mesh.AddTriangle(vertex1, vertex3, vertex4);
                edges.eTriangles = new UnsafeArray<int>(1u, ARAlloc.Temp) { [0] = triangle };
            } else {
                edges.eTriangles = new UnsafeArray<int>(0u, ARAlloc.Temp);
            }
            
            if (hasVertex3 & hasVertex2 & hasVertex4) {
                var triangle = mesh.AddTriangle(vertex3, vertex2, vertex4);
                edges.nTriangles = new UnsafeArray<int>(1u, ARAlloc.Temp) { [0] = triangle };
            } else {
                edges.nTriangles = new UnsafeArray<int>(0u, ARAlloc.Temp);
            }
            
            if (hasVertex2 & hasVertex0 & hasVertex4) {
                var triangle = mesh.AddTriangle(vertex2, vertex0, vertex4);
                edges.wTriangles = new UnsafeArray<int>(1u, ARAlloc.Temp) { [0] = triangle };
            } else {
                edges.wTriangles = new UnsafeArray<int>(0u, ARAlloc.Temp);
            }
        }
        
        [BurstCompile]
        static void MergeQuadsInMesh(in QuadEdges nw, in QuadEdges ne, in QuadEdges sw, in QuadEdges se, ref MeshData mesh, out QuadEdges edges) {
            var nTriangles = ConcatTris(nw.nTriangles, ne.nTriangles);
            var sTriangles = ConcatTris(sw.sTriangles, se.sTriangles);
            var eTriangles = ConcatTris(se.eTriangles, ne.eTriangles);
            var wTriangles = ConcatTris(sw.wTriangles, nw.wTriangles);

            MergeEdges(nw.sTriangles, sw.nTriangles, 0, ref mesh);
            MergeEdges(ne.sTriangles, se.nTriangles, 0, ref mesh);
            MergeEdges(sw.eTriangles, se.wTriangles, 1, ref mesh);
            MergeEdges(nw.eTriangles, ne.wTriangles, 1, ref mesh);
             
            edges = new QuadEdges {
                nTriangles = nTriangles,
                sTriangles = sTriangles,
                eTriangles = eTriangles,
                wTriangles = wTriangles
            };
            
            static UnsafeArray<int> ConcatTris(in UnsafeArray<int> first, in UnsafeArray<int> second) {
                var tris = new UnsafeArray<int>(first.Length + second.Length, ARAlloc.Temp);
                UnsafeUtility.MemCpy(tris.Ptr, first.Ptr, first.Length * sizeof(int));
                UnsafeUtility.MemCpy(tris.Ptr + first.Length, second.Ptr, second.Length * sizeof(int));
                return tris;
            }

            static void MergeEdges(in UnsafeArray<int> edge0, in UnsafeArray<int> edge1, int channel, ref MeshData mesh) {
                var index0 = 0u;
                var index1 = 0u;
                
                while ((index0 < edge0.Length) & (index1 < edge1.Length)) {
                    ref var triangle0 = ref mesh.GetTriangle(edge0[index0]);
                    ref var triangle1 = ref mesh.GetTriangle(edge1[index1]);
                    ref readonly var vertex0 = ref mesh.GetSpatialVertexIndex(triangle0.v1);
                    ref readonly var vertex1 = ref mesh.GetSpatialVertexIndex(triangle1.v0);
                    var coord0 = vertex0[channel];
                    var coord1 = vertex1[channel];
                    if (coord0 == coord1) {
                        index0++;
                        index1++;
                    } else if (coord0 < coord1) {
                        mesh.AddTriangle(triangle0.v1, triangle1.v1, triangle1.v2);
                        triangle1.v1 = triangle0.v1; // shrink existing triangle
                        index0++;
                    } else {
                        mesh.AddTriangle(triangle0.v0, triangle1.v0, triangle0.v2);
                        triangle0.v0 = triangle1.v0; // shrink existing triangle
                        index1++;
                    }
                }
            }
        }

        [BurstCompile]
        static void FillEdgeNorth(in QuadEdges edges, in Quad quad, in Holes holes, ref MeshData mesh, int interval) {
            FillEdge(edges.nTriangles, holes, ref mesh, 0, false, quad.xStart, interval, quad.zStart + quad.size - 1);
        }
        
        [BurstCompile]
        static void FillEdgeSouth(in QuadEdges edges, in Quad quad, in Holes holes, ref MeshData mesh, int interval) {
            FillEdge(edges.sTriangles, holes, ref mesh, 0, true, quad.xStart, interval, quad.zStart);
        }
        
        [BurstCompile]
        static void FillEdgeEast(in QuadEdges edges, in Quad quad, in Holes holes, ref MeshData mesh, int interval) {
            FillEdge(edges.eTriangles, holes, ref mesh, 1, true, quad.zStart, interval, quad.xStart + quad.size - 1);
        }
        
        [BurstCompile]
        static void FillEdgeWest(in QuadEdges edges, in Quad quad, in Holes holes, ref MeshData mesh, int interval) {
            FillEdge(edges.wTriangles, holes, ref mesh, 1, false, quad.zStart, interval, quad.xStart);
        }

        [BurstCompile]
        static void FillEdge(in UnsafeArray<int> edge, in Holes holes, ref MeshData mesh, int channel, bool direction, int from, int interval, int otherCoord) {
            var index0 = 0u;

            var coord1 = from + interval;
            while (index0 < edge.Length) {
                ref var triangle0 = ref mesh.GetTriangle(edge[index0]);
                ref readonly var vertex0 = ref mesh.GetSpatialVertexIndex(direction ? triangle0.v1 : triangle0.v0);
                var coord0 = vertex0[channel];
                if (coord0 == coord1) {
                    index0++;
                    coord1 += interval;
                } else if (coord0 > coord1) {
                    bool hasVertex = channel == 0 
                        ? holes[coord1, otherCoord] 
                        : holes[otherCoord, coord1];
                    if (hasVertex) {
                        var vertexToAdd = channel == 0
                            ? mesh.GetOrAddVertex(coord1, otherCoord)
                            : mesh.GetOrAddVertex(otherCoord, coord1);
                        if (direction) {
                            mesh.AddTriangle(triangle0.v0, vertexToAdd, triangle0.v2);
                            triangle0.v0 = vertexToAdd; // shrink existing triangle
                        } else {
                            mesh.AddTriangle(vertexToAdd, triangle0.v1, triangle0.v2);
                            triangle0.v1 = vertexToAdd; // shrink existing triangle
                        }
                    }
                    coord1 += interval;
                } else {
                    index0++;
                }
            }
        }
        
        public readonly struct LodOutput {
            public readonly Mesh[] meshes;
            public readonly float screenRelativeTransitionHeight;
            
            public LodOutput(Mesh[] meshes, float screenRelativeTransitionHeight) {
                this.meshes = meshes;
                this.screenRelativeTransitionHeight = screenRelativeTransitionHeight;
            }
        }

        [Serializable]
        struct LODInput {
            public int maxDepth;
            public float deviation;
            public float screenRelativeTransitionHeight;
            public int finalSubdivision;
        }
        
        struct Quad {
            public int size;
            public int xStart;
            public int zStart;

            public Quad(int size, int xStart, int zStart) {
                this.size = size;
                this.xStart = xStart;
                this.zStart = zStart;
            }

            public float Deviation(float* heights, in Holes holes, int vertResolution) {
                int halfSize = size / 2;

                float sw = heights[Index(xStart, zStart, vertResolution)];
                float se = heights[Index(xStart + size - 1, zStart, vertResolution)];
                float nw = heights[Index(xStart, zStart + size - 1, vertResolution)];
                float ne = heights[Index(xStart + size - 1, zStart + size - 1, vertResolution)];
                float c = heights[Index(xStart + halfSize, zStart + halfSize, vertResolution)];

                float sSlopeX = (se - sw) / size;
                float sSlopeZ = (c - (se + sw) / 2) / halfSize;
                float eSlopeX = ((ne + se) / 2 - c) / halfSize;
                float eSlopeZ = (ne - se) / size;
                float nSlopeX = (ne - nw) / size;
                float nSlopeZ = ((ne + nw) / 2 - c) / halfSize;
                float wSlopeX = (c - (nw + sw) / 2) / halfSize;
                float wSlopeZ = (nw - sw) / size;

                bool holeState = holes[xStart, zStart];
                float deviation = 0;
                for (int x = 0; x < size; x++) {
                    for (int z = 0; z < size; z++) {
                        if (holeState != holes[xStart + x, zStart + z]) {
                            return float.MaxValue;
                        }
                        bool isSE = x < z;
                        bool isSW = z < size - x;
                        float terrainHeight = heights[Index(xStart + x, zStart + z, vertResolution)];
                        float triangleHeight = (isSE, isSW) switch {
                            (true, true) => sw + sSlopeX * x + sSlopeZ * z,
                            (true, false) => c + eSlopeX * (x - halfSize) + eSlopeZ * (z - halfSize),
                            (false, true) => sw + wSlopeX * x + wSlopeZ * z,
                            (false, false) => c + nSlopeX * (x - halfSize) + nSlopeZ * (z - halfSize)
                        };
                        deviation += math.square(terrainHeight - triangleHeight);
                    }
                }
                
                return deviation;
            }
            
            public void Split(out Quad nw, out Quad ne, out Quad sw, out Quad se) {
                int halfSize = size / 2;
                nw = new Quad(halfSize + 1, xStart, zStart + halfSize);
                ne = new Quad(halfSize + 1, xStart + halfSize, zStart + halfSize);
                sw = new Quad(halfSize + 1, xStart, zStart);
                se = new Quad(halfSize + 1, xStart + halfSize, zStart);
            }
            
            int Index(int x, int z, int vertResolution) {
                return x * vertResolution + z;
            }
        }

        struct QuadEdges {
            public UnsafeArray<int> nTriangles;
            public UnsafeArray<int> sTriangles;
            public UnsafeArray<int> eTriangles;
            public UnsafeArray<int> wTriangles;

            public void Dispose() {
                nTriangles.Dispose();
                sTriangles.Dispose();
                eTriangles.Dispose();
                wTriangles.Dispose();
            }
        }

        struct VertexIndex {
            public int x;
            public int y;

            public VertexIndex(int x, int y) {
                this.x = x;
                this.y = y;
            }

            public int this[int channel] => channel == 0 ? x : y;
        }
        
        struct VertexData {
            public VertexIndex spatialIndex;
            public int meshIndex;

            public VertexData(VertexIndex spatialIndex) : this() {
                this.spatialIndex = spatialIndex;
                meshIndex = -1;
            }
        }
        
        struct Triangle {
            public int v0;
            public int v1;
            public int v2;
        }
        
        ref struct MeshData {
            int _spatialVertexResolution;
            UnsafeArray<VertexData> _spatialVertices;
            NativeList<VertexIndex> _meshVertices;
            
            NativeList<Triangle> _triangles;

            public MeshData(int quadResolution) {
                _spatialVertexResolution = quadResolution * 2 + 1;
                _spatialVertices = new UnsafeArray<VertexData>((uint)(_spatialVertexResolution * _spatialVertexResolution), ARAlloc.TempJob);
                _meshVertices = new NativeList<VertexIndex>(_spatialVertexResolution * _spatialVertexResolution, ARAlloc.TempJob);
                _triangles = new NativeList<Triangle>(quadResolution * quadResolution * 4, ARAlloc.TempJob);

                for (int x = 0; x < _spatialVertexResolution; x++) {
                    for (int y = 0; y < _spatialVertexResolution; y++) {
                        var index = new VertexIndex(x, y);
                        GetVertex(index) = new VertexData(index);
                    }
                }
            }

            public Mesh CreateMesh(TerrainData data, float horizontalPositionScale, float interpolatedScale) {
                var vertices = new NativeArray<Vector3>(_meshVertices.Length, ARAlloc.TempJob);
                var normals = new NativeArray<Vector3>(_meshVertices.Length, ARAlloc.TempJob);
                var triangles = new NativeArray<int>(_triangles.Length * 3, ARAlloc.TempJob);

                for (int i = 0; i < _meshVertices.Length; i++) {
                    ref readonly var vertex = ref GetSpatialVertexIndex(i);
                    float interpolatedX = DiscreteToInterpolated(vertex.x, interpolatedScale);
                    float interpolatedY = DiscreteToInterpolated(vertex.y, interpolatedScale);
                    var height = data.GetInterpolatedHeight(interpolatedY, interpolatedX);
                    vertices[i] = new Vector3(vertex.y * horizontalPositionScale, height, vertex.x * horizontalPositionScale);
                    normals[i] = data.GetInterpolatedNormal(interpolatedY, interpolatedX);
                }
                for (int i = 0; i < _triangles.Length; i++) {
                    ref readonly var triangle = ref GetTriangle(i);
                    triangles[i * 3] = triangle.v0;
                    triangles[i * 3 + 1] = triangle.v1;
                    triangles[i * 3 + 2] = triangle.v2;
                }

                var mesh = new Mesh();
                mesh.indexFormat = vertices.Length > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetIndices(triangles, MeshTopology.Triangles, 0);
                
                vertices.Dispose();
                normals.Dispose();
                triangles.Dispose();

                mesh.RecalculateBounds();
                mesh.UploadMeshData(true);
                
                return mesh;
            }
            
            public int GetOrAddVertex(int x, int y) {
                var index = new VertexIndex(x, y);
                ref var vertex = ref GetVertex(index);
                if (vertex.meshIndex == -1) {
                    vertex.meshIndex = _meshVertices.Length;
                    _meshVertices.Add(index);
                }
                return vertex.meshIndex;
            }

            public ref readonly VertexIndex GetSpatialVertexIndex(int meshIndex) {
                return ref _meshVertices.ElementAt(meshIndex);
            }
            
            public ref VertexData GetVertex(int meshIndex) {
                return ref GetVertex(GetSpatialVertexIndex(meshIndex));
            }
            
            public ref VertexData GetVertex(in VertexIndex spatialIndex) {
                return ref _spatialVertices[(uint)(spatialIndex.x * _spatialVertexResolution + spatialIndex.y)];
            }

            public int AddTriangle(int vertex0, int vertex1, int vertex2) {
                var index = _triangles.Length;
                _triangles.Add(new Triangle {
                    v0 = vertex0,
                    v1 = vertex1,
                    v2 = vertex2
                });
                return index;
            }
            
            public ref Triangle GetTriangle(int index) {
                return ref _triangles.ElementAt(index);
            }
            
            public void Dispose() {
                _spatialVertices.Dispose();
                _meshVertices.Dispose();
                _triangles.Dispose();
            }

            static float DiscreteToInterpolated(float discrete, float scale) {
                const float EdgeTolerance = 0.0001f;
                var result = discrete * scale;
                if (math.abs(result) < EdgeTolerance) {
                    return 0;
                }
                if (math.abs(result - 1) < EdgeTolerance) {
                    return 1;
                }
                return result;
            }
        }

        ref struct Holes {
            int _resolution;
            UnsafeBitmask _holes;

            public Holes(int resolution, Allocator allocator) {
                _resolution = resolution;
                _holes = new UnsafeBitmask((uint)(resolution * resolution), allocator);
            }
            
            public bool this[int x, int z] {
                get => _holes[(uint)(x * _resolution + z)];
                set => _holes[(uint)(x * _resolution + z)] = value;
            }

            public void Dispose() {
                _holes.Dispose();
            }
        }
    }
}
