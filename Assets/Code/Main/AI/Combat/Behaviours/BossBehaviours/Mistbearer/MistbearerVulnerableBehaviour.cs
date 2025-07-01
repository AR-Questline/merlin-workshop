using System;
using Awaken.TG.Main.AI.Combat.Attachments.Bosses;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BossBehaviours.Mistbearer {
    [Serializable]
    public partial class MistbearerVulnerableBehaviour : CustomEnemyBehaviour<MistbearerCombatBase> {
        [SerializeField] float duration = 15f;
        [SerializeField] bool addArmorWhileVulnerable;
        [SerializeField, ShowIf(nameof(addArmorWhileVulnerable))] float armorBonus = -33f;
        
        public override int Weight => 0;
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => false;
        public override bool RequiresCombatSlot => false;
        public override bool IsPeaceful => false;
        protected override NpcStateType StateType => NpcStateType.CustomEnter;
        protected override NpcFSMType FSMType => NpcFSMType.CustomActionsFSM;

        bool _isExiting;
        float _timeElapsed;
        Tween _disableShieldTween;
        StatTweak _armorBonus;

        protected override bool OnStart() {
            _isExiting = false;
            _timeElapsed = 0;
            if (addArmorWhileVulnerable) {
                _armorBonus = StatTweak.Add(ParentModel.NpcElement.Stat(AliveStatType.Armor), armorBonus, TweakPriority.Add, this);
            }
            return true;
        }

        public override void Update(float deltaTime) {
            if (_isExiting) {
                return;
            }
            
            _timeElapsed += deltaTime;
            if (_timeElapsed > duration) {
                Exit().Forget();
            }
        }

        public override bool UseConditionsEnsured() => false;

        async UniTaskVoid Exit() {
            _isExiting = true;
            ParentModel.SetAnimatorState(NpcStateType.CustomExit, NpcFSMType.CustomActionsFSM);
            if (await AsyncUtil.WaitWhile(this, () => CustomActionsFSM.CurrentAnimatorState is { Type: NpcStateType.CustomExit })) {
                ParentModel.StartTeleportBehaviour();
            }
        }

        public override void StopBehaviour() {
            if (!_isExiting) {
                ParentModel.SetAnimatorState(NpcStateType.None, NpcFSMType.CustomActionsFSM);
            }
            _disableShieldTween?.Complete();
            _disableShieldTween = null;

            RemoveArmorBonuses();
        }

        public override void BehaviourInterrupted() {
            RemoveArmorBonuses();
        }

        void RemoveArmorBonuses() {
            if (addArmorWhileVulnerable) {
                _armorBonus?.Discard();
                _armorBonus = null;
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop && !_isExiting && !ParentModel.HasBeenDiscarded) {
                ParentModel.SetAnimatorState(NpcStateType.None, NpcFSMType.CustomActionsFSM);
            }
        }
    }
}