using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.VisualGraphUtils;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours {
    [Serializable]
    public partial class FireballBehaviour : SpellCastingBehaviourBase {
        const float FireBallVelocityMultiplier = 2.5f;
        
        // === Serialized Fields
        [SerializeField] bool predictPlayerMovement = true;
        [SerializeField] bool overrideFireAngleRange;
        [SerializeField, ShowIf(nameof(overrideFireAngleRange))] FloatRange fireAngleRange = new(-60f, 60f);
        [SerializeField] float maxFireBallVelocity = 50;
        [SerializeField, Range(0, 1f), InfoBox("0 - aim at enemy feet, 1 - aim at enemy head")] float aimAtTargetHeight = 0.85f;
        [SerializeField, Range(0, 5f), InfoBox("0 - hit target, 1 - hit +/- 1m from the target")] float inaccuracyOffset = 0f;
        [SerializeField] bool useParabolicShot;
        [SerializeField, ShowIf(nameof(useParabolicShot))] bool useHighParabolicShot;
        [SerializeField] ItemProjectileAttachment.ItemProjectileData projectileData = new();
        [SerializeField, Range(1, 25)] int projectilesAmount = 1;
        [SerializeField] NpcDamageData damageData = NpcDamageData.DefaultMagicAttackData;
        [SerializeField] bool exposeWeakspot;
        
        protected override bool ExposeWeakspot => exposeWeakspot;
        protected override ShareableARAssetReference InHandPrefab => fireBallInHandPrefab is { IsSet: true } ? fireBallInHandPrefab : projectileData.visualPrefab; 

        protected override async UniTask CastSpell(bool returnFireballInHandAfterSpawned = true) {
            ICharacterView parentView = ParentModel.NpcElement.CharacterView;
            for (int i = 0; i < projectilesAmount; i++) {
                CombatBehaviourUtils.FireProjectileParams fireParams = GetFireParams(parentView);
                
                VGUtils.ShootParams shootParams = VGUtils.ShootParams.Default;
                shootParams.shooter = ParentModel.NpcElement;
                shootParams.startPosition = GetSpellPosition();
                shootParams.upDirection = GetSpellUpDirection();
                shootParams.projectileSlotType = EquipmentSlotType.Throwable;
                shootParams.rawDamageData = damageData.GetRawDamageData(Npc);
                shootParams.damageTypeData = damageData.GetDamageTypeData(Npc);
                shootParams = shootParams.WithCustomProjectile(projectileData.ToProjectileData());
                CombatBehaviourUtils.FireProjectile(fireParams, shootParams);
                
                PlaySpecialAttackReleaseAudio();
                
                if (!await AsyncUtil.DelayTime(this, 0.1f) || _fireBallInstance == null) {
                    ReturnInstantiatedPrefabs();
                    return;
                }
            }

            if (returnFireballInHandAfterSpawned) {
                ReturnInstantiatedPrefabs();
            }
        }

        protected virtual CombatBehaviourUtils.FireProjectileParams GetFireParams(ICharacterView parentView) {
            var target = ParentModel.NpcElement?.GetCurrentTarget();
            return new CombatBehaviourUtils.FireProjectileParams {
                shooterView = parentView,
                target = target,
                fireAngleRange = overrideFireAngleRange ? fireAngleRange : CombatBehaviourUtils.DefaultFireAngleRange,
                aimAtEnemyHeight = aimAtTargetHeight,
                maxVelocity = maxFireBallVelocity,
                velocityMultiplier = FireBallVelocityMultiplier,
                predictPlayerMovement = predictPlayerMovement,
                parabolicShot = useParabolicShot,
                highShot = useHighParabolicShot,
                inaccuracy = inaccuracyOffset
            };
        }
    }
}