using System.Linq;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Attachments.Humanoids;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Combat.Behaviours.RangedBehaviours;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.FPP;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class BowShootingInteraction : SimpleInteractionBase {
        [SerializeField] Transform shootingTarget;
        [SerializeField] GameObject arrowProjectilePrefab;

        Item _usedItem;
        BowBehaviour _bowBehaviour;
        IEventListener _weaponAnimationEventsListener;
        FakeBowEventsListener _fakeBowEventsListener;

        protected override void BeforeDelayStart(NpcElement npc) {
            World.EventSystem.TryDisposeListener(ref _weaponAnimationEventsListener);
            var behaviourOwner = npc.ParentModel?.TryGetElement<IBehavioursOwner>();
            _weaponAnimationEventsListener = behaviourOwner?.ListenTo(EnemyBaseClass.Events.AnimationEvent, OnWeaponAnimationEvent, npc);
        }

        protected override void AfterDelayStart(NpcElement npc) {
            EquipWeapons(npc);
        }

        protected override void BeforeDelayExit(NpcElement npc, InteractionStopReason reason) {
            World.EventSystem.TryDisposeListener(ref _weaponAnimationEventsListener);
            _bowBehaviour?.SetFakeBow(null);
            _bowBehaviour?.SetFakeTarget(Vector3.zero);
        }

        void OnWeaponAnimationEvent(ARAnimationEvent animationEvent) {
            SetBowBehaviour();

            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackStart) {
                _bowBehaviour?.SpawnArrowInHand().Forget();
            } else if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                _bowBehaviour?.SpawnProjectile();
            }
        }

        public void BowPull(float duration) {
            if (_bowBehaviour is { HasBeenDiscarded: true }) {
                ClearBowBehaviour();
                SetBowBehaviour();
                return;
            }

            _bowBehaviour?.SpawnArrowInHand(1f / duration).Forget();
        }

        void SetBowBehaviour() {
            if (_bowBehaviour is { HasBeenDiscarded: true }) {
                ClearBowBehaviour();
            }

            if (_bowBehaviour == null) {
                _bowBehaviour ??= _interactingNpc.ParentModel.TryGetElement<EnemyBaseClass>().AddElement(new BowBehaviour());
                _bowBehaviour.SetArrowPrefab(arrowProjectilePrefab);
            }
            
            if (_fakeBowEventsListener == null) {
                CharacterBow bow = _interactingNpc.CharacterView.transform.GetComponentInChildren<CharacterBow>(true);
                if (bow != null) {
                    _fakeBowEventsListener = bow.GetComponentInParent<Animator>(true).AddComponent<FakeBowEventsListener>();
                    _fakeBowEventsListener.SetInteraction(this);
                    _bowBehaviour.SetFakeBow(bow);
                    _bowBehaviour.SetFakeTarget(shootingTarget.position);
                }
            }

            if (_fakeBowEventsListener == null) {
                RetrySetBowBehaviour().Forget();
            }
        }

        async UniTaskVoid RetrySetBowBehaviour() {
            if (await AsyncUtil.DelayFrame(this, 3)) {
                SetBowBehaviour();
            }
        }

        void ClearBowBehaviour() {
            _bowBehaviour?.ReturnArrowPrefab();
            _bowBehaviour?.Discard();
            _bowBehaviour = null;
            if (_fakeBowEventsListener != null) {
                Destroy(_fakeBowEventsListener);
                _fakeBowEventsListener = null;
            }
        }

        protected override void ForceEndEffects(NpcElement npc) {
            base.ForceEndEffects(npc);
            UnequipWeapons(npc);
        }

        void EquipWeapons(NpcElement npc) {
            var combatBaseClass = npc.ParentModel.TryGetElement<EnemyBaseClass>();
            _usedItem = npc.Inventory.Items.FirstOrDefault(i => i.IsRanged);
            if (combatBaseClass == null || _usedItem == null) {
                return;
            }

            if (!_usedItem.IsEquipped) {
                npc.Inventory.Equip(_usedItem);
            }
            EquipWeaponBehaviour.AttachWeaponsToHands(combatBaseClass.MainHandItem, combatBaseClass.OffHandItem, npc);
            SetBowBehaviour();
        }

        void UnequipWeapons(NpcElement npc) {
            ClearBowBehaviour();
            if (npc is not {HasBeenDiscarded: false} || npc.IsInCombat() || _usedItem is not {HasBeenDiscarded: false} and not { IsEquipped: true }) {
                return;
            }

            npc.Inventory.Unequip(_usedItem);
        }
    }
}
