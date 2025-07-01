# Occluder component

An Occluder MonoBehaviour component specifies which entities are occluders. After you attach this component to a GameObject, the [baking process](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/manual/baking.html) attaches the relevant occluder ECS components to the converted entity.

> [!TIP]
> The [Scene view](xref:UsingTheSceneView) provides a visual [Gizmo](xref:GizmosMenu) that displays a wireframe of the assigned occluder mesh using the current settings. This helps you to position, rotate, and scale the occluder mesh, if necessary, so it's inscribed within the visual mesh.

## Occluder Inspector reference

| **Property**       | **Description**                                              |
| ------------------ | ------------------------------------------------------------ |
| **Mesh**           | The mesh to use for occlusion culling calculations. This should be a low-poly mesh that is inscribed within the visual mesh. |
| **Local Position** | The position offset to apply to the occlusion mesh.          |
| **Local Rotation** | The rotation offset to apply to the occlusion mesh.          |
| **Local Scale**    | The scale offset to apply to the occlusion mesh.             |

## Additional resource

* [Occlusion View component](burst-occlusion-culling-components-occlusion-view.md)