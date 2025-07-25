# Screen Space Refraction override

Screen space refraction uses the color buffer or Reflection Probes to produce a refraction effect.

HDRP uses screen space refraction by default if you set a Material's [Surface Type](Surface-Type.md) to **Transparent**. For information about how screen space refraction works in HDRP, or to turn refraction off, see [Refraction in HDRP](Override-Screen-Space-Refraction.md).

The **Screen Space Refraction** override controls **Screen Weight Distance**, which sets how quickly screen space refractions fades from sampling colors from the color buffer to sampling colors from the next level of the [reflection and refraction hierarchy](how-hdrp-calculates-color-for-reflection-and-refraction.md).

Increase **Screen Weight Distance** to reduce visible seams on an object between refracted colors from the screen, and refracted colors from probes or the Skybox.

## Using Screen Weight Distance

To use this setting, you must enable it on a [Volume](understand-volumes.md), as follows:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component.
2. In the Inspector for this object, select **Add Override** > **Lighting** > **Screen Space Refraction**.

![Comparison image of two outdoor scenes. In the first image, a refraction effect is visible. In the second image, the colours appear normal.](Images/screen-weight-distance.png)<br/>
In the refractive cube on the left of the screen, **Screen Weight Distance** affects the edges of the screen where HDRP fades from using the color buffer to using Reflection Probes.

## Properties

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Screen Weight Distance** |   Adjust this value to set how quickly HDRP fades from sampling colors from the color buffer to sampling colors from the next level of the [reflection and refraction hierarchy](how-hdrp-calculates-color-for-reflection-and-refraction.md). Use **Screen Weight Distance** to reduce visible seams between refracted colors from the screen, and refracted colors from probes or the Skybox. |

You can also use the [Volume Scripting API](Volumes-API.md) to change **Screen Weight Distance**.

## Additional resources

- [Transparent and translucent materials](transparent-and-translucent-materials.md)
