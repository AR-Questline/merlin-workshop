# DOTS instancing shader

To render entities with Entities Graphics, shaders must be DOTS Instancing compatible.

## SRP Shaders

For default Universal Render Pipeline (URP) and High Definition Render Pipeline (HDRP) shaders, they are already DOTS instacing compatible.

## ShaderGraph

If you have custom material properties in ShaderGraph that you want it to be DOTS instanced, see [Material overrides](material-overrides.md).

## Custom Shader

Entities Graphics provides a sample scene which demonstrates a simple unlit shader which renders using DOTS instancing.

- URP: URPSamples > SampleScenes > 6. Misc > SimpleDotsInstancingShader

For information on where to download the samples from, see [Sample Projects](sample-projects.md).

For more information about the dots instancing, see [DOTS Instancing shaders](xref:dots-instancing-shaders).