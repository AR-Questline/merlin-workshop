# Runtime entity creation

To render an entity, Entities Graphics requires that the entity contains a specific minimum set of components. The list of components Entities Graphics requires is substantial and may change in the future. To allow you to flexibly create entities at runtime in a way that is consistent between package versions, Entities Graphics provides the `RenderMeshUtility.AddComponents` API.

## RenderMeshUtility - AddComponents

This API takes an entity and adds the components Entities Graphics requires based on the given mesh and material, and a `RenderMeshDescription`, which is a struct that describes additional rendering settings. There are two versions of the API:

- One version accepts a `RenderMesh`. For information on the structure of a `RenderMesh`, see [RenderMesh](#rendermesh). Entities Graphics only uses this version during the GameObject baking process; using this version at runtime doesn't produce rendering entities.
- A second version accepts a `RenderMeshArray`. For information on the structure of a `RenderMeshArray`, see [RenderMeshArray](#rendermesharray). Use this version of `AddComponents` at runtime.

### RenderMeshDescription and RenderFilterSettings

A `RenderMeshDescription` struct describes when and how to draw an entity. It contains a `RenderFilterSettings` which specifies layering, shadowing and motion vector settings, and light probe usage settings.

### RenderMesh

A `RenderMesh` describes which mesh and material an entity should use. Entities Graphics uses the `RenderMesh` component during GameObject baking before transforming the entity into a more efficient format.

**Note**: Entities Graphics no longer uses this component at runtime. It only uses this component to simplify the GameObject baking process.

### RenderMeshArray

A `RenderMeshArray` contains a list of meshes and materials that a collection of entities share. Each entity can efficiently select from any mesh and material inside this array using a `MaterialMeshInfo` component created using the `MaterialMeshInfo.FromRenderMeshArrayIndices` method. During GameObject baking, Entities Graphics attempts to pack all meshes and materials in a single subscene into a single shared `RenderMeshArray` to minimize chunk fragmentation.

### MaterialMeshInfo

The `MaterialMeshInfo` is a Burst-compatible plain data component that you can use to efficiently select or change an entity's mesh and material. This component supports two methods of selecting or changing an entity's mesh or material:

- Referring to array indices inside a `RenderMeshArray` shared component on the same entity.
- Referring directly to mesh and material IDs that you registered with the Entities Graphics beforehand.

### Usage instructions

This API tries to be as efficient as possible, but it is still a main-thread only API and therefore not suitable for creating a large number of entities. Instead, it is best practice to use `Instantiate` to efficiently clone existing entities then set their components (e.g. `Translation` or `LocalToWorld`) to new values afterward. This workflow has several advantages:

- You can bake the base entity from a Prefab, or create it at runtime using `RenderMeshUtility.AddComponents`. Instantiation performance does not depend on which approach you use.
- `Instantiate` and `SetComponent` / `SetComponentData` don't cause resource-intensive structural changes.
- You can use `Instantiate` and `SetComponent` from Burst jobs using `EntityCommandBuffer.ParallelWriter`, which efficiently scales to multiple cores.
- Internal Entities Graphics components are pre-created for the entities, which means that Entities Graphics does not need to create those components at runtime.

### Example usage

The following code example shows how to use `RenderMeshUtility.AddComponents` to create a base entity and then instantiate that entity many times in a Burst job:

```c#
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public class AddComponentsExample : MonoBehaviour
{
    public Mesh Mesh;
    public Material Material;
    public int EntityCount;

    // Example Burst job that creates many entities
    [GenerateTestsForBurstCompatibility]
    public struct SpawnJob : IJobParallelFor
    {
        public Entity Prototype;
        public int EntityCount;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(int index)
        {
            // Clone the Prototype entity to create a new entity.
            var e = Ecb.Instantiate(index, Prototype);
            // Prototype has all correct components up front, can use SetComponent to
            // set values unique to the newly created entity, such as the transform.
            Ecb.SetComponent(index, e, new LocalToWorld {Value = ComputeTransform(index)});
        }

        public float4x4 ComputeTransform(int index)
        {
            return float4x4.Translate(new float3(index, 0, 0));
        }
    }

    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        // Create a RenderMeshDescription using the convenience constructor
        // with named parameters.
        var desc = new RenderMeshDescription(
            shadowCastingMode: ShadowCastingMode.Off,
            receiveShadows: false);

        // Create an array of mesh and material required for runtime rendering.
        var renderMeshArray = new RenderMeshArray(new Material[] { Material }, new Mesh[] { Mesh });

        // Create empty base entity
        var prototype = entityManager.CreateEntity();

        // Call AddComponents to populate base entity with the components required
        // by Entities Graphics
        RenderMeshUtility.AddComponents(
            prototype,
            entityManager,
            desc,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        entityManager.AddComponentData(prototype, new LocalToWorld());

        // Spawn most of the entities in a Burst job by cloning a pre-created prototype entity,
        // which can be either a Prefab or an entity created at run time like in this sample.
        // This is the fastest and most efficient way to create entities at run time.
        var spawnJob = new SpawnJob
        {
            Prototype = prototype,
            Ecb = ecb.AsParallelWriter(),
            EntityCount = EntityCount,
        };

        var spawnHandle = spawnJob.Schedule(EntityCount, 128);
        spawnHandle.Complete();

        ecb.Playback(entityManager);
        ecb.Dispose();
        entityManager.DestroyEntity(prototype);
    }
}
```
