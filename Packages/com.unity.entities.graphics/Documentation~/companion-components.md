# Companion components

This feature allows you to query MonoBehaviour components in ECS system script by attaching MonoBehaviour components to entities, without converting them to IComponentData. This also means that managed companion components do **not** benefit from the fast performance that ECS components have.

The following graphics related companion components are supported by Entities Graphics:

- Light
- ReflectionProbe
- TextMesh
- SpriteRenderer
- ParticleSystem
- VisualEffect
- DecalProjector (HDRP)
- LocalVolumetricFog (HDRP)
- PlanarReflectionProbe (HDRP)
- ProbeVolume (HDRP)
- Volume
- Volume + Sphere/Box/Capsule/MeshCollider pair (local volumes)

Note that the conversion of Camera (+HDAdditionalCameraData, UniversalAdditionalCameraData) components is disabled by default, because the scene main camera can't be a companion component entity. To enable this conversion, add **HYBRID_ENTITIES_CAMERA_CONVERSION** define to your project settings.

Unity updates the transform of a companion component entity whenever it updates the LocalToWorld component. Parenting a companion component entity to a standard entity is supported. Companion component entities can be included in subscenes. The managed component is serialized in the subscene.

You can write ECS queries including both IComponentData and managed companion components. However, these queries cannot be Burst compiled and must run in the main thread because managed components aren't thread-safe. To do this, use `foreach()` without the [BurstCompile] flag instead of an [IJobEntity](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.IJobEntity.html) with .Schedule().

An example of setting HDRP Light component intensity:

```C#
class AnimateHDRPIntensitySystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach(var hdLight in SystemAPI.Query<RefRW<HDLightAdditionalData>>())
        {
            hdLight.ValueRW.intensity = 1.5f;
        }
    }
}
```
