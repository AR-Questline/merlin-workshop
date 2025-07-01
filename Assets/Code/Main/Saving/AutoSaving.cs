using System;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Saving {
    public class AutoSaving : IDomainBoundService {
        const float MinimumDelayTime = 20f;
        const string AutoSaveId = "auto_save";
        const float IntervalUntilSaveIsPossible = 5f;
        float _lastSaveTime;

        public Domain Domain => Domain.Gameplay;
        public bool RemoveOnDomainChange() {
            World.Services.Get<RecurringActions>().UnregisterAction(AutoSaveId);
            return true;
        }

        static bool CanAutoSave(bool checkSavingMarker = true) => LoadSave.Get.CanAutoSave(checkSavingMarker);
        static bool IsSafeToSave() => UIStateStack.Instance.State.IsMapInteractive && LoadSave.Get.HeroStateAllowsSave();
        // We want to AutoSave after rest always event even on SurvivalMode so we bypass CanAutoSave check here.
        static bool CanSaveAfterRest(bool checkSavingMarker = true) => World.Only<AutoSaveSetting>().Enabled && LoadSave.Get.CanSystemSave(checkSavingMarker);

        public void Init() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Hero.Events.ArrivedAtPortal, this, p => AutoSaveAfterTeleport(p).Forget());
            RegisterAutoSave();
            World.Only<AutoSaveSetting>().ListenTo(Setting.Events.SettingChanged, _ => RefreshAutoSaveCooldown(false), this);
        }

        void RegisterAutoSave() {
            World.Services.Get<RecurringActions>().RegisterAction(AutoSaveWithRecurringRetry, AutoSaveId, World.Only<AutoSaveSetting>().Interval, false);
            World.EventSystem.ListenTo(EventSelector.AnySource, Hero.Events.AfterHeroRested, this, () => AutoSave(CanSaveAfterRest).Forget());
        }

        public void AutoSaveWithRecurringRetry() {
            TryAutoSave().Forget();
        }
        
        async UniTaskVoid TryAutoSave() {
            if (!await AutoSave(CanAutoSave)) {
                // Auto saving failed, change interval and wait until it succeeds
                RefreshAutoSaveCooldown(true);
            }
        }

        void RefreshAutoSaveCooldown(bool retry) {
            if (retry) {
                World.Services.Get<RecurringActions>().UpdateActionInterval(AutoSaveId, IntervalUntilSaveIsPossible, false);
            } else {
                World.Services.Get<RecurringActions>().UpdateActionInterval(AutoSaveId, World.Only<AutoSaveSetting>().Interval, false);
            }
        }

        async UniTask<bool> AutoSave(Func<bool, bool> requirement, bool ignoreMinimumDelay = false) {
            if (!requirement(true)) {
                return false;
            }

            SavingWorldMarker.Add(UniTask.DelayFrame(2), true);
            await UniTask.NextFrame();
            if (!requirement(false)) {
                return false;
            }
            
            float time = Time.realtimeSinceStartup;
            if (ignoreMinimumDelay || time - _lastSaveTime > MinimumDelayTime) {
                _lastSaveTime = time;
                LoadSave.Get.Save(SaveSlot.GetAutoSave());
                RefreshAutoSaveCooldown(false);
                return true;
            }

            return false;
        }

        async UniTaskVoid AutoSaveAfterTeleport(Portal portal) {
            if (portal.DebugFastPortal || portal.DoNotAutoSaveAfterPortaling) {
                return;
            }
            
            Hero hero = Hero.Current;
            bool success = await AsyncUtil.DelayFrame(hero, 3) && await AsyncUtil.WaitUntil(hero, IsSafeToSave);
            if (success) {
                AutoSave(CanAutoSave, ignoreMinimumDelay: true).Forget();
            }
        }
    }
}