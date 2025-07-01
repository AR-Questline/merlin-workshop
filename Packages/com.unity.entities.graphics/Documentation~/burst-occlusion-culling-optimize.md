# Optimize Burst Occlusion Culling

If Burst Occlusion Culling is a good fit for a scene (refer to [When to use Burst Occlusion Culling](burst-occlusion-culling-overview.md#when-to-use-burst-occlusion-culling)), you can configure it to be bespokely optimized for the scene. This page explains the different methods you can use to get the best performance out of Burst Occlusion Culling for a particular scene.

## Optimize occlusion views

The Burst Occlusion Culling system can use a different buffer resolution for each view it processes. A lower-resolution buffer is less resource-intensive to process but produces a less precise culling result. If a view doesn't require precise occlusion culling results, you can reduce the resolution of its occlusion buffer to increase the performance of the Burst Occlusion Culling process.

If an occlusion view uses a lower resolution buffer, the Burst Occlusion Culling system can misidentify some totally hidden objects as being visible. This means that the rendering system must unnecessarily process the objects. If you reduce the resolution of an occlusion view buffer, it's best practice to [profile](xref:Profiler) the scene to make sure that the reduced resolution doesn't degrade overall performance.

## Additional resources

- [Rendering Debugger Culling tab reference](burst-occlusion-culling-debug.md)
