using System.Collections.Generic;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
#if HDRP_10_0_0_OR_NEWER
using UnityEngine.Rendering.HighDefinition;
#endif
#if URP_10_0_0_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif

namespace Unity.Rendering
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
#if !TINY_0_22_0_OR_NEWER
    class LightCompanionBaker : Baker<Light>
    {
        public override void Bake(Light authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);

#if UNITY_EDITOR
            // Explicitly store the LightBakingOutput using a component, so we can restore it
            // at runtime.
            var bakingOutput = authoring.bakingOutput;
            AddComponent(entity, new LightBakingOutputData {Value = bakingOutput});
#endif
        }
    }

    class LightProbeProxyVolumeCompanionBaker : Baker<LightProbeProxyVolume>
    {
        public override void Bake(LightProbeProxyVolume authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
        }
    }

    class ReflectionProbeCompanionBaker : Baker<ReflectionProbe>
    {
        public override void Bake(ReflectionProbe authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
        }
    }

    class TextMeshCompanionBaker : Baker<TextMesh>
    {
        public override void Bake(TextMesh authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var meshRenderer = GetComponent<MeshRenderer>();
            AddComponentObject(entity, authoring);
            AddComponentObject(entity, meshRenderer);
        }
    }

    class SpriteRendererCompanionBaker : Baker<SpriteRenderer>
    {
        public override void Bake(SpriteRenderer authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
        }
    }

    class VisualEffectCompanionBaker : Baker<VisualEffect>
    {
        public override void Bake(VisualEffect authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
        }
    }

    class ParticleSystemCompanionBaker : Baker<ParticleSystem>
    {
        public override void Bake(ParticleSystem authoring)
        {
            var particleSystemRenderer = GetComponent<ParticleSystemRenderer>();
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
            AddComponentObject(entity, particleSystemRenderer);
        }
    }

    class AudioSourceCompanionBaker : Baker<AudioSource>
    {
        public override void Bake(AudioSource authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
        }
    }

#if SRP_10_0_0_OR_NEWER
    class VolumeCompanionBaker : Baker<Volume>
    {
        public override void Bake(Volume authoring)
        {
            var sphereCollider = GetComponent<SphereCollider>();
            var boxCollider = GetComponent<BoxCollider>();
            var capsuleCollider = GetComponent<CapsuleCollider>();
            var meshCollider = GetComponent<MeshCollider>();

            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);

            if(sphereCollider != null)
                AddComponentObject(entity, sphereCollider);

            if(boxCollider != null)
                AddComponentObject(entity, boxCollider);

            if(capsuleCollider != null)
                AddComponentObject(entity, capsuleCollider);

            if(meshCollider != null)
                AddComponentObject(entity, meshCollider);
        }
    }

#endif

#if HDRP_10_0_0_OR_NEWER
    class HDAdditionalLightDataCompanionBaker : Baker<HDAdditionalLightData>
    {
        public override void Bake(HDAdditionalLightData authoring)
        {
            var light = GetComponent<Light>();
            // A disabled light component won't be added to the companion GameObject,
            // other components that require the light component should not be added either.
            if (light.enabled)
            {
                // Setting companions to Dynamic
                var entity = GetEntity(TransformUsageFlags.Dynamic);
#if UNITY_EDITOR
                var isBaking = light.lightmapBakeType == LightmapBakeType.Baked;
                if(!isBaking)
                    AddComponentObject(entity, authoring);
#else
                AddComponentObject(entity, authoring);
#endif
            }
        }
    }

    class HDAdditionalReflectionDataCompanionBaker : Baker<HDAdditionalReflectionData>
    {
        public override void Bake(HDAdditionalReflectionData authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
        }
    }

    class LocalVolumetricFogCompanionBaker : Baker<LocalVolumetricFog>
    {
        public override void Bake(LocalVolumetricFog authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
        }
    }

    class PlanarReflectionProbeCompanionBaker : Baker<PlanarReflectionProbe>
    {
        public override void Bake(PlanarReflectionProbe authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
        }
    }
#if PROBEVOLUME_CONVERSION
    class ProbeVolumeCompanionBaker : Baker<ProbeVolume>
    {
        public override void Bake(ProbeVolume authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
        }
    }
#endif
#endif

#if URP_10_0_0_OR_NEWER
    class UniversalAdditionalLightDataCompanionBaker : Baker<UniversalAdditionalLightData>
    {
        public override void Bake(UniversalAdditionalLightData authoring)
        {
            var light = GetComponent<Light>();

            // A disabled light component won't be added to the companion GameObject,
            // other components that require the light component should not be added either.
            if (light.enabled)
            {
                // Setting companions to Dynamic
                var entity = GetEntity(TransformUsageFlags.Dynamic);
#if UNITY_EDITOR
                var isBaking = light.lightmapBakeType == LightmapBakeType.Baked;
                if (!isBaking)
                    AddComponentObject(entity, authoring);
#else
            AddComponentObject(entity, authoring);
#endif
            }
        }
    }
#endif

#if HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER
    class DecalProjectorCompanionBaker : Baker<DecalProjector>
    {
        public override void Bake(DecalProjector authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
        }
    }
#endif

#if HYBRID_ENTITIES_CAMERA_CONVERSION
    class CameraCompanionBaker : Baker<Camera>
    {
        public override void Bake(Camera authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
        }
    }

#if HDRP_10_0_0_OR_NEWER
    class HDAdditionalCameraDataCompanionBaker : Baker<HDAdditionalCameraData>
    {
        public override void Bake(HDAdditionalCameraData authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
        }
    }
#endif

#if URP_10_0_0_OR_NEWER
    class UniversalAdditionalCameraDataCompanionBaker : Baker<UniversalAdditionalCameraData>
    {
        public override void Bake(UniversalAdditionalCameraData authoring)
        {
            // Setting companions to Dynamic
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, authoring);
        }
    }
#endif

#endif
#endif

#endif
}
