using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.General;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.AI.Idle.Data.Attachment {
    [Serializable]
    public struct InteractionData {
        public IdleType type;
        
        [ShowIf(nameof(HasPosition))] public IdlePosition position;
        [ShowIf(nameof(HasForward))] public IdlePosition forward;
        [ShowIf(nameof(HasRange))] public float range;
        [ShowIf(nameof(HasTags)), Tags(TagsCategory.Interaction)] public string interactionTag;
        
        [ShowIf(nameof(HasAllowInteractionRepeat))] public bool allowInteractionRepeat;
        [ShowIf(nameof(HasWaitTime))] public FloatRange waitTime;
        [ShowIf(nameof(HasUniqueID)), Tags(TagsCategory.InteractionID)] public string uniqueID;
        [ShowIf(nameof(HasScene))] public SceneReference scene;

        public InteractionData(IdleType type, IdlePosition position, IdlePosition forward, float range, string interactionTag, bool allowInteractionRepeat, FloatRange waitTime, string uniqueID, SceneReference scene) {
            this.type = type;
            this.position = position;
            this.forward = forward;
            this.range = range;
            this.interactionTag = interactionTag;
            this.allowInteractionRepeat = allowInteractionRepeat;
            this.waitTime = waitTime;
            this.uniqueID = uniqueID;
            this.scene = scene;
        }

        public readonly bool HasPosition => type is IdleType.Stand or IdleType.Wander or IdleType.Interactions;
        public readonly bool HasForward => type is IdleType.Stand;
        public readonly bool HasRange => type is IdleType.Wander or IdleType.Interactions;
        public readonly bool HasTags => type is IdleType.Interactions;
        public readonly bool HasAllowInteractionRepeat => type is IdleType.Interactions;
        public readonly bool HasWaitTime => type is IdleType.Wander;
        public readonly bool HasUniqueID => type is IdleType.Unique;
        public readonly bool HasScene => type is IdleType.ChangeScene;

        public readonly IInteractionFinder CreateFinder(IdleDataElement element) {
            return type switch {
                IdleType.Stand => new InteractionStandFinder(position, forward, element),
                IdleType.Wander => new InteractionRoamFinder(position, range, waitTime, element),
                IdleType.Interactions => new InteractionBaseFinder(position, range, interactionTag, element, allowInteractionRepeat),
                IdleType.Unique => new InteractionUniqueFinder(uniqueID),
                IdleType.ChangeScene => new InteractionChangeSceneFinder(scene),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        public enum IdleType {
            Stand,
            Wander,
            Interactions,
            Unique,
            ChangeScene,
        }
    }
}