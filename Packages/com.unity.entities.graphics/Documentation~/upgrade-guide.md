# Upgrade to Entities Graphics version 1.0

To upgrade to Entities Graphics package version 1.0, you need to do the following: 

* Remove usage of HLOD.
* Replace runtime usage of `RenderMesh` with `RenderMeshArray`.
* Replace usage of rendering settings in `RenderMesh` with rendering settings in `RenderFilterSettings`.

## Remove HLOD

HLOD is a feature created specifically for the MegaCity demo and has been removed from the 1.0 release of the Entities Graphics package. To upgrade Entities Graphics to 1.0, you must remove any usage of HLOD in your project.

## Replace runtime usage of RenderMesh with RenderMeshArray

Previously, Entities Graphics used the RenderMesh shared component at runtime to create batches for rendering. Entities Graphics 1.0 replaces this with the RenderMeshArray shared component and the MaterialMeshInfo component.

The `RenderMesh` component still exists as a convenient intermediate step during entity baking, but Entities Graphics ignores it at runtime. To upgrade Entities Graphics to 1.0, you must update any code that uses `RenderMesh` or the `RenderMeshUtility.AddComponents` APIs to use the new `RenderMeshArray` versions. For more information, refer to [Runtime entity creation](runtime-entity-creation.md).

## Replace usage of rendering settings in RenderMesh with rendering settings in RenderFilterSettings

Entities Graphics 1.0 moves many rendering settings from the `RenderMesh` shared component to the `RenderFilterSettings` shared component. This includes settings such as rendering layer settings, motion vector rendering settings, and shadow rendering settings. To upgrade Entities Graphics to 1.0, you must update any code that uses the settings in `RenderMesh` to use the settings in `RenderFilterSettings`.

For a full list of changes and updates in this version, refer to the Entities Graphics package [changelog](xref:changelog).