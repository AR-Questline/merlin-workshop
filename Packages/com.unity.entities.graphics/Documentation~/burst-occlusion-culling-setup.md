# Set up Burst Occlusion Culling

To set up Burst Occlusion Culling in your Unity project:

1. Enable the feature.
2. Enable and configure Burst Occlusion Culling for individual cameras, lights, and reflection probes.
3. Configure some entities to be occluders.

## Enable Burst Occlusion Culling

The first step to set up Burst Occlusion Culling is to enable the feature for your project. To do this:

1. Set the `ENABLE_UNITY_OCCLUSION` custom scripting symbol. For information on how to do this, refer to [Custom scripting symbols](xref:CustomScriptingSymbols).
2. Ensure that [Burst](https://docs.unity3d.com/Packages/com.unity.burst@latest/index.html) is enabled. To do this, select **Jobs** > **Burst** > **Enable Compilation**.
3. Select **Occlusion** > **Enable**.
4. Burst Occlusion Culling requires the target CPU to support SSE4 instructions. To be able to build a Unity Player, go to **Edit** > **Project Settings** > **Burst AOT Settings** and set **Target CPU architectures** to **SSE4**.

## Configure per-view occlusion culling

You can enable and configure Burst Occlusion Culling on a per-camera, per-light, and per reflection probe basis. By default, only the [main camera](xref:UnityEngine.Camera.main) uses Burst Occlusion Culling. To enable Burst Occlusion Culling for a camera, light, and reflection probe, add the **Occlusion View** component and enable the **Occlusion Enable** property. The Occlusion View component also controls the resolution of the occlusion buffer for the camera, light, or reflection probe. The occlusion buffer resolution affects the resource intensity of the occlusion culling calculations. For more information about configuration options and performance, refer to [Optimize Burst Occlusion Culling](burst-occlusion-culling-optimize.md)

## Create occluders

By default, [render entities](burst-occlusion-culling-overview.md#how-burst-occlusion-culling-works) are occludees but not occluders. To make an entity able to occlude other entities, you must set it to be an occluder. For performance reasons, not every entity should be an occluder. For information on how to choose which entities to make occluders, see [How to choose occluders](burst-occlusion-culling-overview.md#how-to-choose-occluders).

To set up an entity as an occluder:

1. Create the occluder mesh. This is a low-poly mesh that's inscribed within the original mesh. An inscribed mesh is one that's always perfectly inside the volume of the original mesh. You can author an occluder mesh via an external 3D modelling application or with [ProBuilder](https://docs.unity3d.com/Packages/com.unity.probuilder@latest/index.html).
2. If you created the occluder mesh outside of Unity, import it into your project.
3. Select the authoring GameObject and view it in the Inspector.
4. Add an [Occluder component](burst-occlusion-culling-components-occluder.md) to the authoring GameObject.
5. In the Occluder component, set **Mesh** to the occluder mesh asset.
6. If necessary, modify the offset position, scale, and rotation of the occluder mesh so that it lines up with the original mesh. You can use the [Rendering Debugger](burst-occlusion-culling-debug.md) to visualize your changes to position, scale and rotation.

## Additional resources

- [Optimize Burst Occlusion Culling](burst-occlusion-culling-optimize.md)
