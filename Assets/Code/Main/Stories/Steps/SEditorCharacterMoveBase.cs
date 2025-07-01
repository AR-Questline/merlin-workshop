using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [LabelWidth(120)]
    public abstract class SEditorCharacterMoveBase : EditorStep {
        [Tooltip("What distance from target is good enough to stop?"), PropertyOrder(-10)]
        public float stoppingDistance = 0.2F;
        [Tooltip("Story should wait for arrival."), PropertyOrder(-10)]
        public bool waitForEnd;
        
        [ShowIf(nameof(ShowTargetSelection)), Header("Target Position")]
        public SCharacterMoveBase.TargetType moveToType = SCharacterMoveBase.TargetType.Hero;
        [ShowIf(nameof(ShowLocationRef))]
        public LocationReference target = new() {targetTypes = Locations.TargetType.Actor};
        [ShowIf(nameof(ShowVector3))]
        public Vector3 targetPos;
        [ShowIf(nameof(ShowTargetSelection))]
        public SCharacterMoveBase.AdditionalOffset moveToOffsetType = SCharacterMoveBase.AdditionalOffset.None;
        [ShowIf(nameof(ShowAdditionalOffset)), LabelWidth(50)]
        public Vector3 offset;

        protected virtual bool ShowTargetSelection => true;
        protected bool ShowLocationRef => ShowTargetSelection && moveToType == SCharacterMoveBase.TargetType.Location;
        protected bool ShowVector3 => ShowTargetSelection && moveToType is SCharacterMoveBase.TargetType.WorldPosition;
        protected bool ShowAdditionalOffset => ShowTargetSelection && moveToOffsetType is not SCharacterMoveBase.AdditionalOffset.None;
    }

    public abstract partial class SCharacterMoveBase : StoryStep {
        public float stoppingDistance;
        public bool waitForEnd;
        public TargetType moveToType;
        public LocationReference target;
        public Vector3 targetPos;
        public AdditionalOffset moveToOffsetType;
        public Vector3 offset;

        public sealed override StepResult Execute(Story story) {
            StepResult result = new();
            foreach (ICharacter character in CharactersToMove(story)) {
                TryMoveCharacter(character, story, result);
            }
            return waitForEnd ? result : StepResult.Immediate;
        }
        
        protected abstract IEnumerable<ICharacter> CharactersToMove(Story api);
        protected abstract void TryMoveCharacter(ICharacter character, Story api, StepResult result);
        
        protected CharacterPlace ExtractTargetPos(Story api, ICharacter character) {
            Vector3 moveToPosition;
            Quaternion targetRotation;
            switch (moveToType) {
                case TargetType.Self:
                    moveToPosition = character.Coords;
                    targetRotation = character.Rotation;
                    break;
                case TargetType.Hero:
                    moveToPosition = api.Hero.Coords;
                    targetRotation = api.Hero.Rotation;
                    break;
                case TargetType.Location:
                    var location = target.FirstOrDefault(api);
                    if (location == null) {
                        Log.Critical?.Error($"Location {LogUtils.GetDebugName(target)} not found for story {LogUtils.GetDebugName(api)}.");
                        return new CharacterPlace(character.Coords, stoppingDistance);
                    }
                    moveToPosition = location.Coords;
                    targetRotation = location.Rotation;
                    break;
                case TargetType.WorldPosition:
                    moveToPosition = targetPos;
                    targetRotation = Quaternion.identity;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (moveToOffsetType) {
                case AdditionalOffset.None:
                    break;
                case AdditionalOffset.WorldPositionOffset:
                    moveToPosition += offset;
                    break;
                case AdditionalOffset.TargetRotationBasedOffset:
                    moveToPosition += targetRotation * offset;
                    break;
                case AdditionalOffset.HeroRotationBasedOffset:
                    moveToPosition += api.Hero.Rotation * offset;
                    break;
                case AdditionalOffset.HeroKeepInLineOfSightOffset:
                    var rotation = Quaternion.LookRotation(api.Hero.Coords - character.Coords, Vector3.up);
                    rotation = Quaternion.RotateTowards(api.Hero.Rotation, rotation, 30);
                    moveToPosition += rotation * offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return new CharacterPlace(Ground.SnapNpcToGround(moveToPosition), stoppingDistance);
        }
        
        [Serializable]
        public enum TargetType {
            Self = 0,
            Hero = 1,
            Location = 2,
            WorldPosition = 3,
        }
        
        [Serializable]
        public enum AdditionalOffset {
            None = 0,
            WorldPositionOffset = 1,
            TargetRotationBasedOffset = 2,
            HeroRotationBasedOffset = 3,
            HeroKeepInLineOfSightOffset = 4,
        }
    }
}