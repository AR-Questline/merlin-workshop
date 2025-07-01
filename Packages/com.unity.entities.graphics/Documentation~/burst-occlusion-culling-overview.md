# Burst Occlusion Culling overview

The Burst Occlusion Culling system disables rendering for entities that are hidden behind other entities. This reduces the amount of data that Unity uploads to the GPU every frame and the amount of unnecessary work that the GPU must do.

## How the Burst Occlusion Culling system works

The Burst Occlusion Culling system takes a viewpoint, either a camera, reflection probe, or a light with shadows enabled, and calculates which entities are visible and which are hidden behind other entities. Unity then uses this information to only send visible entities to the GPU for rendering. This improves performance if the added CPU overhead from the time it takes to calculate which entities are visible is less than the time it would take to render the hidden entities on the GPU. The hidden entities render time includes the CPU draw overhead, the upload time from the CPU to GPU, and the GPU time it would take to render the hidden entities.

The Burst Occlusion Culling system uses the following concepts:

* **Occlusion View**: Any viewpoint for which the system is enabled to calculate occlusion culling.
* **Occluder**: An entity that can hide other entities. Any entity that has an occluder component added with a low resolution fully inscribed mesh which is used to determine the screen space coverage of the original mesh and used to test if other meshes are fully occluded by this mesh.
* **Occludee**: Any entity that can be hidden behind occluders and which may be culled if it's fully hidden by an occluder entity.
* **Inscribed Mesh**: A mesh is fully inscribed if it's fully enclosed within the original mesh. This means that when the inscribed mesh is projected and rendered into screen space, it's always within the area that's rendered for the original mesh. This provides a view consistent area coverage of the original mesh that can be used to determine if other objects are fully hidden behind it.

For each occlusion view, the Burst Occlusion Culling system calculates which occluders hide which occludees, and for any remaining occludees that are not hidden, the render pipeline will handle those as normal and dispatch them to the GPU for rendering.

Entities that use the Mesh Renderer component with **Dynamic Occlusion** enabled at [author time](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/manual/editor-authoring-runtime.html) are automatically occludees. To fully setup Burst Occlusion Culling in your scene, you additionally need to specify which entities are occluders, and which viewpoints are occlusion views. For help on how to decide which entities should be occluders, refer to [How to choose occluders](#how-to-choose-occluders). For help of which cameras, reflections probes, and lights should be occlusion views, refer to [How to choose occlusion views](#how-to-choose-occlusion-views).

For performance reasons, the culling system is not intended to use the same meshes in its culling calculations that the rendering system uses to draw entities. Instead, each occluder entity allows you to specify an additional mesh for the culling system to use. This occlusion mesh must be completely inscribed within the original mesh to avoid artifacts such as visible popping (which is where objects appear and disappear visibly on-screen). The occluder mesh must be completely inscribed within the original mesh for the occlusion culling system to be fully conservative. Otherwise, artifacts such as visible popping can occur (which is where objects appear and disappear visibly on-screen). For optimal performance, the Occlusion Mesh should also have a minimal triangle count.

## When to use Burst Occlusion Culling

Burst Occlusion Culling isn't appropriate for every application or scene. Scenes with many unique objects (with unique meshes or materials) that produce a lot of overdraw are perfect for Burst Occlusion Culling. Examples of this type of scene include large open worlds, dense cities, or interiors with separate rooms.

Entities graphics can render instanced objects very quickly so it's often not beneficial to calculate which instanced objects are or aren't occluded and instead pass them all to the GPU to render. This is because the overhead of the occlusion culling calculations can exceed the overhead saved by reducing the number of instanced objects to draw.

If there is a mix of unique and instanced objects in a scene, you can enable Burst Occlusion Culling for the scene, but make the instanced objects not occludees (disable **Dynamic Occlusion** on their Mesh Renderer component). This makes Burst Occlusion Culling optimize the draw submission for unique objects without wasting resources processing the instanced objects.

## How to choose occluders

Occlusion culling gives performance improvements if the overhead of the occlusion culling process is less than the overhead saved by reducing the number of entities to draw. The more occluders there are and the more complex their shape, the more resource intensive the occlusion culling process is. This means that it's important to choose which entities to set as occluders. An entity's size and shape decide its suitability as an occluder. 

Entities likely to be suitable occluders are:

* Entities that fill a significant part of the screen. For example, in a first-person perspective, items in the character’s hands are close to the camera. Also buildings and other large objects that the character can approach, that move toward the character, or that move around the scene.
* Entities that are likely to occlude other entities. For example, a large mountain or building located in the center of the scene, or the walls in the interior of a building.

Entities likely to be unsuitable occluders are:

* Entities unlikely to fill a significant part of the screen. For example, a small book that the camera will never go very close to, or large objects that will always be far away from the camera and will therefore have small screen-space coverage.
* Entities that are unlikely to occlude other entities. For example, mountains at the far distant edge of  a world, or a road mesh where there are few/no objects below the ground. Objects that are thin from more than one perspective are also unlikely to occlude other entities.
* Entities that have a complex and non-convex shape. For example, hands or a tree with many branches and leaves.

## How to choose occlusion views

By default, every frustum view in your scene (except light probes) is an implicit occlusion view with a 512x512 resolution. To manually configure Burst Occlusion Culling for each view, you can add an Occlusion View component to it. With this component you can explicitly set the resolution of the occlusion buffer and enable/disable Burst Occlusion Culling for the view.

Not every viewpoint will benefit from occlusion culling, as it dependent on scene content visible in that view, so it's important to carefully choose which viewpoints to set as occlusion views.

Views likely to be suitable occlusion view are:

* Cameras that view many or complex (in either material or resolution) objects that are large in screen-space area.

Views likely to be unsuitable occlusion views

* Frustum views that have few and/or small or low complexity objects in the view.
* Frustum views which already benefit significantly from frustum culling and/or instancing.

**Note**: For shadow casting lights occlusion culling, material settings don’t have an impact on performance. This means you shouldn't consider complex materials when deciding whether to use Burst Occlusion Culling for lights.

**Note**: The resolution of the occlusion buffer can affect the workload distribution across jobs, so just reducing the resolution may not reduce the CPU overhead. Reducing the resolution can also reduce the total amount of hidden geometry, which can reduce the GPU performance gains. If all the occluder triangles for the scene are fairly evenly distrubted across screen tiles, or no single screen tile is overly high in occluder triangle geometry density than all other tiles, then reducing the resolution of the occlduer buffer should reduce the CPU overhead.

## Additional resources

- [Set up Burst Occlusion Culling](burst-occlusion-culling-setup.md)
