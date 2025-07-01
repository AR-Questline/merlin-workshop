using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.States.ReturnToSpawn {
    public class StateHealAfterReturn : NpcState<StateReturn> {
        const float MaxTimeInState = 2.5f;
        
        HealingStateType _healingStateType;
        IPooledInstance _spawnedItemInHand;
        HashSet<GameObject> _hiddenItems;
        CancellationTokenSource _itemSpawningCancellationToken;
        
        public bool Healed => _healingStateType == HealingStateType.Healed;
        float _timeInState;

        protected override void OnEnter() {
            base.OnEnter();
            _timeInState = 0;
            _healingStateType = HealingStateType.Healing;
            SpawnItemInHand().Forget();
            IBehavioursOwner behavioursOwner = Npc.ParentModel.TryGetElement<IBehavioursOwner>();
            if (behavioursOwner != null) {
                behavioursOwner.ListenTo(EnemyBaseClass.Events.AnimationEvent, OnAnimationEvent, this);
            } else {
                HealToMax();
            }
        }

        void OnAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                HealToMax();
            }
        }
        
        async UniTaskVoid SpawnItemInHand() {
            _itemSpawningCancellationToken = new CancellationTokenSource();
            var spawned = await AIUtils.UseItemInHand(Npc, GameConstants.Get.defaultHealingItem, _itemSpawningCancellationToken);
            if (_itemSpawningCancellationToken == null || _itemSpawningCancellationToken.IsCancellationRequested) {
                return;
            }

            _spawnedItemInHand = spawned.Instance;
            _hiddenItems = spawned.HiddenItems;
        }

        public override void Update(float deltaTime) {
            if (_healingStateType != HealingStateType.Healing) {
                return;
            }
            
            _timeInState += deltaTime;
            if (_timeInState > MaxTimeInState) {
                HealToMax();
            }
        }

        void HealToMax() {
            Npc.Health.SetToFull();
            PrefabPool.InstantiateAndReturn(GameConstants.Get.defaultHealingVFX, Vector3.zero, Quaternion.identity, 3f, Npc.ParentModel.MainView.transform).Forget();
            _healingStateType = HealingStateType.Healed;
        }

        protected override void OnExit() {
            base.OnExit();
            _healingStateType = HealingStateType.None;
            
            _itemSpawningCancellationToken?.Cancel();
            _itemSpawningCancellationToken = null;

            _spawnedItemInHand?.Return();
            AIUtils.RestoreItemsInHand(ref _hiddenItems);
        }

        enum HealingStateType : byte {
            None = 0,
            Healing = 1,
            Healed = 2
        }
    }
}