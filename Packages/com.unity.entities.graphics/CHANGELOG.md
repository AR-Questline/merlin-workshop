---
uid: changelog
---

# Changelog

## [1.2.0-pre.4] - 2023-11-28

### Changed
* Updated version for release preparation


## [1.2.0-exp.3] - 2023-11-09

### Changed

* Update instances of deprecated `MeshWriteData.uvRegion` for `2023.2` or higher
* obsolete `FindObjectsOfType<T>()` changed for `FindObjectsByType<T>()` in `2023.2` or higher.
* The minimum supported editor version is now 2022.3.11f1


## [1.1.0-pre.3] - 2023-10-17

### Changed
* Updated version for release preparation


## [1.1.0-exp.1] - 2023-09-18

### Added

* Support for using a mesh in multiple LODs within a LODGroup when in a subscene.
* A variation of RenderMeshUtility.AddComponents that does not need a RenderMeshArray
* Support maximum LOD level quality setting within a subscene.
* Add support for multiple submeshes per entity. Stop creating one entity per submesh unless skinning is used.
* Support for decal projector components in a subscene.
* Cull based on CullLightmapShadowCasters flag in FrustumCullingJob

### Changed

* MaterialMeshInfo now inherits IEnableable and allows high frequency state changes

### Fixed

* Address incorrect motion vector calculation on initial frame.
* JobTempAlloc warnings that might occur under some circumstances.
* Rendering without a RenderMeshArray component
* Addresses rendering artifacts when Entities Graphics GPU allocation fails
* Memory leaks in Burst Occlusion Culling
* Support for DecalProjector in URP added to CompanionComponentSupportedTypes


## [1.0.16] - 2023-09-11

### Changed
* Updated com.unity.entities dependency to 1.0.16


## [1.0.14] - 2023-07-27

### Added

* Improve receiver sphere culling to make it more precise.

### Changed

* Updated com.unity.entities dependency to 1.0.14


## [1.0.11] - 2023-06-19

### Changed
* Updated com.unity.entities dependency to 1.0.11


## [1.0.10] - 2023-05-23

### Fixed

* Fixed Gizmos for components inside the subscene.
* Fixed HDRP/AxF shader error when using it on objects in the subscene.
* Fixed light cookies shader error.
* Fixed Standalone crash due to deadlocks.
* Fixed missing volume component from standalone player.
* Fixed URP Particle Shaders error on MeshRenderer in subscene.
* Fixed URP Forward+ check sometimes failing in URP Projects configured for 2D rendering.

### Known Issues

* The workflow with auto-generated lightmaps is not supported and for entity-based applications it is disabled. Therefore, lightmaps should manually be generated through Window > Rendering > Lighting.
* When there is no directional light outside the subscene, ambient lighting will be missing from the editor's playmode and the player. Other lighting data e.g. max distance for cascade shadows will also be different compared to when directional light is outside the subscene.
* If there a camera renders into a RenderTexture and this RenderTexture is assigned on a material on a MeshRenderer object inside the subscene, the RenderTexture will be incorrect in player.
* Shader variants for DOTS_INSTANCING_ON will always be compiled and included in player build as they are not being stripped even if they are not in use. This could result in longer player build time and potentially increase runtime memory usage.
* If a scene has fog enabled or has lightmap, and if the subscene does not have the same fog / lightmap settings, when making a player build, the subscene objects will be rendering wrong as they have wrong fog mode / lightmap modes as the shader variant is being stripped.
* Preview is not supported for ParticleSystem and VisualEffect (VFX) objects in subscene.
* ParticleSystem in subscene with Light module won't render the lights.
* When HDRP PlanarReflectionProbe object is in subscene, it is expected that the 'Maximum Planar Reflection Probes on Screen' property in the HDRP asset needs to be increased.
* Subtractive lighting mode renders incorrectly in subscene.
* HDRP LocalVolumetricFog component renders 2x density in subscene.
* HDRP LitTessellation shader errors on OSX Metal.
* LOD max level does not work for entities.
* LOD crossfade is not supported for entities.
* Vertex attributes in Rendering Debugger renders pink for entities in URP.
* URP Decal Projector does not work inside the subscene.
* Lens flare component is not supported in subscene.
* Textmesh Pro component is not supported in subscene.
* UI and Canvas components are not supported in subscene.
* SpeedTree shaders are not supported in subscene in URP.
* Universal render Pipeline/ 2D shaders are not supported on a MeshRenderer. Note: SpriteRenderer components are companion components.
* Universal render Pipeline/VR shaders are not supported in subscene.
* Terrains are not supported in subscene.
* On Console, there is a subscene async load crash issue that might cause player build crashes very quickly after running.
* On Console, there might be some lighting artifacts due to GfxDevice issues.
* On Console, objects in scenes with multiple cameras might flicker in player due to an issue in native graphics jobs.


## [1.0.8] - 2023-04-17

### Added

* Support for CPU-based (Burst) masked occlusion culling on Neon processors.
* Explicit usage of Burst's FMA optimization, parallel sort, and missing changes from previous review feedback of boc-neon branch

### Changed

* Greatly improved performance of CPU-based (Burst) masked occlusion culling.
* Greatly improved performance of Depth and Test debug views used with CPU-based (Burst) masked occlusion culling.
* Reduced the amount of memory allocated by allocating based on the maximum number of worker threads the running platform requires rather than defaulting to using a theoretical upper-bound of 128 worker threads.

### Fixed

* Entities Graphics Occlusion shader throws errors when building the project
* Fixed a GraphicsBuffer leak that could occur in cases where Entities Graphics is running without any entities to render.
* enabling/disabling per-view occlusion

## [1.0.0-pre.65] - 2023-03-21

### Added

* Burst Occlusion Culling occlusion browser tool

### Changed

* Disable Entities Graphics error message if there is no active SRP.


## [1.0.0-pre.44] - 2023-02-13

### Added

* Optimize OnPerformCulling by computing the CullingSplits from a main thread Burst job.
* EntitiesGraphicsSystem.GetMesh and EntitiesGraphicsSystem.GetMaterial are now public.
* Warning in-editor when using Entities.Graphics and URP that Forward+ rendering path should be used.

### Changed

* Removed async readback from deformations and replaced with fixed number of frames.
* The built-in properties for URP has been changed so that they have a slider for most values between 0-1. Relevant properties have also been changed from using a float4 for color in the inspector to use a color property instead.
* `PushBlendWeightSystem.cs`, `PushSkinMatrixSystem.cs`, `MeshRendererBaking.cs` script files have been updated to the latest idiomatic `foreach()`.
* Changed skin matrix / blend shape weight access in CopyBlendShapeWeightsToGPUJob and CopySkinMatricesToGPUJob to readonly.
* Improved `SkinnedMeshRenderer` baking performance
* Updated com.unity.render-pipelines.core dependency from 14.0.4 to 14.0.6

### Removed

 * Removed RemapMaterialMeshIndexJob. Both array indices and runtime IDs are now directly supported for MaterialMeshInfo.

### Fixed

* Occlusion browser not being updated properly in play mode.
* Only call UpdateAllBatches if there are entities graphics chunks
* Fixed a bug with Baked Lights inside subscenes, where the bakingOutput of such Lights was not updated correctly.
* Fixed global ambient probe when multiple cameras are present with HDRP
* Light baking will cause the MeshRenderers to bake, thus automatically updating the light map in the entities scene. This previously required forcing the reimport of the entity scene.
* 'GraphicsDeviceType.OpenGLES2' is obsolete: 'OpenGL ES 2.0 is no longer supported in Unity 2023.1'

## [1.0.0-pre.15] - 2022-11-16

### Added

* Burst Occlusion Culling occlusion browser tool

### Removed

* Removing dependencies on `com.unity.jobs` package.
* Removed confusing RenderMesh overload of RenderMeshUtility.AddComponents, which does not do anything in Entities 1.0.

### Fixed

* Directional shadow caster culling false negatives.
* Make OnPerformCulling private
* Fixed a NullPointerException when running with -nographics or unsupported platform.
* Blend shapes not working on certain GPUs.
* Improved error behavior when null Meshes or Materials are used with Entities.

### Security

## [1.0.0-exp.14] - 2022-10-19

### Changed
* Updated documentation for mesh deformations.
* Removed async readback from deformations and replaced with fixed number of frames.

### Fixed
* Fixed a bug in the BatchingBenchmark sample scene where it would only spawn entites the first time it was dynamically loaded.
* Fixed an issue where Entities Graphics would cause issues with the device being able to idle.


## [1.0.0-epx.8] - 2022-09-21

### Added

* Hybrid assemblies will not be included in DOTS Runtime builds.
* Error/loading shader support for Hybrid Renderer
* Implement error/loading shader support for Entities Graphics.
* Implement Entity picking
* Shadow receiver sphere culling, which culls entities that cannot cast a visible shadow in the camera.
* Support for skinned motion vectors for High Definition Render Pipeline.

### Changed

* HLOD now requires root LOD nodes to have an HLODParent component rather than any GameObject parent.
* Removed use of the obsolete AlwaysUpdateSystem attribute. The new RequireMatchingQueriesForUpdate attribute has been added where appropriate.
* use the existing mesh buffer to retrieve the shared mesh in bind pose.
* use the existing graphics buffer to retrieve the skin weights for skinning.
* use the existing graphics buffer to retrieve the vertex deltas for blendshapes.
* DeformationsInPresentation and SkinnedMeshRendererConversion are now sealed.
* RegisterMaterialMeshSystem  is now internal instead of public.

### Removed

* PushMeshDataSystem, PushSkinMatrixSystem, PushBlendWeightSystem, InstantiateDeformationSystem, BlendShapeDeformationSystem and SkinningDeformationSystem are no longer part of the public API. Use DeformationsInPresentation instead.
* ENABLE_COMPUTE_DEFORMATIONS define, compute deformations are always enabled.

### Fixed

* Mesh and material indices not updating correctly if entity was created ad disabled and later enabled.
* Improved multi threaded load balancing of the Entities Graphics frustum culling Burst job
* Guard against overflow of static readonly int k_MaxSize for the deformation buffers.


## [0.14.0] - 2021-09-17

### Added

* Hybrid Renderer is automatically disabled when required support is not present or -nographics is given on the command line.

### Removed

* Hybrid Renderer V1 is now removed. V2 is the default renderer

### Fixed

* Fixed memory leak on frames where no data uploading happened.
* `HybridRendererSystem` was not correctly tracking all its component read/write dependencies.
* Warning about erroneous materials did not have enough information to act on them.

## [0.13.0] - 2021-03-15

### Removed

* Removed an unused internal struct that could cause compiler warnings.

### Fixed

* Entities that use the ambient light probe now have no SH components and use much less memory.
* Fixed a bug where global ambient probes were not always rendered correctly.

## [0.12.0] - 2021-01-26

### Added

* `CountNewChunksJob`, which is responsible for counting the number of new chunks since last frame, and filling the `newChunks` array.
* New #define DISABLE_HYBRID_LIGHT_PROBES to disable light probes globally to save memory.

### Changed

* (Root)LodRequirement component split to (Root)LodRange and (Root)LodWorldReferencePoint. The LOD ranges are created during conversion and the world reference point gets recalculated every frame. Splitting them allows performance optimizations.
* LODGroupWorldReferencePoint component added to LOD/HLOD groups. Allows us to transform the LOD pivot point once per group and storing it instead of doing repeated work per leaf entity. Total LODRequirementSystem performance increase up to 2x (when combined with the optimization above).
* Cache `GetComponentTypeHandle<>()` calls instead of doing them for each jobs.
* `UpdateAllHybridChunksJob` does not count the number of new chunks since last frame anymore.
* No longer track shader reflection version changes in `HybridRendererSystem`. A new system is now taking care of that in the `StructuralChangePresentationSystemGroup`
* No longer add/remove chunks in the `HybridRendererSystem`. A new system is now taking care of that in the `StructuralChangePresentationSystemGroup`
* Update minimum editor version to 2020.2.1f1-dots.3
*Loaded the Samples project in a 2020.2.1f1-dots.3 editor without any issues or console errors.
*Ran `Full CI [Entities] [project version]`

### Fixed

* LOD bitfield becoming stale when entities get removed or LOD components modified using LiveLink incremental update
* Fixed edge case bug in instance GPU allocation behavior.
* Improved GameObject conversion of light mapped objects during incremental conversion.
* Fixed a performance regression related to ambient probe update when probe grid was not available
* Do not perform structural changes in the `HybridRendererSystem` anymore.
* Static objects now always have per-object motion vectors disabled, regardless of MeshRenderer settings.



## [0.11.0] - 2020-11-13

### Added

* Frame queuing limiting solution to avoid hazards in the GPU uploader
* Hybrid V2 should now render objects with errors (e.g. missing or broken material) as bright magenta when the used SRP contains compatible error shaders, and display warnings.
* Support for lightmaps in hybrid renderer. You will need to bake with subscenes open, upon closing the lightmaps will be converted into the subscene. (Note: Requires release 10.1.0 of graphics packages).
* Support for lightprobes in hybrid renderer. Entities can dynamically look up the the current ambient probe or probe grid. (Note: Requires release 10.1.0 of graphics packages).
* Added error message when total used GPU memory is bigger than some backends can handle (1 GiB)
* HybridBatchPartition shared component that can force entities into separate batches.
* It is now possible to override DOTS instanced material properties using `ISharedComponentData`.
* RenderMeshDescription and RenderMeshUtility.AddComponent APIs to efficiently create Hybrid Rendered entities.

### Changed

* Log warning instead of error message when shader on SMR does not support DOTS Skinning
* Update minimum editor version to 2020.1.2f1

### Fixed

* Fixed float2 and float3 material properties like HDRP emissive color to work correctly.
* GPU buffer now grows by doubling, so initial startup lag is reduced.
* GPU resources are now cleaned up better in case of internal exceptions, leading to less errors in subsequent frames.
* Hybrid Renderer forces entities using URP and HDRP transparent materials into separate batches, so they are rendered in the correct order and produce correct rendering results.
* Fixed a bug with motion vector parameters not getting set correctly.
* HLOD conversion code now properly handles uninitialized components
* Removed internal frame queuing and replace it with frame fencing. Hybrid renderer will now longer wait for GPU buffers to be available, making it easier to see if you are GPU or CPU bound and avoiding some potential deadlocks.
* Disable deformation systems when no graphics device is present instead of throwing error.
* Fixed a bug with converting ambient light probe settings from GameObjects.

## [0.10.0] - 2020-09-24

### Added

* Error message when trying to convert SkinnedMeshRenderer that is using a shader that does not support skinning.

### Removed

* HybridRendererSettings asset was removed since memory management for the hybrid renderer data buffer is now automatic.

### Fixed

* Fixed missing mesh breaking subscene conversion
* Fixed chunk render bounds getting stale when RenderMesh shared component is changed.
* Improved Hybrid V2 memory usage during GPU uploading.
* Chunk render bounds getting stale when RenderMesh shared component is changed.
* Reduced Hybrid V2 peak memory use when batches are deleted and created at the same time.

## [0.9.0] - 2020-08-26

### Added

* Added: Hybrid component conversion support for: ParticleSystem and Volume+collider pairs (local volumes).
* Hybrid component conversion support for: ParticleSystem and Volume+collider pairs (local volumes).

### Fixed

* Fixed parallel for checking errors in Hybrid Renderer jobs.
* Fixed parallel for checking errors in occlusion jobs.


## [0.8.0] - 2020-08-04


### Changed

* Changed SkinnedMeshRendererConversion to take the RootBone into account. The render entities are now parented to the RootBone entity instead of the SkinnedMeshRenderer GameObject entity. As a result the RenderBounds will update correctly when the root bone is transformed.
* Changed SkinnedMeshRendererConversion to compute the SkinMatrices in SkinnedMeshRenderer's root bone space instead of worldspace.

### Fixed

* Fixed the Hybrid V2 uploading code not supporting more than 65535 separate upload operations per frame.
* Fixed render bounds being offset on converted SkinnedMeshRenderers.
* Partially fixed editor picking for Hybrid V2. Picking should now work in simple cases.
* Fixed a memory leak in the HeapAllocator class used by Hybrid Renderer.



## [0.7.0] - 2020-07-10

### Added

* Added support for controling persistent GPU buffer sizes through project settings

### Changed

* Updated minimum Unity Editor version to 2020.1.0b15 (40d9420e7de8)

### Fixed

* Improved hashing of the RenderMesh component.
* Fixed blendshapes getting applied with incorrect weights when the blendshapes are sparse.

### Known Issues

* This version is not compatible with 2020.2.0a17. Please update to the forthcoming alpha.


## [0.6.0] - 2020-05-27

### Added

* Added support for Mesh Deformations using compute shaders.
* Added support for sparse Blendshapes in the compute deformation system.
* Added support for Skinning using sparse bone weights with n number of influences in the compute deformation system.
* Added support for storing matrices as 3x4 on the GPU side. This will used for SRP 10.x series of packages and up.
* Added support for ambient probe environment lighting in URP.

### Changed

* Updated minimum Unity Editor version to 2020.1.0b9 (9c0aec301c8d)

### Fixed

* Fix floating point precision issue in vertex shader skinning.
* Fixed culling of hybrid lights in SceneView when using LiveLink (on 2020.1).

## [0.5.1] - 2020-05-04

### Changed

* Updated dependencies of this package.


## [0.5.0] - 2020-04-24

### Changed

Changes that only affect *Hybrid Renderer V2*:
* V2 now computes accurate AABBs for batches.
* V2 now longer adds WorldToLocal component to renderable entities.

Changes that affect both versions:
* Updated dependencies of this package.

### Deprecated

* Deprecated `FrozenRenderSceneTagProxy` and `RenderMeshProxy`. Please use the GameObject-to-Entity conversion workflow instead.

### Fixed

* Improved precision of camera frustum plane calculation in FrustumPlanes.FromCamera.
* Improved upload performance by uploading matrices as 4x3 instead of 4x4 as well as calculating inverses on the GPU
* Fixed default color properties being in the wrong color space


## [0.4.2] - 2020-04-15

### Changes

* Updated dependencies of this package.


## [0.4.1] - 2020-04-08

### Added (Hybrid V2)

* DisableRendering tag component for disabling rendering of entities

### Changed

* Improved hybrid.renderer landing document. Lots of new information.

### Fixed

* Fixed shadow mapping issues, especially when using the built-in renderer.

### Misc

* Highlighting additional changes introduced in `0.3.4-preview.24` which were not part of the previous changelogs, see below.


## [0.4.0] - 2020-03-13

### Added (All Versions)

* HeapAllocator: Offset allocator for sub-allocating resources such as NativeArrays or ComputeBuffers.

### Added (Hybrid V2)

Hybrid Renderer V2 is a new experimental renderer. It has a significantly higher performance and better feature set compared to the existing hybrid renderer. However, it is not yet confirmed to work on all platforms. To enable Hybrid Renderer V2, use the `ENABLE_HYBRID_RENDERER_V2` define in the Project Settings.

* HybridHDRPSamples Project for sample Scenes, unit tests and graphics tests.
* HybridURPSamples Project for sample Scenes, unit tests and graphics tests.
* MaterialOverride component: User friendly way to configure material overrides for shader properties.
* MaterialOverrideAsset: MaterialOverride asset for configuring general material overrides tied to a shader.
* SparseUploader: Delta update ECS data on GPU ComputeBuffer.
* Support for Unity built-in material properties: See BuiltinMaterialProperties directory for all IComponentData structs.
* Support for HDRP material properties: See HDRPMaterialProperties directory for all IComponentData structs.
* Support for URP material properties: See URPMaterialProperties directory for all IComponentData structs.
* New API (2020.1) to directly write to ComputeBuffer from parallel Burst jobs.
* New API (2020.1) to render Hybrid V2 batches though optimized SRP Batcher backend.

### Changes (Hybrid V2)

* Full rewrite of RenderMeshSystemV2 and InstancedRenderMeshBatchGroup. New code is located at `HybridV2RenderSystem.cs`.
* Partial rewrite of culling. Now all culling code is located at `HybridV2Culling.cs`.
* Hybrid Renderer and culling no longer use hash maps or IJobNativeMultiHashMapVisitKeyMutableValue jobs. Chunk components and chunk/forEach jobs are used instead.
* Batch setup and update now runs in parallel Burst jobs. Huge performance benefit.
* GPU persistent data model. ComputeBuffer to store persistent data on GPU side. Use `chunk.DidChange<T>` to delta update only changed data. Huge performance benefit.
* Per-instance shader constants are no longer setup to constant buffers for each viewport. This makes HDRP script main thread cost significantly smaller and saves significant amount of CPU time in render thread.

### Fixed

* Fixed culling issues (disappearing entities) 8000+ meters away from origin.
* Fixes to solve chunk fragmentation issues with ChunkWorldRenderBounds and other chunk components. Some changes were already included in 0.3.4 package, but not documented.
* Removed unnecessary reference to Unity.RenderPipelines.HighDefinition.Runtime from asmdef.
* Fixed uninitialized data issues causing flickering on some graphics backends (2020.1).

### Misc

* Highlighting `RenderBounds` component change introduced in `0.3.4-preview.24` which was not part of the previous changelogs, see below.


## [0.3.5] - 2020-03-03

### Changed

* Updated dependencies of this package.


## [0.3.4] - 2020-02-17

### Changed

* Updated dependencies of this package.
* When creating entities from scratch with code, user now needs to manually add `RenderBounds` component. Instantiating prefab works as before.
* Inactive GameObjects and Prefabs with `StaticOptimizeEntity` are now correctly treated as static
* `RenderBoundsUpdateSystem` is no longer `public` (breaking)
* deleted public `CreateMissingRenderBoundsFromMeshRenderer` system (breaking)


## [0.3.3] - 2020-01-28

### Changed

* Updated dependencies of this package.


## [0.3.2] - 2020-01-16

### Changed

* Updated dependencies of this package.


## [0.3.1] - 2019-12-16

**This version requires Unity 2019.3.0f1+**

### Changes

* Updated dependencies of this package.


## [0.3.0] - 2019-12-03

### Changes

* Updated dependencies of this package.


## [0.2.0] - 2019-11-22

**This version requires Unity 2019.3 0b11+**

### New Features

* Added support for vertex skinning.

### Fixes

* Fixed an issue where disabled UnityEngine Components were not getting ignored when converted via `ConvertToEntity` (it only was working for subscenes).

### Changes

* Removed `LightSystem` and light conversion.
* Updated dependencies for this package.

### Upgrade guide

  * `Lightsystem` was not performance by default and the concept of driving a game object from a component turned out to be not performance by default. It was also not maintainable because every property added to lights has to be reflected in this package.
  * `LightSystem` will be replaced with hybrid entities in the future. This will be a more clean uniform API for graphics related functionalities.


## [0.1.1] - 2019-08-06

### Fixes

* Adding a disabled tag component, now correctly disables the light.

### Changes

* Updated dependencies for this package.


## [0.1.0] - 2019-07-30

### New Features

* New `GameObjectConversionSettings` class that we are using to help manage the various and growing settings that can tune a GameObject conversion.
* New ability to convert and export Assets, which is initially needed for Tiny.
  * Assets are discovered via `DeclareReferencedAsset` in the `GameObjectConversionDeclareObjectsGroup` phase and can then be converted by a System during normal conversion phases.
  * Assets can be marked for export and assigned a guid via `GameObjectConversionSystem.GetGuidForAssetExport`. During the System `GameObjectExportGroup` phase, the converted assets can be exported via `TryCreateAssetExportWriter`.
* `GetPrimaryEntity`, `HasPrimaryEntity`, and the new `TryGetPrimaryEntity` all now work on `UnityEngine.Object` instead of `GameObject` so that they can also query against Unity Assets.

### Upgrade guide

* Various GameObject conversion-related methods now receive a `GameObjectConversionSettings` object rather than a set of misc config params.
  * `GameObjectConversionSettings` has implicit constructors for common parameters such as `World`, so much existing code will likely just work.
  * Otherwise construct a `GameObjectConversionSettings`, configure it with the parameters you used previously, and send it in.
* `GameObjectConversionSystem`: `AddLinkedEntityGroup` is now `DeclareLinkedEntityGroup` (should auto-upgrade).
* The System group `GameObjectConversionDeclarePrefabsGroup` is now `GameObjectConversionDeclareObjectsGroup`. This cannot auto-upgrade but a global find&replace will fix it.
* `GameObjectConversionUtility.ConversionFlags.None` is gone, use 0 instead.

### Changes

* Changing `entities` dependency to latest version (`0.1.0-preview`).


## [0.0.1-preview.13] - 2019-05-24

### Changes

* Changing `entities` dependency to latest version (`0.0.12-preview.33`).


## [0.0.1-preview.12] - 2019-05-16

### Fixes

* Adding/fixing `Equals` and `GetHashCode` for proxy components.


## [0.0.1-preview.11] - 2019-05-01

Change tracking started with this version.
