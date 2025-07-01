using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

class LODGroupBaker : Baker<LODGroup>
{
    public override void Bake(LODGroup authoring)
    {
        if (authoring.lodCount > 8)
        {
            Debug.LogWarning("LODGroup has more than 8 LOD - Not supported", authoring);
            return;
        }

        var lodGroupData = new MeshLODGroupComponent();
        //@TODO: LOD calculation should respect scale...
        var worldSpaceSize = LODGroupExtensions.GetWorldSpaceScale(GetComponent<Transform>()) * authoring.size;
        lodGroupData.LocalReferencePoint = authoring.localReferencePoint;

        var lodDistances0 = new float4(float.PositiveInfinity);
        var lodDistances1 = new float4(float.PositiveInfinity);
        var lodGroupLODs = authoring.GetLODs();
        for (int i = 0; i < authoring.lodCount; ++i)
        {
            float d = worldSpaceSize / lodGroupLODs[i].screenRelativeTransitionHeight;
            if (i < 4)
                lodDistances0[i] = d;
            else
                lodDistances1[i - 4] = d;
        }

        lodGroupData.LODDistances0 = lodDistances0;
        lodGroupData.LODDistances1 = lodDistances1;

        var entity = GetEntity(TransformUsageFlags.Renderable);
        AddComponent(entity, lodGroupData);
    }
}
