using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Fights.SolarBeam;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Utility.Animations;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours {
    [Serializable]
    public partial class SolarBeamBehaviour : SpellCastingBehaviourBase {
        // === Serialized Fields
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Weapons)]
        ShareableARAssetReference beamVisualPrefab;
        [SerializeField] Vector3 beamVisualOffset;
        [SerializeField] SolarBeamCreationData solarBeamCreationData = SolarBeamCreationData.Default;
        [SerializeField] NpcDamageData damageData = NpcDamageData.DefaultMagicAttackData;
        [SerializeField] bool exposeWeakspot;
        [SerializeField] bool hideInHandVFXAfterCast;
        [SerializeField] bool rotateToHeroWhileCasting;

        GameObject _beamInstance;
        ARAsyncOperationHandle<GameObject> _beamHandle;
        SolarBeam _solarBeam;
        
        protected override bool ExposeWeakspot => exposeWeakspot;
        
        /// <summary>
        /// SpecialAttackStart => Shows InHand VFX
        /// SpecialAttackTrigger => Casts Spell (starts beam)
        /// AttackEnd => End Spell (ends beam)
        /// </summary>
        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.AttackEnd) {
                StopCastingBeam();
            } else {
                base.TriggerAnimationEvent(animationEvent);
            }
        }

        public override void Update(float deltaTime) {
            _solarBeam?.Update(deltaTime);
            base.Update(deltaTime);
        }

        protected override UniTask CastSpell(bool returnFireballInHandAfterSpawned = true) {
            CancelCastingBeam();
            _beamHandle = beamVisualPrefab.Get().LoadAsset<GameObject>();
            _beamHandle.OnComplete(h => {
                if (HasBeenDiscarded) {
                    h.Release();
                    return;
                }
                if (h.Status != AsyncOperationStatus.Succeeded || h.Result == null) {
                    h.Release();
                    Stop();
                    ReturnFireballInHandAfterSpawned();
                    return;
                }
                _beamInstance = Object.Instantiate(h.Result, GetHand());
                _beamInstance.transform.SetLocalPositionAndRotation(beamVisualOffset, Quaternion.identity);
                StartCastingBeam(solarBeamCreationData.Create(damageData.GetRawDamageData(Npc)));
                ReturnFireballInHandAfterSpawned();
                return;

                void ReturnFireballInHandAfterSpawned() {
                    if (returnFireballInHandAfterSpawned && hideInHandVFXAfterCast) {
                        base.ReturnInstantiatedPrefabs();
                    }
                }
            });
            
            return UniTask.CompletedTask;
        }

        void StartCastingBeam(SolarBeamData data) {
            if (rotateToHeroWhileCasting != rotateToTarget) {
                ParentModel.NpcMovement.ResetMainState(_overrideMovementState);
                _overrideMovementState = ParentModel.NpcMovement.ChangeMainState(rotateToHeroWhileCasting ? new NoMoveAndRotateTowardsTarget() : new NoMove());
                ParentModel.NpcMovement.ChangeMainState(_overrideMovementState);
            }
            _solarBeam = new SolarBeam(data, Npc, _beamInstance.transform);
        }

        void StopCastingBeam() {
            ReturnInstantiatedPrefabs();
        }

        protected override void ReturnInstantiatedPrefabs() {
            CancelCastingBeam();
            base.ReturnInstantiatedPrefabs();
        }

        void CancelCastingBeam() {
            if (_beamHandle.IsValid()) {
                _beamHandle.Release();
                _beamHandle = default;
            }

            if (_beamInstance != null) {
                Object.Destroy(_beamInstance);
                _beamInstance = null;
            }
            
            _solarBeam = null;
        }
    }
}