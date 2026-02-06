using Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.VisualGraphUtils;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BossBehaviours.Tristan {
    public class TristanPickUpTrident : FireballBehaviour {
        [SerializeField] NpcStateType loopType = NpcStateType.MagicLoopHold;

        bool _casted;
        
        public override int Weight => (int) (base.Weight * TristanBoss.GetTridentPickUpPriorityMultiplier());
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
        public override bool IsPeaceful => false;
        public override bool UseConditionsEnsured() => TristanBoss.CurrentPhase == 1 && TristanBoss.IsTridentWaiting;
        Attachments.Customs.Tristan TristanBoss => (Attachments.Customs.Tristan) ParentModel;

        protected override bool IsInValidState => base.IsInValidState || NpcGeneralFSM.CurrentAnimatorState.Type == loopType;

        protected override bool StartBehaviour() {
            _casted = false;
            return base.StartBehaviour();
        }
        
        public override void Update(float deltaTime) {
            base.Update(deltaTime);
            if (NpcGeneralFSM.CurrentAnimatorState.Type == loopType && !_casted) {
                // Safety check in case CastSpell wasn't called during the animation event for some reason
                CastSpell().Forget();
            }
        }
        
        protected override async UniTask CastSpell(bool returnFireballInHandAfterSpawned = true) {
            ICharacterView parentView = ParentModel.NpcElement.CharacterView;
            CombatBehaviourUtils.FireProjectileParams fireParams = GetFireParams(parentView);
                
            VGUtils.ShootParams shootParams = VGUtils.ShootParams.Default;
            shootParams.shooter = ParentModel.NpcElement;
            shootParams.upDirection = GetSpellUpDirection();
            shootParams.rawDamageData = damageData.GetRawDamageData(Npc);
            shootParams.damageTypeData = damageData.GetDamageTypeData(Npc);
            shootParams = shootParams.WithCustomProjectile(projectileData.ToProjectileData());
            TristanBoss.MoveTridentToHand(fireParams, shootParams).Forget();
            _casted = true;
                
            PlaySpecialAttackReleaseAudio();
                
            if (!await AsyncUtil.DelayTime(this, 0.1f) || _fireBallInstance == null) {
                ReturnInstantiatedPrefabs();
                return;
            }

            if (returnFireballInHandAfterSpawned) {
                ReturnInstantiatedPrefabs();
            }
        }
    }
}