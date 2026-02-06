using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.Main.Utility.RichLabels.SO;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher115_115c : Patcher {
#if UNITY_PS5
        protected override Version MaxInputVersion => new(1, 15, 32);
        protected override Version FinalVersion => new(1, 15, 33);
#else
        protected override Version MaxInputVersion => new(1, 15, 31);
        protected override Version FinalVersion => new(1, 15, 32);
#endif

        public override void AfterGameLoadedPatch() {
            var hero = Hero.Current;
            if (hero == null) {
                Debug.LogException(new Exception("Hero.Current is null in AfterGameLoadedPatch of " + nameof(Patcher115_115c)));
                return;
            }
            
            TryCleanupDeferredActions();
        }

        static void TryCleanupDeferredActions() {
            var system = World.Only<DeferredSystem>();
            CleanupScene(system, "");

            static void CleanupScene(DeferredSystem system, string sceneName) {
                var actionsInScene = system.ActionsByScene(sceneName);
                if (actionsInScene == null) {
                    return;
                }

                for (int i = actionsInScene.actions.Count - 1; i >= 0; i--) {
                    if (TryRemoveSheppardActor(actionsInScene.actions[i]) || TryRemoveSheppardPresence(actionsInScene.actions[i])
                        || TryRemoveGateguardBackup(actionsInScene.actions[i]) || TryRemoveHoloOrrinEntrance(actionsInScene.actions[i])) {
                        actionsInScene.actions.RemoveAt(i);
                    }
                }

                static bool TryRemoveSheppardActor(DeferredAction action) {
                    if (action is not DeferredActionWithLocationMatch actionWith) {
                        return false;
                    }
                    if (actionWith.Match is not LocationReference.MatchByActor matchBy) {
                        return false;
                    }
                    if (actionWith.Execution is not SNpcTurnIntoGhost.StepExecution) {
                        return false;
                    }
                    if ("b61d9b71-0ebb-4eed-9a1b-a68f9af1aee8".Equals(matchBy.ActorRefGuid)) {
                        return true;
                    }
                    return false;
                }

                static bool TryRemoveSheppardPresence(DeferredAction action) {
                    if (action is not DeferredActionWithPresenceMatch actionWith) {
                        return false;
                    }
                    if (actionWith.PresenceData.richLabelSet.richLabelGuids.Count != 2) {
                        return false;
                    }
                    if (!actionWith.PresenceData.richLabelSet.richLabelGuids[0].Equals("b61d9b71-0ebb-4eed-9a1b-a68f9af1aee8")) {
                        return false;
                    }
                    if (!actionWith.PresenceData.richLabelSet.richLabelGuids[1].Equals("ca6f8a71e97460e4189d61433fd7e1ff")) {
                        return false;
                    }
                    if (actionWith.Execution is not SActivateNpcPresenceViaRichLabels.StepExecution) {
                        return false;
                    }
                    return true;
                }

                static bool TryRemoveGateguardBackup(DeferredAction action) {
                    if (action is not DeferredActionWithLocationMatch actionWith) {
                        return false;
                    }
                    if (actionWith.Match is not LocationReference.MatchByAllTags matchBy) {
                        return false;
                    }
                    if (actionWith.Execution is not SLocationChangeAttachments.StepExecution) {
                        return false;
                    }
                    if (matchBy.Tags.Length == 1 && "hos:gateguardbackup".Equals(matchBy.Tags[0])) {
                        return true;
                    }
                    return false;
                }

                static bool TryRemoveHoloOrrinEntrance(DeferredAction action) {
                    if (action is not DeferredActionWithLocationMatch actionWith) {
                        return false;
                    }
                    if (actionWith.Match is not LocationReference.MatchByAllTags matchBy) {
                        return false;
                    }
                    if (actionWith.Execution is not SLocationDiscard.StepExecution) {
                        return false;
                    }
                    if (matchBy.Tags.Length == 1 && ("HoloOrrin:Entrance".Equals(matchBy.Tags[0]) 
                        || "HoloOrrin:Floor".Equals(matchBy.Tags[0]) || "HoloOrrin:Basement".Equals(matchBy.Tags[0]))) {
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}