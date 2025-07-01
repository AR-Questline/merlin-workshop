# What's new in version 1.0

Summary of changes in Entities Graphics package version 1.0.

The main updates in this release include:

### Added

* Added support for OpenGL ES 3.1 on Android.
* New `RegisterMesh` and `RegisterMaterial` APIs.
* Added efficient Mesh and Material switching at runtime using `MaterialMeshInfo`.

#### New RegisterMesh and RegisterMaterial APIs

In Entities Graphics 1.0, you can directly register your own meshes and materials to use for rendering. To do this, call the new `EntitiesGraphicsSystem.RegisterMaterial` and `EntitiesGraphicsSystem.RegisterMesh` APIs to get Burst-compatible IDs that can be placed in a `MaterialMeshInfo` component.

#### Efficient Mesh and Material switching at runtime using MaterialMeshInfo

Entities Graphics 1.0 uses the new `MaterialMeshInfo` component to specify the mesh and material for entities. `MaterialMeshInfo` is a normal IComponentData, and you can change its value efficiently, unlike a shared component. This makes it possible to efficiently change the mesh and material that Unity uses to render an entity.

To use the new `MaterialMeshInfo` component, you either need to manually register materials and meshes using the new `RegisterMaterial` and `RegisterMesh` APIs, or you can also use array indices to select the corresponding mesh and material from the `RenderMeshArray` shared component, if the entity has one.

Entities that use manually registered IDs don't need to have a `RenderMeshArray` component, while entities that use array indices must have one. Entities Graphics 1.0 entity baking uses `RenderMeshArray`.

### Updated

* Updated the name of the package from Hybrid Renderer to Entities Graphics.
* Updated Scene view entity picking to use [BatchRendererGroup](https://docs.unity3d.com/2022.1/Documentation/Manual/batch-renderer-group.html).
* Rework sub-mesh handling for MeshRenderer components.

#### Rework sub-mesh handling

In previous versions, Entities Graphics generated one entity per sub-mesh on a MeshRenderer. Those entities were mostly duplicates, wasted a lot of memory, and negatively impacted performance.  

To avoid this behavior, add a scripting define to your project. Go to **Edit &gt; Project Settings &gt; Player &gt; Scripting Define Symbols** and add the define `ENABLE_MESH_RENDERER_SUBMESH_DATA_SHARING` to the list.  

The duplicated entities will no longer be created to handle sub-meshes except when using SkinnedMeshRenderer. If your project relies on this behavior, then you might need to manually create one entity per sub-mesh yourself, because Entities Graphics won't do so anymore.

### Removed

* Removed the HLOD component.

For a full list of changes and updates in this version, refer to the Entities Graphics package [changelog](xref:changelog).
