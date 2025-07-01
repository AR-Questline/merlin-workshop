using System;
using Awaken.TG.Main.AI.Combat.Attachments.Customs;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.VisualGraphUtils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours {
    [Serializable]
    public partial class BarnaclatorFireProjectile : CustomEnemyBehaviour<Barnaclator> {
        [SerializeField] ItemProjectileAttachment.ItemProjectileData projectileData = new();
        [SerializeField] NpcDamageData damageData = NpcDamageData.DefaultRangedAttackData;
        
        public override int Weight => 1;

        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
        public override bool IsPeaceful => false;
        protected override NpcStateType StateType => NpcStateType.MagicProjectile;

        Transform _firePoint;
        ProjectilePreload _preloadedProjectile; 

        protected override bool OnStart() {
            _preloadedProjectile = ItemProjectile.PreloadCustomProjectile(projectileData.logicPrefab.Get(), projectileData.visualPrefab);
            return true;
        }
        
        public override void Update(float deltaTime) {
            if (NpcGeneralFSM.CurrentAnimatorState.Type != StateType) {
                ParentModel.StartWaitBehaviour();
            }
        }

        protected override void BehaviourExit() {
            _preloadedProjectile.Release();
        }

        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent is not BarnclatorFireProjectileEvent projectileEvent) {
                return;
            }
            
            _firePoint = ParentModel.GetFirePoint(projectileEvent.slotIndex);
            if (_firePoint != null) {
                SpawnProjectile();
            }
        }

        public override bool UseConditionsEnsured() => ParentModel.AnyHitboxLeft;
        
        void SpawnProjectile() {
            Vector3 spawnPosition = _firePoint.position;

            var npc = ParentModel.NpcElement;
            var parentView = npc.CharacterView;
            var target = npc?.GetCurrentTarget();
            
            CombatBehaviourUtils.FireProjectileParams fireParams = new() {
                shooterView = parentView,
                target = target,
                fireAngleRange = new(-180f, 180f),
                aimAtEnemyHeight = 0.1f,
                maxVelocity = 25f,
                velocityMultiplier = 1,
                predictPlayerMovement = true,
                parabolicShot = true,
                highShot = true,
            };
            VGUtils.ShootParams shootParams = VGUtils.ShootParams.Default;
            shootParams.shooter = ParentModel.NpcElement;
            shootParams.startPosition = spawnPosition;
            shootParams.projectileSlotType = EquipmentSlotType.Throwable;
            shootParams.rawDamageData = damageData.GetRawDamageData(Npc);
            shootParams.damageTypeData = damageData.GetDamageTypeData(Npc);
            shootParams = shootParams.WithCustomProjectile(projectileData.ToProjectileData());
            ProjectileWrapper projectileInstance = CombatBehaviourUtils.FireProjectile(fireParams, shootParams);
            ParentModel.NpcElement.PlayAudioClip(AliveAudioType.SpecialRelease.RetrieveFrom(ParentModel.NpcElement), true);
        }
    }
}