using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Stats.Observers {
    public partial class DifficultyObserver : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        StatTweak _damageDealtTweak;
        StatTweak _magicDamageDealtTweak;
        StatTweak _damageReceivedTweak;
        StatTweak _staminaUsageTweak;
        StatTweak _manaUsageTweak;

        [UnityEngine.Scripting.Preserve] IEnumerable<StatTweak> Tweaks => new[] { _damageDealtTweak, _damageReceivedTweak, _staminaUsageTweak, _manaUsageTweak }.AsEnumerable();

        protected override void OnFullyInitialized() {
            var difficultySetting = World.Only<DifficultySetting>();
            difficultySetting.ListenTo(Setting.Events.SettingRefresh, SettingsChanged, this);
            _damageDealtTweak = new(Hero.Current.CharacterStats.Strength, 1, TweakPriority.Override, OperationType.Multi, this);
            _magicDamageDealtTweak = new(Hero.Current.CharacterStats.MagicStrength, 1, TweakPriority.Override, OperationType.Multi, this);
            _damageReceivedTweak = new(Hero.Current.CharacterStats.IncomingDamage, 1, TweakPriority.Override, OperationType.Multi, this);
            _staminaUsageTweak = new(Hero.Current.CharacterStats.StaminaUsageMultiplier, 1, TweakPriority.Override, OperationType.Multi, this);
            _manaUsageTweak = new(Hero.Current.CharacterStats.ManaUsageMultiplier, 1, TweakPriority.Override, OperationType.Multi, this);
            SettingsChanged(difficultySetting);
        }

        void SettingsChanged(Setting setting) {
            var difficultySetting = (DifficultySetting)setting;
            var difficulty = difficultySetting.Difficulty;
            _damageDealtTweak.SetModifier(difficulty.DamageDealt);
            _magicDamageDealtTweak.SetModifier(difficulty.DamageDealt);
            _damageReceivedTweak.SetModifier(difficulty.DamageReceived);
            _staminaUsageTweak.SetModifier(difficulty.StaminaUsage);
            _manaUsageTweak.SetModifier(difficulty.ManaUsage);
        }
    }
}