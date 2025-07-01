using System;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.AI.Idle.Data.Attachment {
    [Serializable]
    public struct IdleData {
        public float positionRange;
        
        [ListDrawerSettings(CustomAddFunction = nameof(CustomAddBehaviour))]
        public InteractionIntervalData[] behaviours;

        public InteractionCustomData[] customActions;
        
        public InteractionOneShotData[] oneShots;

        InteractionIntervalData CustomAddBehaviour() {
            return InteractionIntervalData.Interactions();
        }

        public static IdleData Default => new() {
            positionRange = 0.8f,
            behaviours = new[] { InteractionIntervalData.StandOnSpawn() },
        };
    }
}