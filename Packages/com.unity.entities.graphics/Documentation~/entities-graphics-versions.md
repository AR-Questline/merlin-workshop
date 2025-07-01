# Entities Graphics feature matrix

Entities Graphics is in active development. The goal is to reach full High Definition Render Pipeline (HDRP) and Universal Render Pipeline (URP) feature coverage. The only exceptions are select old deprecated features in each render pipeline that now have modern replacements.

The following table compares Entities Graphics feature support between HDRP and URP:

| **Feature**                       | **URP**                                     | **HDRP**                               |
| --------------------------------- | ------------------------------------------- | -------------------------------------- |
| **Material property overrides**   | Yes                                         | Yes                                    |
| **Built-in property overrides**   | Yes                                         | Yes                                    |
| **Shader Graph**                  | Yes                                         | Yes                                    |
| **Lit shader**                    | Yes                                         | Yes                                    |
| **Unlit shader**                  | Yes                                         | Yes                                    |
| **Decal shader**                  | N/A                                         | Yes                                    |
| **Particle shader**               | Yes                                         | N/A                                    |
| **LayeredLit shader**             | N/A                                         | Yes                                    |
| **LitTessellation shader**        | N/A                                         | Yes                                    |
| **RenderLayer**                   | Yes                                         | Yes                                    |
| **TransformParams**               | Yes                                         | Yes                                    |
| **DisableRendering**              | Yes                                         | Yes                                    |
| **Motion blur**                   | Yes                                         | Yes                                    |
| **Temporal AA**                   | Yes                                         | Yes                                    |
| **Sun light**                     | Yes                                         | Yes                                    |
| **Point + spot lights**           | Yes                                         | Yes                                    |
| **Ambient probe**                 | Yes                                         | Yes                                    |
| **Light probes**                  | Yes                                         | Yes                                    |
| **Reflection probes**             | Yes                                         | Yes                                    |
| **Lightmaps**                     | Yes                                         | Yes                                    |
| **LOD crossfade**                 | No                                          | No                                     |
| **Custom pass material override** | Yes                                         | Yes                                    |
| **Sorted Transparencies**         | Yes                                         | Yes                                    |
| **Color Space**                   | Only Linear color space is supported        | Only Linear color space is supported   |
| **Rendering Path**                | Only Forward+ is supported                  | Yes                                    |
| **Burst Occlusion culling**       | Experimental<br><br>Note: Must enable define and configure scene with occluders. See [Burst Occlusion Culling](burst-occlusion-culling.md).           | Experimental<br><br>Note: Must enable define and configure scene with occluders. See [Burst Occlusion Culling](burst-occlusion-culling.md). |
| **Skinning & deformations**       | Experimental<br><br>Note: Must use Compute Deformation node in Shader Graph. See [Mesh Deformations](mesh_deformations.md).                            | Experimental<br><br>Note: Must use Compute Deformation node in Shader Graph. See [Mesh Deformations](mesh_deformations.md). |
| **Texture Streaming**             | No                                          | No                                     |
| **Streaming Virtual Texturing**   | N/A                                         | No                                     |
| **Ray-tracing**                   | N/A                                         | No                                     |
| **Other Graphics components**     | Some graphics components are converted to companion components which can be put inside a subscene. See [Companion Components](companion-components.md). | Some graphics components are converted to companion components which can be put inside a subscene. See [Companion Components](companion-components.md). |

> [!NOTE]
> Entities Graphics feature support also depends on feature status of Universal Render Pipeline (URP) and High Definition Render Pipeline (HDRP). See [Render pipeline feature comparison](xref:render-pipelines-feature-comparison).
