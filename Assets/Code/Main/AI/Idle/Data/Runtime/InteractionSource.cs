using System;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Grounds;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Data.Runtime {
    public class InteractionSource : IInteractionSource {
        readonly FallbackInteractionData _fallbackInteractionData;

        public IInteractionFinder Finder { get; }

        public InteractionSource(IInteractionFinder finder, FallbackInteractionData fallbackInteractionData) {
            Finder = finder;
            _fallbackInteractionData = fallbackInteractionData;
        }

        public INpcInteraction GetFallbackInteraction(IdleBehaviours behaviours, IIdleDataSource dataSource) {
            return _fallbackInteractionData.GetInteraction(Finder, behaviours, dataSource);
        }
    }
    
    [Serializable]
    public struct FallbackInteractionData {
        public FallbackInteractionType type;
        [ShowIf(nameof(HasRange))] public float range;
        [ShowIf(nameof(HasWaitTime))] public FloatRange waitTime;
            
        bool HasRange => type is FallbackInteractionType.Wander;
        bool HasWaitTime => type is FallbackInteractionType.Wander;
            
        public FallbackInteractionData(FallbackInteractionType type, float range, FloatRange waitTime) {
            this.type = type;
            this.range = range;
            this.waitTime = new FloatRange(0.5f, 1.5f);
        }
        
        public static FallbackInteractionData Default => new (FallbackInteractionType.Stand, 0f, default);

        public readonly INpcInteraction GetInteraction(IInteractionFinder finder, IdleBehaviours behaviours, IIdleDataSource dataSource) {
            var position = IdlePosition.World(Ground.SnapNpcToGround(finder.GetDesiredPosition(behaviours) + Vector3.up));
            return type switch {
                FallbackInteractionType.Stand => new StandInteraction(position, IdlePosition.Self, dataSource, IInteractionSource.DefaultStandInteractionDuration),
                FallbackInteractionType.Wander => new RoamInteraction(position, range, waitTime, null),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    [Serializable]
    public enum FallbackInteractionType : byte {
        Stand,
        Wander,
    }
}