# Requirements and compatibility

This page contains information on system requirements and compatibility of the Entities Graphics package.

## Render pipeline compatibility

Entities Graphics requires a Scriptable Render Pipeline (SRP) to function.

| **Render pipeline**                        | **Compatibility**    |
| ------------------------------------------ | -------------------- |
| **Built-in Render Pipeline**               | Not supported        |
| **High Definition Render Pipeline (HDRP)** | Unity 2022 LTS       |
| **Universal Render Pipeline (URP)**        | Unity 2022 LTS       |

For **Universal Render Pipeline (URP)**, only Forward+ rendering path is supported.

For more information about the supported feature set, see [Entities Graphics feature matrix](entities-graphics-versions.md).

## Unity Player system requirements

This section describes the Entities Graphics package’s target platform requirements. For platforms or use cases not covered in this section, general system requirements for the Unity Player apply.

For more information, see [System requirements for Unity](xref:system-requirements).

| **Platform**                           | **HDRP**          | **URP**                          |
| -------------------------------------- |------------------ | -------------------------------- |
| **Desktop**                            | Supported         | Supported                        |
| **Android&trade;**                            | Not supported     | Only Graphics API Vulkan and OpenGL ES 3.1 or above is supported |
| **iOS**                                | Not supported     | Only Metal Graphics is supported |
| **Nintendo Switch&trade;**                    | Not supported     | Supported                        |
| **PlayStation&reg; 4**<br>**PlayStation&reg; 5** | Supported         | Supported                        |
| **Xbox&reg; One**<br>**Xbox&reg; Series**        | Supported         | Supported                        |
| **XR platform**                        | Supported         | Supported                        |
| **Web platforms**                      | Not supported     | Not supported                    |

> [!NOTE]
> Entities Graphics also depends on platform support of Scriptable Render Pipeline (SRP). See system requirements for [Universal Render Pipeline (URP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest?subfolder=/manual/requirements.html) and [High Definition Render Pipeline (HDRP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/System-Requirements.html).

## ECS feature compatibility

Entities Graphics does not support multiple [Worlds](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/manual/concepts-worlds.html). Limited support for multiple Worlds is intended in a later version. The current plan is to add support for creating multiple rendering systems, one per renderable World, but then only have one World active for rendering at once.
