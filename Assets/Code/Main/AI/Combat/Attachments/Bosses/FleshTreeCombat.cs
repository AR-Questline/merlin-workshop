using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Bosses {
    [UnityEngine.Scripting.Preserve]
    public partial class FleshTreeCombat: BaseBossCombat {
        public override ushort TypeForSerialization => SavedModels.FleshTreeCombat;

        [Saved] bool _inRage;
        StatTweak _movementSpeedMultiplierTweak;
        IEventListener _healthDecreaseListener;
        
        protected override void AfterVisualLoaded(Transform transform) {
            base.AfterVisualLoaded(transform);
            SetInitialState();
        }

        void SetInitialState() {
            if (_inRage) {
                SetPhase(1);
            } else {
                _healthDecreaseListener = NpcElement.ListenTo(Stat.Events.StatChanged(AliveStatType.Health), OnHealthChanged, this);
            }
        }
        
        void OnHealthChanged(Stat stat) {
            if (HasBeenDiscarded) return;
            
            var health = (LimitedStat)stat;
            if (health.Percentage <= 0.5f) {
                SetPhaseWithTransition(1);
                World.EventSystem.TryDisposeListener(ref _healthDecreaseListener);
            }
        }

        protected override void OnPhaseTransitionFinished(int phase) {
            if (phase >= 1) {
                if (_movementSpeedMultiplierTweak == null || _movementSpeedMultiplierTweak.HasBeenDiscarded) {
                    _movementSpeedMultiplierTweak = StatTweak.Multi(CharacterStats.MovementSpeedMultiplier, 1.5f, TweakPriority.Multiply, this);
                    _movementSpeedMultiplierTweak.MarkedNotSaved = true;
                }
            } else {
                _movementSpeedMultiplierTweak?.Discard();
                _movementSpeedMultiplierTweak = null;
            }
        }
    }
}