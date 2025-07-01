# Entities Graphics overview

Entities Graphics acts as a bridge between ECS for Unity and Unity's existing rendering architecture. This allows you to use ECS instead of GameObjects for significantly improved runtime memory layout and performance in large scenes, while maintaining the compatibility and ease of use of Unity's existing workflows.

## Entities Graphics feature matrix

For more information about the render pipeline feature compatibility, see [Entities Graphics feature matrix](entities-graphics-versions.md).

## The GameObject baking system

Entities Graphics includes systems that bake GameObjects into equivalent Entities. You can use these systems to bake the GameObjects in the Unity Editor. 

To bake entities in the Unity Editor, put them in a SubScene. The Unity Editor performs the baking offline, and saves the results to disk.

Unity performs the following steps during baking:

- The baking system bakes [MeshRenderer](xref:class-MeshRenderer) and [MeshFilter](xref:class-MeshFilter) components into a RenderMesh component on the entity. Depending on the render pipeline your Project uses, the baking system might also add other rendering-related components.
- The baking system bakes [LODGroup](xref:class-LODGroup) components in GameObject hierarchies to MeshLODGroupComponents. Each entity referred by the LODGroup component has a MeshLODComponent.
- The baking system bakes the Transform of each GameObject into a LocalToWorld component on the entity. Depending on the Transform's properties, the baking system might also add Translation, Rotation, and NonUniformScale components.

## Runtime functionality

At runtime, Entities Graphics processes all entities that have LocalToWorld, RenderMesh, and RenderBounds components. Many HDRP and URP features require their own material property components. These components are added during the MeshRenderer baking. Processed entities are added to batches. Unity renders the batches using the [SRP Batcher](https://blogs.unity3d.com/2019/02/28/srp-batcher-speed-up-your-rendering/).

There are two best practice ways to instantiate entities at runtime: Prefabs and the `RenderMeshUtility.AddComponents` API:

* Unity bakes Prefabs to an optimal data layout during baking. There are no additional structural changes during instantiation. Use prefabs if you want to instantiate complex objects.
* If you want to build the entity from a C# script, use the [RenderMeshUtility.AddComponents](runtime-entity-creation.md) API. This API automatically adds all the graphics components that the active render pipeline requires. Don't add graphics components manually. This is less efficient (adds structural changes) and is not forward compatible with future Entities Graphics package versions.

> [!NOTE]
> No ECS modifications are allowed in PresentationGroup. With the following exceptions:
>
>* **UpdatePresentationSystemGroup**: You are allowed to modify component data. But do not do ECS structural changes.
>* **StructuralChangePresentationSystemGroup**: You are allowed to do ECS structural changes and modify component data.Since the presentation group is the last System Group running in the frame, you are not allowed to do any ECS data modifications after the presentation group. The culling jobs run after, and they lean on data being in the same state as it was in the presentation group. If you want to delay structural changes to the end of the frame, delay them instead to the beginning of the next frame.