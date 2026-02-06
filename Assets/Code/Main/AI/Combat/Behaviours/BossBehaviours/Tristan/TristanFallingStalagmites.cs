using Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.VisualGraphUtils;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BossBehaviours.Tristan {
    public class TristanFallingStalagmites : FireballBehaviour {
        public override int Weight => (int) (base.Weight * TristanBoss.StalagmitesPriorityMultiplier);
        public override bool CanBeInterrupted => false;
        public override bool UseConditionsEnsured() => !TristanBoss.AnyStalagmites;
        Attachments.Customs.Tristan TristanBoss => (Attachments.Customs.Tristan) ParentModel;
        
        protected override async UniTask CastSpell(bool returnFireballInHandAfterSpawned = true) {
            ICharacterView parentView = ParentModel.NpcElement.CharacterView;
            CombatBehaviourUtils.FireProjectileParams fireParams = GetFireParams(parentView);
                
            VGUtils.ShootParams shootParams = VGUtils.ShootParams.Default;
            shootParams.shooter = ParentModel.NpcElement;
            shootParams.upDirection = GetSpellUpDirection();
            shootParams.rawDamageData = damageData.GetRawDamageData(Npc);
            shootParams.damageTypeData = damageData.GetDamageTypeData(Npc);
            shootParams = shootParams.WithCustomProjectile(projectileData.ToProjectileData());
            TristanBoss.CreateFallingStalagmites(fireParams, shootParams, KnockdownType, KnockdownStrength).Forget();
                
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