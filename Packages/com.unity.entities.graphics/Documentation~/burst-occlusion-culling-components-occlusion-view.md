# Occlusion View component

An Occlusion View MonoBehaviour component specifies which cameras, lights, and reflection probes use Burst Occlusion Culling. It also configures the size of the buffer to use for occlusion culling calculations which affects the resource intensity and precision of the calculations.

## Occlusion View Inspector reference

| **Property**                | **Description**                                              |
| --------------------------- | ------------------------------------------------------------ |
| **Occlusion Enabled**       | Controls whether the attached cameras, light, or reflection probes uses Burst Occlusion Culling. |
| **Occlusion Buffer Width**  | The width of the buffer to use for occlusion culling calculations. This value should always be a multiple of 16. |
| **Occlusion Buffer Height** | The height of the buffer to use for occlusion culling calculations. This value should always be a multiple of 16. |

## Additional resource

* [Occluder component](burst-occlusion-culling-components-occluder.md)
* [Optimize occlusion views](burst-occlusion-culling-optimize.md#optimize-occlusion-views)