using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Observers;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items {
    public partial class ArmorWeight : Element<Hero> {
        const float ManaUsageTalentReductionMultiplier = 0.5f;
        const int NoStaminaUsageIncreaseModifier = 0;

        public sealed override bool IsNotSaved => true;

        readonly Dictionary<StatType, StatTweak> _tweaks = new();

        public float ArmorWeightScore { get; private set; } = -1;
        public ItemWeight ArmorWeightType => ItemWeight.FromArmorScore(ArmorWeightScore);

        public float MaxEquipmentWeight => (ParentModel.HeroStats.EncumbranceLimit * GameConstants.Get.HeavyArmorThreshold) /
                                           (ParentModel.HeroStats.ArmorWeightMultiplier <= 0 ? 0.0001f : ParentModel.HeroStats.ArmorWeightMultiplier);
        
        public float EquipmentWeight => CalculateCurrentEquipmentWeight();
        /// <summary>
        /// Includes armor weight multiplier stat
        /// </summary>
        public float EquipmentWeightPercent => (CalculateCurrentEquipmentWeight() / ParentModel.HeroStats.EncumbranceLimit) * ParentModel.HeroStats.ArmorWeightMultiplier;

        readonly StatType[] _percentageStatsInDescription = {
            CharacterStatType.ManaRegen,
            CharacterStatType.StaminaRegen,
            CharacterStatType.AttackSpeed,
            CharacterStatType.SpellChargeSpeed,
            HeroStatType.DashSpeed, 
            CharacterStatType.MovementSpeedMultiplier, 
            CharacterStatType.ManaUsageMultiplier,
        };

        readonly StatType[] _valueStatsInDescription = {
            HeroStatType.MaxDashOptimalCounter,
        };

        public new static class Events {
            public static readonly Event<ArmorWeight, float> ArmorWeightScoreChanged = new(nameof(ArmorWeightScoreChanged));
        }

        protected override void OnFullyInitialized() {
            AttachListeners().Forget();
        }

        async UniTaskVoid AttachListeners() {
            // We want to skip the initial state changes
            // and delayed because some systems may have not applied modifications yet
            if (!await AsyncUtil.DelayFrame(this)) {
                 return;
            }
            ParentModel.HeroItems.ListenTo(ICharacterInventory.Events.AfterEquipmentChanged, _ => CalculateArmorWeight(), this);
            ParentModel.ListenTo(Stat.Events.StatChanged(HeroStatType.EncumbranceLimit), _ => CalculateArmorWeight(), this);
            ParentModel.ListenTo(Stat.Events.StatChanged(HeroStatType.ArmorWeightMultiplier), _ => CalculateArmorWeight(), this);
            ParentModel.ListenTo(Stat.Events.StatChanged(HeroStatType.ArmorPenaltyMultiplier), _ => CalculateArmorWeight(), this);
            
            ParentModel.ListenTo(Stat.Events.StatChanged(ProfStatType.LightArmor), _ => CalculateArmorWeight(), this);
            ParentModel.ListenTo(Stat.Events.StatChanged(ProfStatType.MediumArmor), _ => CalculateArmorWeight(), this);
            ParentModel.ListenTo(Stat.Events.StatChanged(ProfStatType.HeavyArmor), _ => CalculateArmorWeight(), this);
            
            CalculateArmorWeight();
        }

        //if percentage is the same display name by name (comma seperate)and percantage value  at end without linq

        public string CreateDescription() {
            StringBuilder sb = new();
            var weightParamsEnumerable = GameConstants.Get.armorWeightScoreParams.Where(weightScore =>
                _percentageStatsInDescription.Contains(weightScore.StatType) ||
                _valueStatsInDescription.Contains(weightScore.StatType)).ToList();
            int allParamsCount = weightParamsEnumerable.Count;

            Dictionary<float, List<string>> percentageStats = new();
            List<string> valueStats = new();

            foreach (var weightParam in weightParamsEnumerable) {
                float value = weightParam.GetValue(ArmorWeightType);

                if (_percentageStatsInDescription.Contains(weightParam.StatType)) {
                    if (!percentageStats.ContainsKey(value)) {
                        percentageStats[value] = new List<string>();
                    }
                    percentageStats[value].Add(weightParam.StatType.DisplayName.ToString().ToLower());
                }

                if (_valueStatsInDescription.Contains(weightParam.StatType)) {
                    valueStats.Add($"{weightParam.StatType.DisplayName.ToString().ToLower()} <b>{value}</b>");
                }
            }

            foreach (var kvp in percentageStats) {
                float value = kvp.Key;
                string valueString = Mathf.Approximately(value, 1) ? $" {LocTerms.NoChanges.Translate()}" :
                    value > 1 ? $" +{Mathf.Abs(1 - value):P0}" :
                    $" -{Mathf.Abs(1 - value):P0}";
                sb.Append(string.Join(", ", kvp.Value) + valueString.Bold() + ", ");
            }

            sb.Append(string.Join(", ", valueStats) + ".");

            return sb.ToString().FirstCharacterToUpper();
        }
        
        public NoiseStrength ArmorNoiseStrength() {
            if (ArmorWeightType == ItemWeight.Light) {
                return NoiseStrength.CrouchingMovementLight;
            }
            if (ArmorWeightType == ItemWeight.Medium) {
                return NoiseStrength.CrouchingMovementMedium;
            }
            return NoiseStrength.CrouchingMovementHeavy;
        }

        float CalculateCurrentEquipmentWeight() {
            var weightsSum = 0f;
            foreach (var equippedItem in ParentModel.HeroItems.EquippedItems()) {
                if (equippedItem.IsArmor) {
                    weightsSum += equippedItem.Weight;
                }
            }
            return weightsSum;
        }

        void CalculateArmorWeight() {
            float oldArmorWeightScore = ArmorWeightScore;
            ArmorWeightScore = Mathf.Clamp01(EquipmentWeightPercent);
            
            this.Trigger(Events.ArmorWeightScoreChanged, ArmorWeightScore);
            ApplyEffects();
            ApplyInformativeStatuses(oldArmorWeightScore, ArmorWeightScore);
        }

        void ApplyEffects() {
            var effects = World.Services.Get<GameConstants>().armorWeightScoreParams;
            ItemWeight nextArmorWeightType = ItemWeight.LighterArmorType(ArmorWeightType);
            foreach (ArmorWeightScoreParams effect in effects) {
                float modifier = GetEffectModifier(effect, nextArmorWeightType);

                if (!_tweaks.TryGetValue(effect.StatType, out var tweak) || tweak == null || tweak.HasBeenDiscarded) {
                    _tweaks.Add(effect.StatType, new(
                        tweakedStat: ParentModel.Stat(effect.StatType),
                        modifier: modifier,
                        priority: null,
                        operation: effect.OperationType,
                        parentModel: this));
                } else {
                    tweak.SetModifier(modifier);
                }
            }
        }

        void ApplyInformativeStatuses(float oldValue, float newValue) {
            var oldScore = ItemWeight.FromArmorScore(oldValue);
            var newScore = ItemWeight.FromArmorScore(newValue);
            if (oldScore == newScore) {
                return;
            }
            
            var statuses = ParentModel.Statuses;
            var commonRef = CommonReferences.Get;
            statuses.RemoveStatus(commonRef.ArmorStatus(oldScore));
            var statusTemplate = commonRef.ArmorStatus(newScore);
            statuses.AddStatus(statusTemplate, StatusSourceInfo.FromStatus(statusTemplate).WithCharacter(ParentModel)).newStatus.MarkedNotSaved = true;
        }

        float GetEffectModifier(in ArmorWeightScoreParams effect, ItemWeight lighterArmorType) {
            ItemWeight armorWeightType = ArmorWeightType;
            
            if (armorWeightType == ItemWeight.Heavy && ParentModel.Development.HeavyArmorNoStaminaUsageIncrease &&
                effect.StatType == CharacterStatType.StaminaUsageMultiplier) {
                return NoStaminaUsageIncreaseModifier;
            } 
            
            float multiplier = 1;
            multiplier = HandleReducedManaUsageTalent(effect, armorWeightType, multiplier);

            float effectStrength = effect.GetValue(armorWeightType);
            
            if (armorWeightType != ItemWeight.Overload) {
                effectStrength = HandleProficiencyEffect(effect, armorWeightType, lighterArmorType, effectStrength);
            }
            
            float effectModifier = effectStrength * multiplier;

            multiplier = ParentModel.Stat(HeroStatType.ArmorPenaltyMultiplier).ModifiedValue;
            if (effect.IsPenalty(armorWeightType) && multiplier != 1f) {
                if (effect.OperationType == OperationType.Multi) {
                    if (effectModifier < 1) {
                        effectModifier = 1 - (1 - effectModifier) * multiplier;
                    } else {
                        effectModifier = 1 + (effectModifier - 1) * multiplier;
                    }
                } else if (effect.OperationType == OperationType.Add || effect.OperationType == OperationType.AddPreMultiply){
                    effectModifier *= multiplier;
                }
            }
            return effectModifier;
        }

        static float HandleProficiencyEffect(ArmorWeightScoreParams effect, ItemWeight armorWeightType, ItemWeight lighterArmorType, float effectStrength) {
            if (effect.StatType == HeroStatType.MaxDashOptimalCounter) {
                return effectStrength;
            }
            
            float lighterArmorValue = effect.GetValue(lighterArmorType);

            Stat linkedProficiency = armorWeightType.LinkedProficiency(Hero.Current);
            if (linkedProficiency != null) {
                effectStrength = math.lerp(
                    effectStrength,
                    lighterArmorValue,
                    linkedProficiency.ModifiedValue.RemapTo01(10, 100, true));
            }

            return effectStrength;
        }

        float HandleReducedManaUsageTalent(ArmorWeightScoreParams effect, ItemWeight armorWeightType, float multiplier) {
            bool correctArmorType = armorWeightType == ItemWeight.Medium || armorWeightType == ItemWeight.Heavy;
            bool talentUnlocked = ParentModel.Development.ArmorReducedManaUsage;
            bool correctStat = effect.StatType == CharacterStatType.ManaUsageMultiplier;
            if (correctArmorType && talentUnlocked && correctStat) {
                multiplier = ManaUsageTalentReductionMultiplier;
            }

            return multiplier;
        }

        public string GetDescription() {
            return string.Empty;
            // Temporarily disabled since it's not used
            // var weightParams = GameConstants.Get.armorWeightScoreParams;
            //
            // return ArmorWeightType == ItemWeight.Light ? LightWeightDescription() :
            //     ArmorWeightType == ItemWeight.Medium ? MediumWeightDescription() :
            //     ArmorWeightType == ItemWeight.Heavy ? HeavyWeightDescription() : 
            //     string.Empty;
            //
            // string LightWeightDescription() {
            //     string parameter = Mathf.Abs(weightParams
            //             .FirstOrDefault(p => p.StatType == CharacterStatType.StaminaUsageMultiplier)
            //             .GetValue(ItemWeight.Light))
            //         .ToString();
            //
            //     return LocTerms.WeightLightDesc.Translate(parameter);
            // }
            //
            // string MediumWeightDescription() {
            //     string dashModifier = ((1 - ParentModel.Data.dashDurationMediumArmor) * 100).ToString();
            //     float parameterValue = weightParams
            //         .FirstOrDefault(p => p.StatType == CharacterStatType.ManaUsageMultiplier)
            //         .GetValue(ItemWeight.Medium);
            //     string parameter = ParentModel.Development.ArmorReducedManaUsage
            //         ? (parameterValue * ManaUsageTalentReductionMultiplier).ToString().ColoredText(ARColor.green)
            //         : parameterValue.ToString();
            //
            //     return LocTerms.WeightMediumDesc.Translate(parameter, dashModifier);
            // }
            //
            // string HeavyWeightDescription() {
            //     float dashModifier = (1 - ParentModel.Data.dashDurationHeavyArmor) * 100;
            //
            //     return LocTerms.WeightHeavyDesc
            //         .Translate(GetParams().Append(dashModifier.ToString()).ToArray());
            //
            //     IEnumerable<string> GetParams() {
            //         var enumerator = weightParams.GetEnumerator();
            //         while (enumerator.MoveNext()) {
            //             var param = (ArmorWeightScoreParams)enumerator.Current;
            //             if (param.StatType == HeroStatType.NoiseMultiplier) {
            //                 yield return param.GetValue(ItemWeight.Heavy).ToString();
            //             } else if (param.StatType == CharacterStatType.ManaUsageMultiplier) {
            //                 float value = param.GetValue(ItemWeight.Heavy);
            //                 if(ParentModel.Development.ArmorReducedManaUsage) {
            //                     yield return (value * ManaUsageTalentReductionMultiplier).ToString().ColoredText(ARColor.green);
            //                 }
            //                 
            //                 yield return value.ToString();
            //             } else if (param.StatType == CharacterStatType.StaminaUsageMultiplier) {
            //                 if (ParentModel.Development.HeavyArmorNoStaminaUsageIncrease) {
            //                     yield return NoStaminaUsageIncreaseModifier.ToString().ColoredText(ARColor.green);
            //                 } else {
            //                     yield return param.GetValue(ItemWeight.Heavy).ToString();
            //                 }
            //             }
            //         }
            //     }
            // }
        }
    }
}