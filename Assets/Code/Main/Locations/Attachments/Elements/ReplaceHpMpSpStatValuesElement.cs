using System.Threading;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class ReplaceHpMpSpStatValuesElement : Element<ICharacter> {
        public sealed override bool IsNotSaved => true;

        bool _disableListeners;
        int _nextFrameNumber;
        CancellationTokenSource _cts;
        StatTweak _hpOverride, _mpOverride, _spOverride;
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static ReplaceHpMpSpStatValuesElement Create(ICharacter character) {
            if (character.HasElement<ReplaceHpMpSpStatValuesElement>()) {
                Log.Important?.Error($"Attempted to add ReplaceHpMpSpStatValuesElement to a character {LogUtils.GetDebugName(character)} that already has one");
                return null;
            }
            return character.AddElement(new ReplaceHpMpSpStatValuesElement());;
        }
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static void Remove(ICharacter character) {
            character.TryGetElement<ReplaceHpMpSpStatValuesElement>()?.Discard();
        }

        protected override void OnInitialize() {
            InitListeners();
            TryReplaceStatsNextFrame();
        }

        void InitListeners() {
            ParentModel.ListenTo(Stat.Events.StatChanged(AliveStatType.MaxHealth), OnStatChanged, this);
            ParentModel.ListenTo(Stat.Events.StatChanged(CharacterStatType.MaxStamina), OnStatChanged, this);
            ParentModel.ListenTo(Stat.Events.StatChanged(CharacterStatType.MaxMana), OnStatChanged, this);
        }

        void TryReplaceStatsNextFrame() {
            if (_nextFrameNumber.Equals(Time.frameCount + 1)) {
                return;
            }
            _nextFrameNumber = Time.frameCount + 1;
            if (_cts != null) {
                return;
            }
            ReplaceStatsNextFrame().Forget();
        }

        async UniTaskVoid ReplaceStatsNextFrame() {
            _cts = new CancellationTokenSource();
            while (Time.frameCount < _nextFrameNumber) {
                if (!await AsyncUtil.DelayFrame(this, cancellationToken: _cts.Token)) {
                    return;
                }
            }
            _cts = null;
            ReplaceStats();
        }

        void ReplaceStats() {
            _disableListeners = true;

            var hpStat = ParentModel.Stat(AliveStatType.MaxHealth);
            var spStat = ParentModel.Stat(CharacterStatType.MaxStamina);
            var mpStat = ParentModel.Stat(CharacterStatType.MaxMana);
            
            if (_hpOverride == null) {
                float hpValue = hpStat.ModifiedValue;
                float spValue = spStat.ModifiedValue;
                float mpValue = mpStat.ModifiedValue;

                _hpOverride = new StatTweak(hpStat, spValue, TweakPriority.Override, OperationType.Override, this);
                _spOverride = new StatTweak(spStat, mpValue, TweakPriority.Override, OperationType.Override, this);
                _mpOverride = new StatTweak(mpStat, hpValue, TweakPriority.Override, OperationType.Override, this);
            } else {
                _hpOverride.SwapModifier(0, TweakPriority.Add, OperationType.Add);
                _spOverride.SwapModifier(0, TweakPriority.Add, OperationType.Add);
                _mpOverride.SwapModifier(0, TweakPriority.Add, OperationType.Add);
                
                float hpValue = hpStat.ModifiedValue;
                float spValue = spStat.ModifiedValue;
                float mpValue = mpStat.ModifiedValue;

                _hpOverride.SwapModifier(spValue, TweakPriority.Override, OperationType.Override);
                _spOverride.SwapModifier(mpValue, TweakPriority.Override, OperationType.Override);
                _mpOverride.SwapModifier(hpValue, TweakPriority.Override, OperationType.Override);
            }

            _disableListeners = false;
        }

        void OnStatChanged() {
            if (_disableListeners) {
                return;
            }
            TryReplaceStatsNextFrame();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _cts?.Cancel();
        }
    }
}
