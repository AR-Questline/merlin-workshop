using System;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher115_115a : Patcher {
        protected override Version MaxInputVersion => new(1, 15, 1);
        protected override Version FinalVersion => new(1, 15, 2);

        public override void AfterGameLoadedPatch() {
            var hero = Hero.Current;
            if (hero == null) {
                Debug.LogException(new Exception("Hero.Current is null in AfterGameLoadedPatch of " + nameof(Patcher113_114)));
                return;
            }

            TryResimulateSoulFragments(hero);
            TryCleanupDeferredActions();
        }

        static void TryResimulateSoulFragments(Hero hero) {
            var fragments = hero.Development.WyrdSoulFragments;
            var count = fragments.UnlockedFragmentsCount - 1; // Don't count baseline
            if (count <= 0) {
                return;
            }

            WyrdSoulFragmentType[] unlockingOrder = {
                WyrdSoulFragmentType.Prologue,
                WyrdSoulFragmentType.Excalibur,
                WyrdSoulFragmentType.Shield,
                WyrdSoulFragmentType.Helmet,
                WyrdSoulFragmentType.Hob,
            };
            fragments.LockAll();
            bool notificationStateBefore = AdvancedNotificationBuffer.AllNotificationsSuspended;
            AdvancedNotificationBuffer.AllNotificationsSuspended = true;
            for (int i = 0; i < count; i++) {
                fragments.Unlock(unlockingOrder[i]);
            }
            AdvancedNotificationBuffer.AllNotificationsSuspended = notificationStateBefore;
        }

        static void TryCleanupDeferredActions() {
            var system = World.Only<DeferredSystem>();
            CleanupScene(system, CommonReferences.Get.SarrasCampaignSceneReference.Name);
            CleanupScene(system, "");

            static void CleanupScene(DeferredSystem system, string sceneName) {
                var actionsInSarras = system.ActionsByScene(sceneName);
                if (actionsInSarras == null) {
                    return;
                }
                
                string[] guidsToRemoveFromDeferredActions = {
                    "bceaf319958bbf54e8ddb1a4ebda2010", // Spec_SoS_EnemyMonster_T4_Tadpole
                };

                for (int i = actionsInSarras.actions.Count - 1; i >= 0; i--) {
                    if (actionsInSarras.actions[i] is not DeferredActionWithLocationMatch actionWithLocation) {
                        continue;
                    }
                    if (actionWithLocation.Match is not LocationReference.MatchByTemplates matchByTemplates) {
                        continue;
                    }
                    if (actionWithLocation.Execution is not SLocationChangeAttachments.StepExecution) {
                        continue;
                    }
                    if (guidsToRemoveFromDeferredActions.Contains(matchByTemplates.Template?.GUID)) {
                        actionsInSarras.actions.RemoveAt(i);
                    }
                }
            }
        }
    }
}