using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Unity.Mathematics;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.MVC;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.HUD.Bars;
using Awaken.TG.Main.Skills;
using Awaken.Utility.GameObjects;
using DG.Tweening;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroHealthBar : VCHeroHUDBar {
        [SerializeField] Transform burnAnimation;
        [SerializeField] Transform poisonAnimation;
        [SerializeField] Transform bleedAnimation;

        protected override StatType StatType => AliveStatType.Health;
        protected override float Percentage => math.clamp(Target.Health?.ModifiedValue / Target.MaxHealthWithReservation ?? 1f, 0f, 1f);
        protected override float PredictionPercentage => (Target.Health?.ModifiedValue + Target.HealthRegen?.PredictedModification) / Target.MaxHealthWithReservation ?? 1f;
        protected override bool EnableAutoValueDropAnimation => false;
        
        readonly Dictionary<Transform, Sequence> _running = new();

        protected override void Init() {
            base.Init();
            Target.Element<HealthElement>().ListenTo(HealthElement.Events.OnDamageTaken, OnDamageTaken, this);
            Target.Statuses.ListenTo(CharacterStatuses.Events.AddedStatus, OnStatusAdd, this);
            Target.Statuses.ListenTo(Model.Events.AfterElementsCollectionModified, OnStatusRemove, this);
        }
        
        void OnStatusAdd(Status status) {
            if (status == null || status.Character != Hero.Current) return;
            var keywords = status.Keywords.ToArray();
            
            if (keywords.Contains(Keyword.StatusBurn)) {
                PlayAnimation(burnAnimation);
            } 
            if (keywords.Contains(Keyword.StatusPoison)) {
                PlayAnimation(poisonAnimation);
            } 
            if (keywords.Contains(Keyword.StatusBleed)) {
                PlayAnimation(bleedAnimation);
            }
        }
        
        void OnStatusRemove() {
            var state = OnStatusChanged();
            
            if (!state.hasBurn) {
                HideAnimation(burnAnimation);
            } 
            if (!state.hasPoison) {
                HideAnimation(poisonAnimation);
            }
            if (!state.hasBleed) {
                HideAnimation(bleedAnimation);
            }
        }
        
        (bool hasBurn, bool hasPoison, bool hasBleed) OnStatusChanged() {
            bool hasBurn = false;
            bool hasPoison = false;
            bool hasBleed = false;

            foreach (var status in Target.Statuses.AllStatuses) {
                if (!hasBurn && status.Keywords.Contains(Keyword.StatusBurn)) {
                    hasBurn = true;
                }
                if (!hasPoison && status.Keywords.Contains(Keyword.StatusPoison)) {
                    hasPoison = true;
                }
                if (!hasBleed && status.Keywords.Contains(Keyword.StatusBleed)) {
                    hasBleed = true;
                }
                
                if (hasBurn && hasPoison && hasBleed) {
                    break;
                }
            }

            return (hasBurn, hasPoison, hasBleed);
        }

        void OnDamageTaken(DamageOutcome outcome) {
            var dmg = outcome.Damage;
            if (dmg == null || outcome.FinalAmount <= 0f || dmg.IsDamageOverTime) return;

            TryPlayAnimation();
        }
        
        void PlayAnimation(Transform anim) {
            if (anim == null || Bar is not GlowingBar glowingBar) return;
            if (_running.TryGetValue(anim, out var sequence) && sequence.IsPlaying()) {
                sequence.Kill();
            }
            
            anim.position = glowingBar.Indicator.position;
            anim.TrySetActiveOptimized(true);
            
            var seq = DOTween.Sequence()
                .AppendCallback(() => anim.position = glowingBar.Indicator.position)
                .SetLoops(-1)
                .SetUpdate(true)
                .OnKill(() => HideAnimation(anim));
            
            if (!_running.TryAdd(anim, seq)) {
                _running[anim] = seq;
            }
        }

        void HideAnimation(Transform anim) {
            anim.TrySetActiveOptimized(false);
            _running.Remove(anim);
        }

        protected override void OnDiscard() {
            base.OnDiscard();
            
            foreach (var kv in _running.ToArray()) {
                if (kv.Key != null) {
                    _running[kv.Key]?.Kill();
                }
            }
            
            _running.Clear();
        }
    }
}