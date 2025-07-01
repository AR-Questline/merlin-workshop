using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Times;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.Utilities;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Locations.Actions {
    /// <summary>
    /// Lumbering Interact is used for locations that are supposed to have a specific object that should get hit to drop loot,
    /// that object can respawn using backup objects so they can be hit multiple times in a row.
    /// </summary>
    public partial class LumberingAction : ToolInteractAction, IRefreshedByAttachment<LumberingAttachment> {
        public override ushort TypeForSerialization => SavedModels.LumberingAction;

        const int DefaultHealth = 1;
        const int RestoreTime = 12; //hours
        const int RestoreAmount = 2;

        LumberingAttachment _spec;
        int _backupCount;
        [Saved] int _aliveBackupCount;
        [Saved] ARDateTime _nextRestoreTime;
        IEventListener _restoreListener;
        Transform _activeObject;
        Quaternion _activeObjectDefaultRotation;
        
        protected override bool CanInteractThroughDamage => ParentModel.Interactability == LocationInteractability.Active;
        
        public void InitFromAttachment(LumberingAttachment spec, bool isRestored) {
            _spec = spec;
            _backupCount = spec.BackupObjects.Length;
            _requiredToolType = ToolType.Lumbering;
            _activeObject = spec.activeObject;
            _activeObjectDefaultRotation = _activeObject.localRotation;
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            _aliveBackupCount = _backupCount;
        }

        protected override void OnRestore() {
            base.OnRestore();
            if (_aliveBackupCount != _backupCount) {
                for (int i = _aliveBackupCount; i < _backupCount; i++) {
                    _spec.BackupObjects[i].SetActive(false);
                    _spec.BackupObjects[i].transform.localScale = Vector3.zero;
                }
                _restoreListener = World.Any<GameRealTime>().ListenTo(GameRealTime.Events.GameTimeChanged, RestoreBackupDelay, this);
            }
        }

        protected override void OnLocationFullyInitialized() {
            if (_alive == null) {
                return;
            }
            
            _alive.MaxHealth.SetTo(DefaultHealth, false);
            _alive.Health.SetTo(DefaultHealth, false);
            _alive.HealthElement.ListenTo(HealthElement.Events.BeforeTakenFinalDamage, BeforeDeathHook, this);
                
            if (_aliveBackupCount != 0) {
                ParentModel.SetInteractability(LocationInteractability.Active);
            } else {
                _alive.Trigger(IAlive.Events.ResetFracture, false);
            }
        }

        void BeforeDeathHook(HookResult<HealthElement, Damage> hook) {
            if (!CanInteractThroughDamage) {
                hook.Prevent();
                return;
            }
            
            if (ParentModel.TryGetElement<IAlive>()?.Health.ModifiedValue > hook.Value.Amount) {
                return;
            }
            
            var attacker = hook.Value.DamageDealer;
            AddItemsToAttacker(attacker.Inventory).Forget();
            
            AbstractLocationAction.Interact(attacker, ParentModel);
            DestroyCurrentObject(hook.Value.DamageDealer.ParentTransform.position);
            FMODManager.PlayOneShot(_spec.hitSound, ParentModel.Coords);

            if (_restoreListener == null) {
                var time = World.Any<GameRealTime>();
                _nextRestoreTime = time.WeatherTime + TimeSpan.FromHours(RestoreTime);
                _restoreListener = time.ListenTo(GameRealTime.Events.GameTimeChanged, RestoreBackupDelay, this);
            }
            
            if (_aliveBackupCount > 0) {
                UseBackupObject().Forget();
            }
            hook.Prevent();
        }

        void DestroyCurrentObject(Vector3 attackerPos) {
            var objectTransform = _activeObject.transform;
            attackerPos.y = objectTransform.position.y;
            objectTransform.localRotation = Quaternion.LookRotation(attackerPos - objectTransform.position, Vector3.up);
            _alive.Trigger(IAlive.Events.Fracture, true);
            ParentModel.SetInteractability(LocationInteractability.Inactive);
        }

        void SpawnCurrentObject() {
            var objectTransform = _activeObject.transform;
            objectTransform.localRotation = _activeObjectDefaultRotation;
            objectTransform.localScale = Vector3.zero;
            _alive.Trigger(IAlive.Events.ResetFracture, true);
            objectTransform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutCubic);
            ParentModel.SetInteractability(LocationInteractability.Active);
        }
        
        async UniTaskVoid UseBackupObject() {
            if (!await AsyncUtil.DelayTime(this, 1f)) {
                return;
            }

            _aliveBackupCount--;
            var objectTransform = _spec.BackupObjects[_aliveBackupCount].transform;
            await objectTransform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InCubic)
                .OnComplete(() => objectTransform.gameObject.SetActive(false));

            if (HasBeenDiscarded) {
                return;
            }

            SpawnCurrentObject();
            _alive.MaxHealth.SetTo(DefaultHealth, false);
            _alive.Health.SetTo(DefaultHealth, false);
        }
        
        void RestoreBackupDelay(ARDateTime time) {
            if (_nextRestoreTime > time) {
                return;
            }
            _nextRestoreTime += TimeSpan.FromHours(RestoreTime);
            int aliveCountBeforeRestored = _aliveBackupCount;
            _aliveBackupCount += RestoreAmount;
            if (_aliveBackupCount >= _backupCount) {
                _aliveBackupCount = _backupCount;
                World.EventSystem.RemoveListener(_restoreListener);
                _restoreListener = null;
            }

            if (aliveCountBeforeRestored == 0) {
                SpawnCurrentObject();
            }

            for (int i = aliveCountBeforeRestored; i < _aliveBackupCount; i++) {
                var objectTransform = _spec.BackupObjects[i].transform;
                _spec.BackupObjects[i].SetActive(true);
                objectTransform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutCubic);
            }
        }
        
        async UniTaskVoid AddItemsToAttacker(ICharacterInventory inventory) {
            if (!await AsyncUtil.DelayTime(inventory, 0.5f)) {
                return;
            }
            
            if (ParentModel == null || ParentModel.HasBeenDiscarded) {
                return;
            }
            
            var lootTableItems = ItemUtils.GetItemSpawningDataFromLootTable(_spec.lootTable.LootTable(_spec), ParentModel.Spec, this);
            IEnumerable<Item> items = lootTableItems
                .Where(x => x?.ItemTemplate != null)
                .Select(x => new Item(x));
            
            foreach(Item item in items) {
                inventory.Add(item);
            }
        }

#if UNITY_EDITOR
        public override string ModifyName(string original) {
            var displayedInfo = base.ModifyName(original);
            if (displayedInfo != original) {
                return displayedInfo;
            }

            if (original.IsNullOrWhitespace()) {
                Log.Important?.Error($"Fill DisplayName in {MainView.gameObject.name} - {ParentModel.Spec}!");
            }

            return original;
        }
#endif
    }
}