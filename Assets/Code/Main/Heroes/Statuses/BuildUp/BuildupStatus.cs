using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses.Attachments;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.Statuses.BuildUp {
    public abstract partial class BuildupStatus : Status {
        public const float DefaultDecayRateDuration = 10f;

        public sealed override bool IsNotSaved => true;
        
        readonly BuildupConsumptionType _buildupConsumptionType;
        readonly BuildupStatusType _buildupStatusType;
        readonly float _maxDuration;
        readonly float _decayRateMultiplier;
        readonly float _gainMultiplier;
        readonly bool _startActivated;
        readonly bool _isDecayUsingEffectModifier;
        bool _activated;
        
        public BuildupStatusType BuildupStatusStatusType => _buildupStatusType;
        /// <summary>
        /// 0-1
        /// </summary>
        public float BuildupProgress => CurrentBuildup / NeededBuildup;
        public bool Active => _activated || _startActivated;
        public string DurationText => $"{DurationProgress:F1} {LocTerms.SecondsAbbreviation.Translate()}";
        public override float EffectModifier => ParentModel.ParentModel.Stat(EffectModifierStat).ModifiedValue;
        protected float CurrentBuildup { get; private set; }
        StatType BuildupStat => _buildupStatusType.BuildupStatType;
        StatType EffectModifierStat => _buildupStatusType.EffectModifierType;
        float DurationProgress => BuildupProgress * _maxDuration;
        float NeededBuildup => Character.Stat(BuildupStat)?.ModifiedValue ?? 10f;
        float DecayRate => NeededBuildup * _decayRateMultiplier;
        
        public new static class Events {
            public static readonly Event<BuildupStatus, BuildupStatus> BuildupCompleted = new(nameof(BuildupCompleted));
        }

        public static void CreateBuildupStatus(BuildupAttachment data, CharacterStatuses statuses, 
            StatusTemplate statusTemplate, StatusSourceInfo sourceInfo, float startingBuildup) {
            
            ICharacter character = statuses.ParentModel;
            character.Trigger(ICharacter.Events.TriedToDealBuildupStatus, new TrialBuildupData(character, data, sourceInfo.SourceCharacter.Get()));
            if (!statusTemplate.IsBuildupAble || data == null || IsImmune(character, data) || !statusTemplate.CanBeApplied) {
                return;
            }

            BuildupStatus newBuildupStatus = data.BuildupType switch {
                BuildupType.ActivateThisStatus => statuses.AddElement(new BuildupStatusActivation(data, statusTemplate, sourceInfo)),
                BuildupType.ChangeStatusToDifferent => statuses.AddElement(new BuildupStatusUpgradable(data, statusTemplate, sourceInfo)),
                _ => throw new ArgumentOutOfRangeException()
            };

            newBuildupStatus.ParentModel.AddBuildupStatus(newBuildupStatus);
            if (newBuildupStatus is { HasBeenDiscarded: false }) {
                newBuildupStatus.Buildup(startingBuildup, true);
            }
        }
        
        protected BuildupStatus(BuildupAttachment data, StatusTemplate template, StatusSourceInfo sourceInfo, SkillVariablesOverride variableOverride = null) : base(template, sourceInfo, variableOverride) {
            _startActivated = data.StartActivated;
            _maxDuration = data.BuildupDuration;
            _decayRateMultiplier = 1 / _maxDuration;
            _gainMultiplier = data.BuildupGainMultiplier;
            _buildupConsumptionType = data.BuildupConsumptionType;
            _buildupStatusType = data.BuildupStatusType;
            _isDecayUsingEffectModifier = data.IsDecayUsingEffectModifier;
        }

        protected override void OnInitialize() {
            if (_startActivated) {
                ActivateStatus();
            }
        }

        public void ActivateStatus() {
            if (_activated) {
                return;
            }

            _activated = true;
            base.OnInitialize();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_activated) {
                base.OnDiscard(fromDomainDrop);
            }
        }

        /// <summary>
        /// Buildup the status
        /// </summary>
        /// <returns>Was buildup completed</returns>
        public bool Buildup(float buildupStrength, bool ignoreMultiplier = false) {
            if (ignoreMultiplier) {
                CurrentBuildup += buildupStrength;   
            } else {
                CurrentBuildup += buildupStrength * _gainMultiplier;
            }

            bool buildupComplete = CurrentBuildup >= NeededBuildup;

            if (buildupComplete) {
                switch (_buildupConsumptionType) {
                    case BuildupConsumptionType.ConsumeMax:
                        CurrentBuildup %= NeededBuildup;
                        break;
                    case BuildupConsumptionType.ClampToMax:
                        CurrentBuildup = NeededBuildup;
                        break;
                    case BuildupConsumptionType.Clear:
                        CurrentBuildup = 0;
                        break;
                }
                OnBuildupComplete();
                this.Trigger(Events.BuildupCompleted, this);
                this.TriggerChange();
            }
            return buildupComplete;
        }

        public void CompleteBuildup() {
            Buildup(NeededBuildup - CurrentBuildup, true);
        }
        
        /// <returns>Fully decayed</returns>
        public bool Decay(float deltaTime) {
            if (HasBeenDiscarded) return true;
            
            if (_isDecayUsingEffectModifier && _activated) {
                CurrentBuildup -= deltaTime * DecayRate * (1f / EffectModifier);
            } else {
                CurrentBuildup -= deltaTime * DecayRate;
            }
            
            if (CurrentBuildup < 0) {
                CurrentBuildup = 0;
                OnDecayed();
                return true;
            }

            return false;
        }
        
        protected virtual void OnBuildupComplete() { }

        protected virtual void OnDecayed() {
            if (HasBeenDiscarded) {
                return;
            }
            Discard();
        }

        static bool IsImmune(ICharacter character, BuildupAttachment data) {
            var statType = data.BuildupStatusType.BuildupStatType;
            return character.Stat(statType)?.BaseValue == StatusStatsValues.CantGetBuildupValue;
        }
    }
}