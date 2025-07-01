using Awaken.Utility;
using System;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Combat.Behaviours.MeleeBehaviours;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Bosses {
    [Serializable]
    public partial class WightCombat : BaseBossCombat {
        public override ushort TypeForSerialization => SavedModels.WightCombat;

        [SerializeField] float guardLeaveMaxDistance = 10f;
        [SerializeField] float guardLeaveMinTime = 6f;

        AIBlock _aiBlock;
        WightPhase _currentWightPhase;
        float _guardPhaseTimer;

        protected override void OnInitialize() {
            base.OnInitialize();
            NpcElement.HealthElement.ListenTo(HealthElement.Events.OnDamageBlocked, OnDamageBlocked, this);
        }
        
        void OnDamageBlocked(Damage damage) {
            if (_currentWightPhase == WightPhase.Guard) {
                damage.WithHitSurface(SurfaceType.HitMagic);
            }
        }
        
        protected override void Tick(float deltaTime, NpcElement npc) {
            base.Tick(deltaTime, npc);

            if (_currentWightPhase == WightPhase.Guard) {
                _guardPhaseTimer += deltaTime;
            } else {
                _guardPhaseTimer = 0f;
            }
        }

        protected override void OnBehaviourStarted(IBehaviourBase behaviour) {
            base.OnBehaviourStarted(behaviour);
            
            if (behaviour is DodgeBehaviour) {
                SetWightPhase(WightPhase.Guard);
            } else if (_currentWightPhase is WightPhase.Guard && ShouldLeaveGuard()) {
                SetWightPhase(WightPhase.Aggressive);
            }

            bool ShouldLeaveGuard() {
                return NpcElement?.GetCurrentTarget() != null && _guardPhaseTimer >= guardLeaveMinTime && DistanceToTarget <= guardLeaveMaxDistance;
            }
        }

        protected override void OnBehaviourStopped(IBehaviourBase behaviour) {
            if (behaviour is ChargeIntoBehaviour) {
                SwitchWightPhaseNoTransition();
            } else if (behaviour is PoiseBreakBehaviour) {
                SetWightPhase(WightPhase.Aggressive);
            }
        }

        void SwitchWightPhaseNoTransition() {
            WightPhase oppositePhase = _currentWightPhase switch {
                WightPhase.Aggressive => WightPhase.Guard,
                WightPhase.Guard => WightPhase.Aggressive,
                _ => throw new ArgumentOutOfRangeException()
            };
                
            SetWightPhase(oppositePhase, true);
        }

        void SetWightPhase(WightPhase phase, bool withoutTransition = false) {
            if (_currentWightPhase == phase) {
                return;
            }
            _currentWightPhase = phase;
            if (withoutTransition) {
                SetPhase((int)phase);
            } else {
                SetPhaseWithTransition((int)phase);
            }

            UpdateBlockElement();
        }

        void UpdateBlockElement() {
            bool hasBlockElement = _aiBlock is { HasBeenDiscarded: false };
            
            switch (_currentWightPhase) {
                case WightPhase.Aggressive when hasBlockElement:
                    _aiBlock.Discard();
                    _aiBlock = null;
                    break;
                case WightPhase.Guard when !hasBlockElement:
                    _aiBlock = NpcElement.AddElement<AIBlock>();
                    break;
            }
        }

        enum WightPhase : byte {
            Aggressive = 0,
            Guard = 1,
        }
    }
}