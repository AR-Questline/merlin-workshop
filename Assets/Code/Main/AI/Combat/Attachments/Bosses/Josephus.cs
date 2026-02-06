using System;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Bosses {
    [UnityEngine.Scripting.Preserve]
    [Serializable]
    public partial class Josephus : BaseBossCombat {
        public override ushort TypeForSerialization => SavedModels.Josephus;

        [SerializeField, Range(0f, 1f)] float chanceToChangePhaseAfterAttack = 0.5f;
        [SerializeField] float phaseChangeCooldown = 15f;
        
        float _phaseChangeCooldown;

        public override bool CanMoveOnPhaseTransition => CurrentPhase != 1;

        // === Initialization
        public override void InitFromAttachment(BossCombatAttachment spec, bool isRestored) {
            if (spec.BossBaseClass is not Josephus josephus) {
                Log.Critical?.Error("Josephus: Spec is not Josephus!");
                return;
            }
            chanceToChangePhaseAfterAttack = josephus.chanceToChangePhaseAfterAttack;
            phaseChangeCooldown = josephus.phaseChangeCooldown;
            _phaseChangeCooldown = phaseChangeCooldown;
            base.InitFromAttachment(spec, isRestored);
        }

        protected override void OnInitializeInternal() {
            base.OnInitializeInternal();
            this.ListenTo(StaggerBehaviour.Events.BeforeStaggerExit, OnStaggerExit, this);
        }

        protected override void Tick(float deltaTime, NpcElement npc) {
            _phaseChangeCooldown -= deltaTime;
            _phaseChangeCooldown = math.max(0, _phaseChangeCooldown);
            base.Tick(deltaTime, npc);
        }

        protected override void OnBehaviourStopped(IBehaviourBase behaviour) {
            if (CurrentPhase > 0 && _phaseChangeCooldown <= 0 && behaviour is AttackBehaviour) {
                if (RandomUtil.UniformFloat(0, 1) <= chanceToChangePhaseAfterAttack) {
                    SetPhaseWithTransition(0);
                }
            }
        }

        protected override void OnPhaseTransitionFinished(int phase) {
            _phaseChangeCooldown = phaseChangeCooldown;
            NpcElement.CharacterStats.Stamina.SetToFull();
        }

        void OnStaggerExit(HookResult<EnemyBaseClass, StaggerBehaviour> result) {
            if (CurrentPhase != 1) {
                SetPhaseWithTransition(1, true);
                result.Prevent();
            }
        }
    }
}