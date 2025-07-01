## Mesh deformations (Experimental Feature)

> [!IMPORTANT]
> This version of mesh deformations is experimental. This means that it isn't yet ready to use for production and parts of the implementation and API will change. Also, this version doesn't support some features that exist for the Skinned Mesh Renderer component.

This page describes how to use skinning and blendshapes to deform meshes. This is similar to what the [Skinned Mesh Renderer](https://docs.unity3d.com/Manual/class-SkinnedMeshRenderer.html) component does. 

To use mesh deformations in your Unity Project, you first need to set up your project to support them. Then, to control deformations, write to either the [Skin Matrix](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Deformations.SkinMatrix.html) or [Blend Shape](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Deformations.BlendShapeWeight.html) ECS component. For examples on how to do this, refer to the scenes `SampleScenes/5. Deformation/MeshDeformations` and `SampleScenes/5. Deformation/SkinnedCharacter` in assets folder of [URPSamples](https://github.com/Unity-Technologies/EntityComponentSystemSamples/tree/master/GraphicsSamples/URPSamples) and [HDRPSamples](https://github.com/Unity-Technologies/EntityComponentSystemSamples/tree/master/GraphicsSamples/HDRPSamples).

## Setup

To use mesh deformations in your Unity Project:

1. Enable support for mesh deformations in your [Unity Project](#project-setup).
2. Create a [material to use with the deformed mesh](#material-setup).
3. Create a [mesh to apply the material to](#mesh-setup).

### Project setup

Before you can use mesh deformations in your Unity project, you must set up your Unity Project to support this feature. To do this:

1. Make sure your Unity Project uses the Entities Graphics package. For information on how to install packages, see the [Package Manager manual](https://docs.unity3d.com/Manual/upm-ui.html).
2. If you intend to use per-vertex motion vectors, go to Project Settings (menu: **Edit** > **Project Settings**) and, in the Player section, add `ENABLE_DOTS_DEFORMATION_MOTION_VECTORS` to **Scripting Define Symbols**. Unity currently only supports this when using the High Definition Render Pipeline. **Note**: To apply changes to the define, you must re-save any Shader Graphs.
3. Create a Skinned Mesh Renderer with compatible materials using the [Mesh setup](#mesh-setup) and [Material setup](#material-setup) steps.

When Unity [bakes](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/manual/baking.html) a GameObject or Prefab that contains a Skinned Mesh Renderer component into an entity, it adds the correct deformation ECS components. Furthermore, the deformation systems dispatch and apply the deformations to the mesh.

> [!NOTE]
> To create motion, write to the SkinMatrix and BlendShapeWeights ECS components.

### Material setup

After you set up your project to support mesh deformations, you can create a material that Entities Graphics can use to render mesh deformations. To do this:

1. Create a new Shader Graph and open it. You can use any Shader Graph from the High Definition Render Pipeline (HDRP) or the Universal Render Pipeline (URP).
2. Add the [Compute Deformation](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/Compute-Deformation-Node.html) node to the Shader Graph.
3. Connect the position, normal, and tangent outputs of the node to the vertex position, normal, and tangent slots in the master node respectively.
4. Save the Shader Graph.

### Mesh setup

After you create a material that supports mesh deformations, you can set up a mesh that Entities Graphics can deform using your material. To do this:

1. Select a GameObject or Prefab and make sure it uses the Skinned Mesh Renderer component, and not the Mesh Renderer component.
2. Make sure that the mesh has blendshapes and/or a valid bind pose and skin weights. If Unity doesn't detect the appropriate data, it displays an error in the Skinned Mesh Renderer component Inspector.
3. Assign the material you created in [Material setup](#material-setup) to all material slots on the Skinned Mesh Renderer.


### Vertex shader skinning

> [!IMPORTANT]
> Mesh deformations are compute shader based by default when using graphics entities. Vertex shader deformation workflows are not encouraged and will not be supported in the future.

Vertex shader skinning skins the mesh on the GPU in the vertex shader. To enable this, use the Linear Blend Skinning node instead of the Compute Node. Linear blend skinning only supports dense 4 bones per vertex and is not compatible with blend shapes or motion vectors.

> [!NOTE]
> When you use vertex shader skinning,  compute deformation still run in the background.


## Known limitations

- Not compatible with [Scene View Draw Modes](https://docs.unity3d.com/Manual/ViewModes.html), use Rendering Debugger for [Universal](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest?subfolder=/manual/features/rendering-debugger.html) and [High Definition](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/Render-Pipeline-Debug-Window.html) Render Pipelines respectively. 
- Some Shader Graph operations are not supported, for instance using a sub graph.
- Render bounds are not resized or transformed based on the mesh deformations.
- No frustum or occlusion culling, Unity processes mesh deformation for everything that uses it in the scene and its sub scenes.
- Deformed meshes can disappear or show in their bind pose when Unity renders them as GameObjects.
- Compute deformation performance varies based on GPU.
- Not compatible with VFX Graph.
- OpenGLCore is not supported on desktop in the experimental version.

## Feature comparison

|                                        		    | **Skinned Mesh Renderer** | **Entities Graphics** |
| ------------------------------------------------- | --------------------------| ----------------------|
| Linear Blend Skinning 							| Supported 				| Supported |
| Blend Shapes 										| Supported 				| Supported |
| Per Vertex Motion Vectors 						| [Supported](xref:UnityEngine.SkinnedMeshRenderer.skinnedMotionVectors) | Only in HDRP (With define) |
| Optional normals & tangents 						| Supported 				| --- |
| Resizeable render bounds based on animated pose 	| [Supported](xref:UnityEngine.SkinnedMeshRenderer.updateWhenOffscreen) | --- |
| Bake Mesh 										| [Supported](xref:UnityEngine.SkinnedMeshRenderer.BakeMesh(UnityEngine.Mesh)) | --- |
| Cloth Simulation 									| Supported 				| --- |
| Quality setting for limiting skin influences 		| Supported 				| --- |
| CPU Deformations 									| [Supported](xref:UnityEditor.PlayerSettings.gpuSkinning) | --- |
| Blend Shape Frames 								| Supported 				| --- |