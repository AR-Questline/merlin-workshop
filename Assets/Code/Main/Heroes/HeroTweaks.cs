using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Controls;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroTweaks : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroTweaks;

        HeroEncumbered HeroEncumbered => _heroEncumbered ??= (ParentModel.TryGetElement<HeroEncumbered>() ?? ParentModel.AddElement<HeroEncumbered>());
        HeroEncumbered _heroEncumbered;

        public HeroTweaks() {
            ModelElements.SetInitCapacity(45);
            ModelElements.SetInitCapacity(typeof(StatTweak), 1, 42);
        }

        protected override void OnInitialize() {
            AttachListeners().Forget();
        }
        
        async UniTaskVoid AttachListeners() {
            // We want to skip the initial state changes
            // and delayed because some systems may have not applied modifications yet
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            
            ParentModel.ListenTo(Stat.Events.ChangingStat(CharacterStatType.Stamina), StaminaChanging, this);
            ParentModel.ListenTo(LimitedStat.Events.LimitedStatLimitsReached(CharacterStatType.Stamina), OnStaminaChangePrevented, this);
            
            ParentModel.ListenTo(Stat.Events.StatChanged(HeroStatType.EncumbranceLimit), RefreshEncumbrance, this);
            ParentModel.ListenTo(IItemOwner.Relations.Owns.Events.AfterAttached, RefreshEncumbrance, this);
            ParentModel.ListenTo(IItemOwner.Relations.Owns.Events.AfterDetached, RefreshEncumbrance, this);
            ParentModel.HeroItems.ListenTo(Events.AfterChanged, RefreshEncumbrance, this);
            
            RefreshEncumbrance();
        }

        void RefreshEncumbrance() {
            float currentWeight = ParentModel.HeroItems.CurrentWeight;
            bool shouldBeEncumbered = currentWeight > ParentModel.HeroStats.EncumbranceLimit;
            HeroEncumbered.ToggleEncumbered(shouldBeEncumbered);
        }

        void StaminaChanging(HookResult<IWithStats, Stat.StatChange> hook) {
            // --- Stamina Limit
            float value = hook.Value.value;
            LimitStamina(value);
        }

        void OnStaminaChangePrevented(LimitedStat.LimitedStatChange limitedStatChange) {
            // --- If value change was less then 0 we handle it in OnStatChanging event
            if (limitedStatChange.valueThatWasSet < 0) {
                return;
            }
            LimitStamina(limitedStatChange.desiredChangeOfValue);
        }

        void LimitStamina(float value) {
            if (value < 0) {
                // If we drained out whole stamina we block stamina regen for long time & wait for full stamina restore.
                float valueAfterDecrease = ParentModel.Stamina.ModifiedValue + value;

                GameConstants gameConstants = GameConstants.Get;
                float maxPrevent = gameConstants.maxStaminaRegenPreventDuration;
                float mediumPrevent = gameConstants.mediumStaminaRegenPreventDuration;
                float shortPrevent = gameConstants.shortStaminaRegenPreventDuration;

                float mediumPreventAfter = -gameConstants.mediumPreventAfterStaminaConsumed;
                float maxStaminaConsumed = -gameConstants.maxPreventAfterStaminaConsumed;
                
                if (valueAfterDecrease < 0) {
                    float duration = Mathf.Lerp(maxPrevent, mediumPrevent, valueAfterDecrease.Remap(maxStaminaConsumed, -1, 0, 1)) *
                                     ParentModel.Stat(HeroStatType.StaminaDepletedTimeMultiplier);
                    PreventStaminaRegenDuration.PreventWithStatus(ParentModel, new TimeDuration(duration), new TimeDuration(duration));
                } else {
                    PreventStaminaRegenDuration.Prevent(ParentModel, new TimeDuration(value > mediumPreventAfter ? shortPrevent : mediumPrevent));
                }
            }
        }
    }
}