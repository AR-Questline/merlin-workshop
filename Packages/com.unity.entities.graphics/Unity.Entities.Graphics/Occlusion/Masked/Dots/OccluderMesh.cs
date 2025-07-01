#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Unity.Rendering.Occlusion.Masked.Dots
{
    struct OcclusionMesh : IComponentData
    {
        const float EPSILON = 1E-12f;
        enum ClipPlanes
        {
            CLIP_PLANE_NONE = 0x00,
            CLIP_PLANE_NEAR = 0x01,
            CLIP_PLANE_LEFT = 0x02,
            CLIP_PLANE_RIGHT = 0x04,
            CLIP_PLANE_BOTTOM = 0x08,
            CLIP_PLANE_TOP = 0x10,
            CLIP_PLANE_SIDES = (CLIP_PLANE_LEFT | CLIP_PLANE_RIGHT | CLIP_PLANE_BOTTOM | CLIP_PLANE_TOP),
            CLIP_PLANE_ALL = (CLIP_PLANE_LEFT | CLIP_PLANE_RIGHT | CLIP_PLANE_BOTTOM | CLIP_PLANE_TOP | CLIP_PLANE_NEAR)
        };

        public unsafe void Transform(float4x4 mvp, BatchCullingProjectionType projectionType, float nearClip,
            v128* frustumPlanes, float halfWidth, float halfHeight, float pixelCenterX, float pixelCenterY,
            float4* transformedVerts,
            NativeArray<float3> clippedVerts,
            NativeArray<float4> clippedTriExtents,
            ClippedOccluder* clipped
            )
        {
            clipped->expandedVertexSize = 0;
            clipped->screenMin = float.MaxValue;
            clipped->screenMax = -float.MaxValue;

            float3* vin = (float3*)vertexData.GetUnsafePtr();
            float4* vout = transformedVerts;

            float clipW = nearClip;
            int numVertsBehindNearPlane = 0;

            for (int i = 0; i < vertexCount; ++i, ++vin, ++vout)
            {
                *vout = math.mul(mvp, new float4(*vin, 1.0f));
                vout->y = -vout->y;

                float4 p = vout->xyzw;

                if (projectionType == BatchCullingProjectionType.Orthographic)
                {
                    p.w = p.z;
                }
                else
                {
                    if (p.w < clipW)
                    {
                        numVertsBehindNearPlane++;
                        continue;
                    }
                    p.xyz /= p.w;
                }
                p.y *= -1.0f;
                clipped->screenMin = math.min(clipped->screenMin, p);
                clipped->screenMax = math.max(clipped->screenMax, p);
            }
            // if all triangles are behind the near plane we can stop it now
            if (numVertsBehindNearPlane == vertexCount)
                return;

            // compute the expanded data, after transforming the vertices the triangle is check if it faces the camera
            vout = transformedVerts;
            int* indexPtr = (int*)indexData.GetUnsafePtr();

            float3x3* vertices = stackalloc float3x3[6];// 1 + 5 planes triangles that can be generated

            const int singleBufferSize = 3 + 5;// 3 vertex + 5 planes = 8 maximum generated vertices per a triangle + 5 clipping planes
            const int doubleBufferSize = singleBufferSize * 2;
            float3* vertexClipBuffer = stackalloc float3[doubleBufferSize];

            // Loop over three indices at a time, and clip the resulting triangle
            for (int i = 0; i < indexCount; i+=3)
            {
                int numTriangles = 1;
                // Fill out vertices[0][0..3] with the initial unclipped vertices
                if (projectionType == BatchCullingProjectionType.Orthographic)
                {
                    // Use the vertices' Z coordinate if the view is orthographic
                    vertices[0][0] = vout[indexPtr[i]].xyz;
                    vertices[0][1] = vout[indexPtr[i + 1]].xyz;
                    vertices[0][2] = vout[indexPtr[i + 2]].xyz;
                }
                else
                {
                    // Use the vertices' W coordinate if the view is perspective
                    vertices[0][0] = vout[indexPtr[i]].xyw;
                    vertices[0][1] = vout[indexPtr[i + 1]].xyw;
                    vertices[0][2] = vout[indexPtr[i + 2]].xyw;
                }

                // buffer group have guardBand that adds extra padding to avoid clipping triangles on the sides
                // Test clipping simply checks that the projected triangles are inside the frustum exploiting
                // the checks against the w or z depending if it's orthographic or perspective projection
                ClippingTestResult clippingTestResult = TestClipping(vertices[0], nearClip, projectionType == BatchCullingProjectionType.Orthographic);
                // If the whole triangle is outside the clipping bounds, then it is discarded entirely. We just skip to
                // the next triangle.
                if (clippingTestResult == ClippingTestResult.Outside) continue;

                // If the triangle is partially inside and partially outside the clipping bounds, then we turn it into a
                // polygon entirely contained within the clipping bounds.
                if (clippingTestResult == ClippingTestResult.Clipping)
                {
                    numTriangles = 0;
                    // Load the initial vertices into the clip buffer
                    vertexClipBuffer[0] = vertices[0][0];
                    vertexClipBuffer[1] = vertices[0][1];
                    vertexClipBuffer[2] = vertices[0][2];

                    int nClippedVerts = 3;
                    // The vertex clip buffer is a double buffer, which means that we use half of the buffer for reading
                    // and the other half for writing. The `bufferSwap` variable controls which half is the read and
                    // which half is written. After each plane is processed, we toggle it between 0 and 1, i.e. we flip
                    // the swapchain.
                    int bufferSwap = 0;
                    for (int n = 0; n < 5; ++n)//clipping 5 planes
                    {
                        // Sutherland-Hodgman polygon clipping algorithm https://mikro.naprvyraz.sk/docs/Coding/2/FRUSTUM.TXT
                        // swapping buffers from input output, the algorithm works by clipping every plane once at a time
                        float3* outVtx = vertexClipBuffer + ((bufferSwap ^ 1) * singleBufferSize);
                        float3* inVtx = vertexClipBuffer + (bufferSwap * singleBufferSize);
                        float4 plane = new float4(frustumPlanes[n].Float0, frustumPlanes[n].Float1, frustumPlanes[n].Float2, frustumPlanes[n].Float3);
                        nClippedVerts = ClipPolygon(outVtx, inVtx, plane, nClippedVerts);
                        // Toggle between 0 and 1
                        bufferSwap ^= 1;
                    }
                    // nClippedVerts can be lower than 3 and that's why numTriangles is 0 as default,
                    // from there for every extra vertex a triangle it's added as a fan
                    /*
                     *    x
                     *    |\
                     *    | \
                     *    |  \
                     *    |   \
                     *    x-_  \
                     *       `-_\  <------if the clipping plane is here it will add 2 vertex producing
                     *          `x
                     *
                     *     x
                     *     |\
                     *     | \
                     *     |  \
                     *     |   \   <------X are the new added vertices and the final result will be
                     *     x-_  \
                     *        `X_X
                     *           `x
                     *
                     *     x
                     *     |\
                     *     |.\
                     *     | :\
                     *     | \ \   <------X are the new added vertices and the final result will be the extra edges cutting from the the first vertex to the new ones and between the added ones
                     *     x-_\ \
                     *        `X-X
                     *
                     */
                    if (nClippedVerts >= 3)
                    {
                        // Copy over the clipped vertices to the result array
                        vertices[0][0] = vertexClipBuffer[bufferSwap * singleBufferSize + 0];
                        vertices[0][1] = vertexClipBuffer[bufferSwap * singleBufferSize + 1];
                        vertices[0][2] = vertexClipBuffer[bufferSwap * singleBufferSize + 2];

                        numTriangles++;

                        for (int n = 2; n < nClippedVerts - 1; n++)
                        {
                            // ^ If we have more than 3 vertices after clipping, create a triangle-fan, with the 0th
                            // vertex shared across all triangles of the fan.
                            vertices[numTriangles][0] = vertexClipBuffer[bufferSwap * singleBufferSize];
                            vertices[numTriangles][1] = vertexClipBuffer[bufferSwap * singleBufferSize + n];
                            vertices[numTriangles][2] = vertexClipBuffer[bufferSwap * singleBufferSize + n + 1];
                            numTriangles++;
                        }
                    }

                }

                for(int n = 0; n < numTriangles; ++n)
                {
                    int2x3 intHomogeneousVertices = int2x3.zero;
                    float2x3 homogeneousVertices = float2x3.zero;
                    float2 halfRes = new float2(halfWidth, halfHeight);
                    float2 pixelCenter = new float2(pixelCenterX, pixelCenterY);
                    if (projectionType == BatchCullingProjectionType.Orthographic)
                    {
                        homogeneousVertices[0] = (vertices[0][0].xy * halfRes) + pixelCenter;
                        homogeneousVertices[1] = (vertices[0][1].xy * halfRes) + pixelCenter;
                        homogeneousVertices[2] = (vertices[0][2].xy * halfRes) + pixelCenter;

                        homogeneousVertices[0] *= (float)(1 << BufferGroup.FpBits);
                        homogeneousVertices[1] *= (float)(1 << BufferGroup.FpBits);
                        homogeneousVertices[2] *= (float)(1 << BufferGroup.FpBits);

                        intHomogeneousVertices.c0.x = (int)math.round(homogeneousVertices.c0.x);
                        intHomogeneousVertices.c1.x = (int)math.round(homogeneousVertices.c1.x);
                        intHomogeneousVertices.c2.x = (int)math.round(homogeneousVertices.c2.x);
                        intHomogeneousVertices.c0.y = (int)math.round(homogeneousVertices.c0.y);
                        intHomogeneousVertices.c1.y = (int)math.round(homogeneousVertices.c1.y);
                        intHomogeneousVertices.c2.y = (int)math.round(homogeneousVertices.c2.y);

                        homogeneousVertices = new float2x3(intHomogeneousVertices) / (float)(1 << BufferGroup.FpBits);
                    }
                    else
                    {
                        homogeneousVertices[0] = (vertices[0][0].xy * halfRes) / vertices[0][0].z + pixelCenter;
                        homogeneousVertices[1] = (vertices[0][1].xy * halfRes) / vertices[0][1].z + pixelCenter;
                        homogeneousVertices[2] = (vertices[0][2].xy * halfRes) / vertices[0][2].z + pixelCenter;

                        homogeneousVertices[0] *= (float)(1 << BufferGroup.FpBits);
                        homogeneousVertices[1] *= (float)(1 << BufferGroup.FpBits);
                        homogeneousVertices[2] *= (float)(1 << BufferGroup.FpBits);

                        intHomogeneousVertices.c0.x = (int)math.round(homogeneousVertices.c0.x);
                        intHomogeneousVertices.c1.x = (int)math.round(homogeneousVertices.c1.x);
                        intHomogeneousVertices.c2.x = (int)math.round(homogeneousVertices.c2.x);
                        intHomogeneousVertices.c0.y = (int)math.round(homogeneousVertices.c0.y);
                        intHomogeneousVertices.c1.y = (int)math.round(homogeneousVertices.c1.y);
                        intHomogeneousVertices.c2.y = (int)math.round(homogeneousVertices.c2.y);

                        homogeneousVertices = new float2x3(intHomogeneousVertices) / (float)(1 << BufferGroup.FpBits);
                    }

                    // Checkin determinant > 0 more details in:
                    // Triangle Scan Conversion using 2D Homogeneous Coordinates
                    // https://www.cs.cmu.edu/afs/cs/academic/class/15869-f11/www/readings/olano97_homogeneous.pdf
                    // Section 5.2
                    // In 3D, a matrix determinant gives twice the signed volume of a tetrahedron.In
                    // eye space, this is the tetrahedron with the eye at the apex and the
                    // triangle to be rendered as the base.If all of the 2D w coordinates
                    // are 1, the determinant is also exactly twice the signed screenspace aren of the triangle.
                    // If the determinant is zero, either the triangle is degenerate or the view is edge - on.
                    // Furthermore, for vertices defined by the right-hand rule, the determinant is positive if the triangle
                    // is front-facing and negative if the triangle is back-facing. 

                    float area = (homogeneousVertices.c1.x - homogeneousVertices.c2.x) * (homogeneousVertices.c0.y - homogeneousVertices.c2.y)
                               - (homogeneousVertices.c2.x - homogeneousVertices.c0.x) * (homogeneousVertices.c2.y - homogeneousVertices.c1.y);

                    if (area > EPSILON)
                    {
                        if (projectionType == BatchCullingProjectionType.Orthographic)
                        {
                            homogeneousVertices[0] = vertices[n][0].xy;
                            homogeneousVertices[1] = vertices[n][1].xy;
                            homogeneousVertices[2] = vertices[n][2].xy;
                        }
                        else
                        {
                            homogeneousVertices[0] = vertices[n][0].xy / vertices[n][0].z;
                            homogeneousVertices[1] = vertices[n][1].xy / vertices[n][1].z;
                            homogeneousVertices[2] = vertices[n][2].xy / vertices[n][2].z;
                        }
                        homogeneousVertices[0].y *= -1.0f;
                        homogeneousVertices[1].y *= -1.0f;
                        homogeneousVertices[2].y *= -1.0f;

                        clippedTriExtents[clipped->sourceIndexOffset * 2 + clipped->expandedVertexSize / 3] = new float4(
                            math.min(math.min(homogeneousVertices[0], homogeneousVertices[1]), homogeneousVertices[2]),
                            math.max(math.max(homogeneousVertices[0], homogeneousVertices[1]), homogeneousVertices[2])
                        );
                        // Copy final vertex data into output arrays
                        for (int m = 0; m < 3; ++m)
                        {
                            clippedVerts[clipped->sourceIndexOffset * 6 + clipped->expandedVertexSize] = vertices[n][m];
                            clipped->expandedVertexSize++;
                        }
                    }
                }
            }
            // If fully visible we can skip computing the screen aabb again but with the extra clipping logic
            if (numVertsBehindNearPlane == 0 && !(clipped->screenMin.x > 1.0f || clipped->screenMin.y > 1.0f || clipped->screenMax.x < -1.0f || clipped->screenMax.y < -1.0f))
                return;

            if (numVertsBehindNearPlane == 0 || numVertsBehindNearPlane == vertexCount)// if triangles does not cross near plane we can skip the slow path too
                return;

            var edges = stackalloc int2[]
            {
                new int2(0,1), new int2(1,3), new int2(3,2), new int2(2,0),
                new int2(4,6), new int2(6,7), new int2(7,5), new int2(5,4),
                new int2(4,0), new int2(2,6), new int2(1,5), new int2(7,3)
            };

            float3 minAABB, maxAABB;
            maxAABB.x = maxAABB.y = maxAABB.z = -float.MaxValue;
            minAABB.x = minAABB.y = minAABB.z =  float.MaxValue;
            vin = (float3*)vertexData.GetUnsafePtr();

            for (int i = 0; i < vertexCount; ++i, ++vin)
            {
                float3 p = vin->xyz;

                minAABB = math.min(minAABB, p);
                maxAABB = math.max(maxAABB, p);
            }

            AABB aabb;
            aabb.Center = (maxAABB + minAABB) * 0.5f;
            aabb.Extents = (maxAABB - minAABB) * 0.5f;

            var verts = stackalloc float4[16];

            float4x2 u = new float4x2(mvp.c0 * aabb.Min.x, mvp.c0 * aabb.Max.x);
            float4x2 v = new float4x2(mvp.c1 * aabb.Min.y, mvp.c1 * aabb.Max.y);
            float4x2 w = new float4x2(mvp.c2 * aabb.Min.z, mvp.c2 * aabb.Max.z);

            for (int corner = 0; corner < 8; corner++)
            {
                float4 p = u[corner & 1] + v[(corner & 2) >> 1] + w[(corner & 4) >> 2] + mvp.c3;
                p.y = -p.y;
                verts[corner] = p;
            }

            int internalVertexCount = 8;
            for (int i = 0; i < 12; i++)
            {
                var e = edges[i];
                var a = verts[e.x];
                var b = verts[e.y];

                if ((a.w < clipW) != (b.w < clipW))
                {
                    var p = math.lerp(a, b, (clipW - a.w) / (b.w - a.w));
                    verts[internalVertexCount++] = p;
                }
            }

            for (int i = 0; i < internalVertexCount; i++)
            {
                float4 p = verts[i];

                if (projectionType == BatchCullingProjectionType.Orthographic)
                {
                    p.w = p.z;
                }
                else
                {
                    if (p.w < EPSILON)
                        continue;

                    p.xyz /= p.w;
                }
                p.y *= -1.0f;
                clipped->screenMin = math.min(clipped->screenMin, p);
                clipped->screenMax = math.max(clipped->screenMax, p);
            }
        }

        enum ClippingTestResult
        {
            Inside = 0,
            Clipping,
            Outside
        };
        unsafe ClippingTestResult TestClipping(float3x3 vertices, float NearClip, bool isOrtho)
        {

            ClippingTestResult* straddleMask = stackalloc ClippingTestResult[5];
            straddleMask[0] = TestClipPlane(ClipPlanes.CLIP_PLANE_NEAR, vertices, NearClip, isOrtho);
            straddleMask[1] = TestClipPlane(ClipPlanes.CLIP_PLANE_LEFT, vertices, NearClip, isOrtho);
            straddleMask[2] = TestClipPlane(ClipPlanes.CLIP_PLANE_RIGHT, vertices, NearClip, isOrtho);
            straddleMask[3] = TestClipPlane(ClipPlanes.CLIP_PLANE_BOTTOM, vertices, NearClip, isOrtho);
            straddleMask[4] = TestClipPlane(ClipPlanes.CLIP_PLANE_TOP, vertices, NearClip, isOrtho);

            if( straddleMask[0] == ClippingTestResult.Inside &&
                straddleMask[1] == ClippingTestResult.Inside &&
                straddleMask[2] == ClippingTestResult.Inside &&
                straddleMask[3] == ClippingTestResult.Inside &&
                straddleMask[4] == ClippingTestResult.Inside)
            {
                return ClippingTestResult.Inside;
            }
            if (straddleMask[0] == ClippingTestResult.Outside ||
                straddleMask[1] == ClippingTestResult.Outside ||
                straddleMask[2] == ClippingTestResult.Outside ||
                straddleMask[3] == ClippingTestResult.Outside ||
                straddleMask[4] == ClippingTestResult.Outside)
            {
                return ClippingTestResult.Outside;
            }
            return ClippingTestResult.Clipping;
        }

        // This function checks whether and how a triangle intersects a clipping plane.
        // The clipping plane divides 3D space into two parts - one being inside the frustum and one being outside. This
        // function returns whether the input triangle is fully on the inside, fully on the ouside, or a bit on both
        // sides, i.e. clipping the plane.
        unsafe ClippingTestResult TestClipPlane(ClipPlanes clipPlane, float3x3 vertices, float NearClip, bool isOrtho)
        {
            // Evaluate all 3 vertices against the frustum plane
            float* planeDp = stackalloc float[3];

            if (!isOrtho)
            {
                for (int i = 0; i < 3; ++i)
                {
                    switch (clipPlane)
                    {
                        case ClipPlanes.CLIP_PLANE_LEFT: planeDp[i] = vertices[i].z + vertices[i].x; break;
                        case ClipPlanes.CLIP_PLANE_RIGHT: planeDp[i] = vertices[i].z - vertices[i].x; break;
                        case ClipPlanes.CLIP_PLANE_BOTTOM: planeDp[i] = vertices[i].z + vertices[i].y; break;
                        case ClipPlanes.CLIP_PLANE_TOP: planeDp[i] =  vertices[i].z - vertices[i].y; break;
                        case ClipPlanes.CLIP_PLANE_NEAR: planeDp[i] = vertices[i].z - NearClip; break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < 3; ++i)
                {
                    switch (clipPlane)
                    {
                        case ClipPlanes.CLIP_PLANE_LEFT: planeDp[i] = 1.0f + vertices[i].x; break;
                        case ClipPlanes.CLIP_PLANE_RIGHT: planeDp[i] = 1.0f - vertices[i].x; break;
                        case ClipPlanes.CLIP_PLANE_BOTTOM: planeDp[i] = 1.0f + vertices[i].y; break;
                        case ClipPlanes.CLIP_PLANE_TOP: planeDp[i] = 1.0f - vertices[i].y; break;
                        case ClipPlanes.CLIP_PLANE_NEAR: planeDp[i] = 1.0f + NearClip; break;
                    }
                }
            }
            if (planeDp[0] > EPSILON && planeDp[1] > EPSILON && planeDp[2] > EPSILON)
            {
                return ClippingTestResult.Inside;
            }

            if (planeDp[0] <= EPSILON && planeDp[1] <= EPSILON && planeDp[2] <= EPSILON)
            {
                return ClippingTestResult.Outside;
            }
            return ClippingTestResult.Clipping;
        }

        unsafe int ClipPolygon(float3* outVtx, float3* inVtx, float4 plane, int n)
        {
            float3 p0 = inVtx[n - 1];
            float dist0 = math.dot(p0, plane.xyz) + plane.w;

            // Loop over all polygon edges and compute intersection with clip plane (if any)
            int nout = 0;

            for (int k = 0; k < n; k++)
            {
                float3 p1 = inVtx[k];
                float dist1 = math.dot(p1, plane.xyz) + plane.w;

                if (dist0 > EPSILON)
                {
                    outVtx[nout++] = p0;
                }

                // Edge intersects the clip plane if dist0 and dist1 have opposing signs
                if (math.sign(dist0) != math.sign(dist1))
                {
                    // Always clip from the positive side to avoid T-junctions
                    if (dist0 > EPSILON)
                    {
                        float t = dist0 / (dist0 - dist1);
                        outVtx[nout++] = t * (p1 - p0) + p0;
                    }
                    else
                    {
                        float t = dist1 / (dist1 - dist0);
                        outVtx[nout++] = t * (p0 - p1) + p1;
                    }
                }

                dist0 = dist1;
                p0 = p1;
            }

            return nout;
        }

        public int vertexCount;
        public int indexCount;

        public BlobAssetReference<float3> vertexData;
        public BlobAssetReference<int> indexData;

        public float3x4 localTransform;
    }
}

#endif
