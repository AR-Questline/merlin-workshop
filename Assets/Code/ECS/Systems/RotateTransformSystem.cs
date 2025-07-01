using Awaken.ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Awaken.ECS.Systems {
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default)]
    [BurstCompile]
    public partial class RotateTransformSystem : SystemBase {
        protected override void OnUpdate() {
            var deltaTime = SystemAPI.Time.DeltaTime;
            Dependency = Entities
                .ForEach((ref LocalToWorld localToWorld, in RotateTransformComponent rotateLinkedTransform) => {
                var rotationAngle = rotateLinkedTransform.rotationSpeed * deltaTime;
                var rotationQuaternion = quaternion.AxisAngle(rotateLinkedTransform.rotationAxis, rotationAngle);
                var rotationMatrix = GetRotationMatrix(rotationQuaternion);
                localToWorld.Value = math.mul(localToWorld.Value, rotationMatrix);
            }).Schedule(Dependency);
        }

        static float4x4 GetRotationMatrix(quaternion rotation) {
            var r = new float3x3(rotation);
            return new float4x4(
                new float4(r.c0, 0.0f),
                new float4(r.c1, 0.0f),
                new float4(r.c2, 0.0f),
                new float4(0f, 0f, 0f, 1.0f));
        }
    }
}