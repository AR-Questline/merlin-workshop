using Awaken.ECS.Components;
using Awaken.ECS.Critters.Components;
using Awaken.ECS.DrakeRenderer;
using FMODUnity;
using Unity.Entities;
using UnityEngine;

namespace Awaken.ECS.Critters {
    public static class CritterEntityData {
        public const float ToAndFromIdleTransitionTime = 0.5f;
        public const float ToDeathTransitionTime = 0.2f;
 
        public const int WalkingAnimationIndex = 0;
        public const int IdleAnimationIndex = 1;
        public const int DeathAnimationIndex = 0; // TODO change when death animation will be created
        
        public static ComponentType[] CritterEntityComponentTypes => s_critterEntityComponentTypes ??= new[] {
            ComponentType.ReadWrite<DrakeVisualEntitiesTransform>(),
            ComponentType.ReadWrite<CritterAnimatorParams>(),
            ComponentType.ReadWrite<DrakeVisualEntity>(),
            ComponentType.ReadWrite<CritterMovementState>(),
            ComponentType.ReadWrite<CritterGroupSharedData>(),
            ComponentType.ReadWrite<CrittersGroupEntity>(),
            ComponentType.ReadWrite<CritterIndexInGroup>(),
            ComponentType.ReadWrite<StudioEventEmitter>(),
            // LinkedLifetimeRequest component should always be last in the array
            ComponentType.ReadWrite<LinkedEntitiesAccessRequest>(),
        };

        static ComponentType[] s_critterEntityComponentTypes;
    }
}