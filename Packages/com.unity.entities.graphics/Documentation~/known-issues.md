# Known issues

Entities Graphics is a bridge between ECS for Unity and Unity's existing rendering architecture. As we progress towards consolidating ECS workflows, there are still some outstanding incompatibilities in existing workflows. This page describes the known issues and provides a workaround if one exists.

## Lighting

The [subscene](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/manual/conversion-subscenes.html) workflow in Unity's entity component system architecture is incompatible with some lighting features and workflows:

### Automatic lightmap generation

In GameObject-based Unity applications, you can enable the [auto-generate lightmaps](xref:UnityEngine.LightingSettings.autoGenerate) feature to automatically precompute lighting data for a scene when the scene data changes. This workflow is currently not supported with Subscenes. So, if you install Entities and Entities Graphics, Unity disables this option in the [Lighting window](xref:lighting-window) and you must instead click **Generate Lighting** to manually start the lightbaking process.	

### Baked lightmaps with shared subscenes

In entities-based applications, you can use the same subscene within multiple scenes. This helps you to reuse functionality and assets throughout your application. However, there is a lighting-related limitation to this workflow in that subscenes can only store and use a single baked lightmap.

Unity stores the baked lightmap for an subscene as part of the subscene itself, and each subscene can only store a single lightmap. This means if you bake lightmaps for a configuration of subscenes, the lightmaps for each one will only ever represent the entities from the configuration of subscenes at the time of the bake. If you unload an subscene, the lightmaps of the other subscenes will still show shadows for the entities in the unloaded subscene. If you load a new subscene, the lightmaps in the other subscenes won't show shadows from entities in the new subscene. For example:

1. You have two subscenes. One that contains a plane and one that contains a cube.
2. You bake the lighting and the result, from the perspective of the plane subscene, is a lightmap that shows a shadow from the cube cast onto the plane.
3. If you unload the subscene that contains the cube, the baked lighting for the plane subscene still shows the cube shadow.
4. If you load a new subscene that contains a sphere, the baked lighting for the plane subscene won't show a shadow for the sphere.

If you want to bake lightmaps and change which subscenes are loaded at runtime, use one of the following methods to work around this limitation:

* Use real-time lighting.
* Arrange the content of each subscene in a way that entities in one don't cast shadows into another. If this isn't possible, also consider combining subscenes that must cast shadows into one another.

### Directional light

If all of the directional lights are inside the subscene, ambient lighting will be missing from the editor's playmode and the player. Other lighting data e.g. max distance for cascade shadows will also be different compared to when directional light is outside the subscene.

### Lightmap / Fog modes in player

If a scene has fog enabled or has lightmap, and if the subscene does not have the same fog / lightmap settings, when making a player build, the subscene objects will be rendering wrong as they have wrong fog mode / lightmap modes as the shader variant is being stripped.

To workaround this issue:

1. Open ProjectSettings > Graphics > Shader Stripping
2. On Lightmap Modes and Fog Modes, select Custom 
3. Click on Import from current scene, or select the correct modes to make sure subscene objects have the correct fog and light modes to build shader variants

OR

1. Open the subscene and apply the same fog / lighting settings on the Lighting Panel as the original scene

## RenderTexture

If there a camera renders into a RenderTexture and this RenderTexture is assigned on a material on a MeshRenderer object inside the subscene, the RenderTexture will be incorrect in player. To workaround this, re-assign the RenderTexture to the entity material in runtime.

## Shader Stripping

Once the Entities Graphics package is installed, when building the player, the shader variants for DOTS_INSTANCING_ON will always be compiled and included in player build as they are not being stripped even if they are not in use. This could result in longer player build time and potentially increase runtime memory usage.

To workaround this, a scriptable shader stripping script can be created to strip any unused shader variants. See Stripping shader variants using Editor scripts section in [Shader variant stripping](xref:shader-variant-stripping).

## Companion Components

### ParticleSystem and VisualEffect

For ParticleSystem and VisualEffect (VFX) objects in subscene, preview is only available on SceneView if Preference > Entities > Scene View Mode is set to Authoring Data. GameView is always on “Runtime Data” mode, therefore particles / VFX preview is not available on GameView.

It is also known that ParticleSystem will not render the lights in the light module if ParticleSystem is being put in subscene or is converted to companion components.

### HDRP PlanarReflectionProbe

For HDRP PlanarReflectionProbe object in subscene, it is expected that the 'Maximum Planar Reflection Probes on Screen' property in the HDRP asset needs to be increased.

## Additional resources

* [Entities Graphics feature matrix](entities-graphics-versions.md)
