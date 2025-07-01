# Rendering Debugger Culling tab reference

The **Culling** tab in the [Rendering debugger](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/Render-Pipeline-Debug-Window.html) includes debugging options and visualizations to help you investigate Burst Occlusion Culling issues.

<table>
  <thead>
    <tr>
      <th><strong>Property</strong></th>
      <th colspan="2"><strong>Description</strong></th>
    </tr>
  </thead>
  <tbody>
<tr>
  <td><strong>Freeze Occlusion</strong></td>
  <td colspan="2">Indicates whether to temporarily pause the occlusion culling system and maintain the current occlusion results.<br/><br/>This is useful to confirm that objects you expect to be occluded during a single frame from a particular point-of-view are.<br/><br/>For an example of how to use this: enable **Freeze Occlusion** and identify some objects that you expect to be occluded from the current camera’s current point-of-view. Move the camera to a different position to view those objects and visually confirm what was occluded and what wasn't.<br/><br/>**Note**: The culling results include both occlusion culling and frustum culling.</td>
</tr>
<tr>
  <td><strong>Pinned View</strong></td>
  <td colspan="2">Specifies a view to use the occlusion culling results from. Unity uses the occlusion results from the pinned view in all other views. For example, other cameras, reflection probes, and the Scene view camera. When enabled, all views won't render objects that are occluded from the perspective of the pinned view, even if the objects aren't occluded from the perspective of the other views.<br/><br/>This is useful to view occlusion culling results over time.<br/><br/>For an example of how to use this: pin the [main camera](xref:UnityEngine.Camera.main) in your scene, [move the Scene view camera](xref:SceneViewNavigation) around to inspect which objects are hidden. Then move the main camera around to update the culling results.<br/><br/>Select **None** to disable view pinning.<br/><br/>**Note**: The culling results include both occlusion culling and frustum culling.</td>
</tr>
<tr>
  <td rowspan="7"><strong>Debug Mode</strong></td>
  <td colspan="2">Specifies an occlusion culling debug visualization to render.</td>
</tr>
<tr>
  <td><strong>None</strong></td>
  <td>Disables occlusion culling debug visualizations.</td>
</tr>
<tr>
  <td><strong>Depth</strong></td>
  <td>Visualizes the occlusion depth buffer for the camera viewport. This view of the scene only contains occluders. The view also displays the occlusion mesh for the occluders; not their visual mesh.
</td>
</tr>
<tr>
  <td><strong>Test</strong></td>
  <td>Visualizes the occlusion depth buffer and shows occluded entities as red squares. This is the same view as **Depth** with the addition of red squares that show the bounds of the occludees that were determined to be hidden and thus not sent to the GPU for rendering in the current frame
</td>
</tr>
<tr>
  <td><strong>Mesh</strong></td>
  <td>Visualizes the occluder meshes and uses a unique color per mesh. This helps you to view how each configured occlusion mesh aligns with the render mesh and to confirm if the mesh is inscribed.</td>
</tr>
<tr>
  <td><strong>Bounds</strong></td>
  <td>Visualizes the axis-aligned bounding boxes for all meshes in the viewport.</td>
</tr>
<tr>
  <td><strong>Inverted</strong></td>
  <td>Inverts the logic Unity uses to display culling results. When enabled, Unity shows culled objects and hides not-culled objects. **Note**: Unity also renders all objects that aren't occluders or occludees. </td>
</tr>
</tbody>
</table>

## Additional resources

- [Universal Render Pipeline Rendering Debugger](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest?subfolder=/manual/features/rendering-debugger.html)
- [High Definition Render Pipeline Rendering Debugger](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/Render-Pipeline-Debug-Window.html)
