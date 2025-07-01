using Awaken.CommonInterfaces;
using Awaken.ECS.Mipmaps.Components;
using Awaken.Utility.Graphics.Mipmaps;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Awaken.ECS.Mipmaps.Systems {
    [UpdateAfter(typeof(MipmapsMissingFactorSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class MipmapsFactorSystem : SystemBase {
        protected override void OnUpdate() {
            var camera = CurrentCamera.Value;
            if (!camera) {
#if UNITY_EDITOR
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                camera = sceneView ? sceneView.camera : null;
                if (!camera) {
                    return;
                }
#else
                return;
#endif
            }
            var cameraData = new CameraData(camera);
            
            Entities
                .WithAbsent<SkipMipmapsFactorCalculationTag>()
                .ForEach((ref MipmapsFactorComponent mipmapsFactorComponent, in LocalToWorld localToWorld,
                    in WorldRenderBounds worldRenderBounds, in UVDistributionMetricComponent uvDistribution) => {
                    var fullUvDistribution = uvDistribution.value * UVDistributionTransformScale(localToWorld);
                    mipmapsFactorComponent.value = CalculateMipmapFactor(cameraData, worldRenderBounds.Value, fullUvDistribution);
                })
                .ScheduleParallel();

            Entities
                .WithAbsent<SkipMipmapsFactorCalculationTag>()
                .ForEach((ref MipmapsTransformFactorComponent mipmapsTransformFactor, in LocalToWorld localToWorld,
                    in WorldRenderBounds worldRenderBounds) => {
                    var transformScale = UVDistributionTransformScale(localToWorld);
                    mipmapsTransformFactor.value = CalculateTransformMipmapFactor(cameraData, worldRenderBounds.Value, transformScale);
                })
                .ScheduleParallel();
        }
        
        static float UVDistributionTransformScale(in LocalToWorld localToWorld) {
            var scale = localToWorld.Value.Scale();
            var maxScale = math.cmax(scale);
            return maxScale * maxScale;
        }

        static float CalculateMipmapFactor(in CameraData cameraData, in AABB bounds, float uvDistributionMetric) {
            // based on  http://web.cse.ohio-state.edu/~crawfis.3/cse781/Readings/MipMapLevels-Blog.html
            // screenArea = worldArea * (ScreenPixels/(D*tan(FOV)))^2
            // mip = 0.5 * log2 (uvArea / screenArea)
            float distanceSq = DistanceSq(bounds, cameraData.cameraPosition);
            // if (distanceSq < 1e-06) {
            //     return 0;
            // }

            // uvDistributionMetric is the average of triangle area / uv area (a ratio from world space triangle area to normalised uv area)
            // - triangle area is in world space
            // - uv area is in normalised units (0->1 rather than 0->texture size)

            // m_CameraEyeToScreenDistanceSquared / dSq is the ratio of screen area to world space area

            return distanceSq / (uvDistributionMetric * cameraData.cameraEyeToScreenDistanceSq);
        }

        static float CalculateTransformMipmapFactor(in CameraData cameraData, in AABB bounds, float transformScale) {
            // based on  http://web.cse.ohio-state.edu/~crawfis.3/cse781/Readings/MipMapLevels-Blog.html
            // screenArea = worldArea * (ScreenPixels/(D*tan(FOV)))^2
            // mip = 0.5 * log2 (uvArea / screenArea)
            float distanceSq = DistanceSq(bounds, cameraData.cameraPosition);
            // if (distanceSq < 1e-06) {
            //     return 0;
            // }

            // uvDistributionMetric is the average of triangle area / uv area (a ratio from world space triangle area to normalised uv area)
            // - triangle area is in world space
            // - uv area is in normalised units (0->1 rather than 0->texture size)

            // m_CameraEyeToScreenDistanceSquared / dSq is the ratio of screen area to world space area

            return distanceSq / (transformScale * cameraData.cameraEyeToScreenDistanceSq);
        }

        static float DistanceSq(in AABB bounds, in float3 point) {
            return math.lengthsq(math.max(math.abs(point - bounds.Center), bounds.Extents) - bounds.Extents);
        }
    }
}
