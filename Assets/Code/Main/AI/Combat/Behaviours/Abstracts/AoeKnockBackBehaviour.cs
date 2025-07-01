using System;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Utility.Animations;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    public partial class AoeKnockBackBehaviour : KnockBackAttackBehaviour {
        [SerializeField] AoeTargetType spellCastPositionType;
        [SerializeField, ShowIf(nameof(ShowMovementOffset))] float adversaryPositionAfterSeconds = 2f;
        [SerializeField] float vfxRandomOffset;
        [SerializeField] Vector3 vfxSpawnPointOffset;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference prepareAttackVFX;
        [SerializeField] bool snapToGround;
        [SerializeField] bool parentVfxToCaster;
        
        Vector3 _explosionVfxSpawnPointAfterPreparation;
        bool _wasAttackPrepared;
        IPooledInstance _prepareAttackVFXInstance;

        public override bool CanBeUsed => CanSeeTarget();
        protected override NpcStateType StateType => animatorStateType;
        protected override MovementState OverrideMovementState => new NoMove();
        Quaternion CharacterRotation => ParentModel.DefaultVFXParent.rotation;
        
        bool ShowMovementOffset => spellCastPositionType == AoeTargetType.AdversaryConsideringItsMovement;
        
        protected override bool OnStart() {
            _wasAttackPrepared = false;
            return true;
        }
        
        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            CastSpell(animationEvent).Forget();
        }

        async UniTaskVoid CastSpell(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackStart) {
                if (prepareAttackVFX.IsSet) {
                    _explosionVfxSpawnPointAfterPreparation = GetSpellPosition();
                    Vector3 position = Ground.SnapToGround(_explosionVfxSpawnPointAfterPreparation);
                    Transform parent = null;
                    if (parentVfxToCaster) {
                        position -= ParentModel.Coords;
                        parent = ParentModel.DefaultVFXParent;
                    }
                    _prepareAttackVFXInstance = await PrefabPool.Instantiate(prepareAttackVFX, position, Quaternion.identity, parent);
                    _wasAttackPrepared = true;
                }
            }

            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                SpawnDamageSphere(_wasAttackPrepared ? _explosionVfxSpawnPointAfterPreparation : GetSpellPosition());
                ReturnPrepareAttackVFXInstance();
            }
        }

        void ReturnPrepareAttackVFXInstance() {
            if (_prepareAttackVFXInstance != null) {
                VFXUtils.StopVfxAndReturn(_prepareAttackVFXInstance, PrefabPool.DefaultVFXLifeTime);
                _prepareAttackVFXInstance = null;
            }
        }

        protected override void BehaviourExit() {
            ReturnPrepareAttackVFXInstance();
            base.BehaviourExit();
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            ReturnPrepareAttackVFXInstance();
            base.OnDiscard(fromDomainDrop);
        }

        Vector3 GetSpellPosition() {
            Vector3 spellPosition;
            if (spellCastPositionType == AoeTargetType.SpellCaster) {
                spellPosition = ParentModel.Coords;
            } else if (spellCastPositionType == AoeTargetType.Adversary) {
                spellPosition = ParentModel.NpcElement.GetCurrentTarget().Coords;
            } else if (spellCastPositionType == AoeTargetType.AdversaryConsideringItsMovement) {
                var target = ParentModel.NpcElement.GetCurrentTarget();
                Vector3 movementPositionOffset = CombatBehaviourUtils.GetPredictedMovementPositionOffset(target, adversaryPositionAfterSeconds);
                spellPosition = target.Coords + movementPositionOffset;
            } else if (spellCastPositionType == AoeTargetType.MainHand) {
                spellPosition = ParentModel.NpcElement.MainHand.position;
            } else if (spellCastPositionType == AoeTargetType.OffHand) {
                spellPosition = ParentModel.NpcElement.OffHand.position;
            } else {
                spellPosition = ParentModel.Coords;
            }

            spellPosition += CharacterRotation * vfxSpawnPointOffset;

            if (vfxRandomOffset > 0) {
                spellPosition += (UnityEngine.Random.insideUnitCircle * vfxRandomOffset).X0Y();
            }

            if (snapToGround) {
                spellPosition = Ground.SnapToGround(spellPosition);
            }

            return spellPosition;
        }

        bool CanSeeTarget() {
            var target = ParentModel.NpcElement.GetCurrentTarget();
            return target != Hero.Current || AIEntity.CanSee(target.AIEntity, false) == VisibleState.Visible;
        }

        [Serializable]
        enum AoeTargetType : byte {
            SpellCaster,
            Adversary,
            AdversaryConsideringItsMovement,
            MainHand,
            OffHand
        }
    }
}
