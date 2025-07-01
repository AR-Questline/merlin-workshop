#pragma warning disable CS0282
#if MODULE_ENTITIES
using Unity.Entities;
using UnityEngine;

namespace Pathfinding.ECS {
	using Pathfinding;

	[UpdateBefore(typeof(RepairPathSystem))]
	[UpdateInGroup(typeof(AIMovementSystemGroup))]
	[RequireMatchingQueriesForUpdate]
	public partial class SyncDestinationTransformSystem : SystemBase {
		protected override void OnUpdate() {
			// If there will be multiple simulation steps during this frame, only update the destination points on the first step.
			// It cannot change between simulation steps anyway.
			if (!AIMovementSystemGroup.TimeScaledRateManager.IsFirstSubstep) return;

			foreach (var(point, destinationSetterWrapper) in SystemAPI.Query<RefRW<DestinationPoint>, SystemAPI.ManagedAPI.UnityEngineComponent<AIDestinationSetter> >()) {
				var destinationSetter = destinationSetterWrapper.Value;
				if (destinationSetter.target != null) {
					point.ValueRW = new DestinationPoint {
						destination = destinationSetter.target.position,
						facingDirection = destinationSetter.useRotation ? destinationSetter.target.forward : Vector3.zero
					};
				}
			}
		}
	}
}
#endif
