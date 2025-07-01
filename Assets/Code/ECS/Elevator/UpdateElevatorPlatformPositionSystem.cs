using Unity.Entities;

namespace Awaken.ECS.Elevator {
    [UpdateInGroup(typeof(ElevatorSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class UpdateElevatorPlatformPositionSystem : SystemBase {
        protected override void OnUpdate() {
            Entities.ForEach((ref ElevatorChainData elevatorChainData, in ElevatorPlatformCurrentPositionY platformCurrentPositionY) => {
                elevatorChainData.platformPrevPositionY = elevatorChainData.platformCurrentPositionY;
                elevatorChainData.platformCurrentPositionY = platformCurrentPositionY.value;
            }).Run();
        }
    }
}