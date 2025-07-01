using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Combat.Attachments.Bosses;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Combat;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BossBehaviours.Mistbearer {
    [Serializable]
    public partial class MistbearerTeleportBehaviour : CustomEnemyBehaviour<MistbearerCombatBase> {
        const float VfxReturnDelay = 3f;
        
        [SerializeField] LocationReference teleports;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference teleportVFX;
        [SerializeField] bool addArmorWhileTeleporting;
        [SerializeField, ShowIf(nameof(addArmorWhileTeleporting))] float armorBonus = 33f;
        
        public override int Weight => 0;
        public override bool CanBeInterrupted => true;
        public override bool AllowStaminaRegen => false;
        public override bool RequiresCombatSlot => false;
        public override bool IsPeaceful => false;
        protected override NpcStateType StateType => NpcStateType.AttackGeneric9;
        IEnumerable<Location> Teleports => teleports.MatchingLocations(null);
        
        IPooledInstance _vfxInstance;
        CancellationTokenSource _vfxCancellationTokenSource;
        StatTweak _armorBonus;

        protected override bool OnStart() {
            PrepareVfx().Forget();
            
            if (addArmorWhileTeleporting) {
                _armorBonus = StatTweak.Add(ParentModel.NpcElement.Stat(AliveStatType.Armor), armorBonus, TweakPriority.Add, this);
            }
            
            ParentModel.GenericAttackData = new GenericAttackData {
                canBeExited = true,
                canUseMovement = false,
                isLooping = true
            };
            
            return true;
        }

        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                PlayVfx();
                Teleport();
            }
        }

        public override bool UseConditionsEnsured() => false;

        async UniTaskVoid PrepareVfx() {
            _vfxCancellationTokenSource = new CancellationTokenSource();
            _vfxInstance = await PrefabPool.Instantiate(teleportVFX, Vector3.zero, Quaternion.identity, parent: ParentModel.ParentModel.MainView.transform, cancellationToken: _vfxCancellationTokenSource.Token);
        }

        void PlayVfx() {
            _vfxCancellationTokenSource?.Cancel();
            _vfxCancellationTokenSource = null;
            _vfxInstance?.Instance.GetComponent<VisualEffect>().Play();
            _vfxInstance?.Return(VfxReturnDelay).Forget();
            _vfxInstance = null;
        }

        void Teleport() {
            var target = ParentModel.NpcElement?.GetCurrentTarget();
            var targetPos = target?.Coords ?? ParentModel.Coords;
            var sortedTeleports = Teleports.OrderBy(l => Vector3.SqrMagnitude(l.Coords - Npc.Coords)).ToArray();
            int count = sortedTeleports.Count();
            int mistbearerTeleportIndex = RandomUtil.UniformInt(count / 2, count - 1);
            var mistbearerTeleport = sortedTeleports[mistbearerTeleportIndex]; 
            
            if (ParentModel.CurrentPhase > 0) {
                TeleportDestination[] cloneDestinations = new TeleportDestination[ParentModel.AmountOfCopies];
                sortedTeleports = sortedTeleports.OrderBy(l => Vector3.SqrMagnitude(l.Coords - mistbearerTeleport.Coords)).ToArray();
                mistbearerTeleportIndex = RandomUtil.UniformInt(0, ParentModel.AmountOfCopies);
                int filledClonedDestinations = 0;
                for (int i = 0; i < ParentModel.AmountOfCopies + 1; i++) {
                    if (i == mistbearerTeleportIndex) {
                        mistbearerTeleport = sortedTeleports[i];
                    } else {
                        cloneDestinations[filledClonedDestinations] = new TeleportDestination {
                            position = sortedTeleports[i].Coords, 
                            Rotation = Quaternion.Euler(0, (targetPos - sortedTeleports[i].Coords).ToHorizontal2().Horizontal2ToAngle(), 0)
                        };
                        filledClonedDestinations++;
                    }
                }
                ParentModel.SpawnNewCopies(cloneDestinations);
            }
            
            ParentModel.NpcMovement.Controller.TeleportTo(new TeleportDestination {
                position = mistbearerTeleport.Coords,
                Rotation = Quaternion.Euler(0, (targetPos - mistbearerTeleport.Coords).ToHorizontal2().Horizontal2ToAngle(), 0)
            });
            ParentModel.ParentModel.ListenToLimited(GroundedEvents.AfterTeleported, g => AfterTeleported(g).Forget(), this);
        }

        async UniTaskVoid AfterTeleported(IGrounded _) {
            // Wait unitl All Copies are fully loaded so the first attack isn't off-sync.
            if (!await AsyncUtil.WaitUntil(ParentModel, () => ParentModel.AllCopiesLoaded)) {
                return;
            }
            
            // Wait 2 frames for Distance Update
            if (!await AsyncUtil.DelayFrame(this, 2)) {
                return;
            }

            ParentModel.SetAnimatorState(NpcStateType.MediumRange);
            if (ParentModel.allSummonsKilled) {
                ParentModel.StartBehaviour(ParentModel.SummonBehaviour);
            } else {
                ParentModel.TryToStartNewBehaviourExcept(this);
            }

            if (ParentModel is MistbearerCombat mistbearerCombat) {
                mistbearerCombat.ResetDamageTakenCounters();
            }
            RemoveArmorBonuses();
        }
        
        public override void StopBehaviour() {
            RemoveArmorBonuses();
        }

        public override void BehaviourInterrupted() {
            RemoveArmorBonuses();
        }

        void RemoveArmorBonuses() {
            if (addArmorWhileTeleporting) {
                _armorBonus?.Discard();
                _armorBonus = null;
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            _vfxCancellationTokenSource?.Cancel();
            _vfxCancellationTokenSource = null;
            _vfxInstance?.Return(VfxReturnDelay).Forget();
            _vfxInstance = null;
        }
    }
}