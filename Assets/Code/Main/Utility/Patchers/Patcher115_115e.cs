using System;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher115_115e : Patcher_RestoreOnFastTravelOrSpawn {
        protected override Version MaxInputVersion => new(1, 15, 69);
        protected override Version FinalVersion => new(1, 15, 70);

        public Patcher115_115e() : base(new[] {
            // SceneByGuid("fb416a5607561c543837a4bbffbbd147"), // Dungeon_Archives - cannot process because of Quest Logic :(
            SceneByGuid("20b3c255d1df96f49b262b4cdc2ca619"), // Dungeon_HiddenPassage
        }) { }

        public override void AfterGameLoadedPatch() {
            var hero = Hero.Current;
            if (hero == null) {
                Debug.LogException(new Exception("Hero.Current is null in AfterGameLoadedPatch of " + nameof(Patcher115_115e)));
                return;
            }
            
            TryCleanupDeferredActions();
        }

        static void TryCleanupDeferredActions() {
            var system = World.Only<DeferredSystem>();
            foreach (var actionsBySceneData in system.AllActionsByScenes) {
                CleanupScene(actionsBySceneData);
            }

            static void CleanupScene(DeferredActionsBySceneData actionsInScene) {
                bool alreadyFoundAnselm = false;
                bool alreadyFoundYwain = false;
                bool alreadyFoundMaggot = false;

                for (int i = actionsInScene.actions.Count - 1; i >= 0; i--) {
                    if (TryRemoveYvainPresence(actionsInScene.actions[i], ref alreadyFoundYwain)
                        || TryRemoveAnselmPresence(actionsInScene.actions[i], ref alreadyFoundAnselm) 
                        || TryRemoveMaggotPresence(actionsInScene.actions[i], ref alreadyFoundMaggot)) {
                        actionsInScene.actions.RemoveAt(i);
                    }
                }
                
                static bool TryRemoveAnselmPresence(DeferredAction action, ref bool anselmAlreadyFound) {
                    if (action is not DeferredActionWithPresenceMatch actionWith) {
                        return false;
                    }
                    if (actionWith.PresenceData.richLabelSet.richLabelGuids.Count != 2) {
                        return false;
                    }
                    if (!actionWith.PresenceData.richLabelSet.richLabelGuids[0].Equals("55a1fbf5-dc1d-41ec-aeb8-14bd46e4ff27")) {
                        return false;
                    }
                    if (!actionWith.PresenceData.richLabelSet.richLabelGuids[1].Equals("980723c7c80f0dd4ea08c1c1b19b6c18")) {
                        return false;
                    }
                    if (actionWith.Execution is not SActivateNpcPresenceViaRichLabels.StepExecution { Availability: true }) {
                        return false;
                    }

                    if (anselmAlreadyFound) {
                        return true;
                    } else {
                        anselmAlreadyFound = true;
                        return false;
                    }
                }
                
                static bool TryRemoveYvainPresence(DeferredAction action, ref bool yvainAlreadyFound) {
                    if (action is not DeferredActionWithPresenceMatch actionWith) {
                        return false;
                    }
                    if (actionWith.PresenceData.richLabelSet.richLabelGuids.Count != 3) {
                        return false;
                    }
                    if (!actionWith.PresenceData.richLabelSet.richLabelGuids[0].Equals("06f1e32b-7ea8-4351-ab52-7c1b2ce1063c")) {
                        return false;
                    }
                    if (!actionWith.PresenceData.richLabelSet.richLabelGuids[1].Equals("980723c7c80f0dd4ea08c1c1b19b6c18")) {
                        return false;
                    }
                    if (!actionWith.PresenceData.richLabelSet.richLabelGuids[2].Equals("9ae0a403-dc61-4162-b133-4fd77878a71b")) {
                        return false;
                    }
                    if (actionWith.Execution is not SActivateNpcPresenceViaRichLabels.StepExecution { Availability: true }) {
                        return false;
                    }

                    if (yvainAlreadyFound) {
                        return true;
                    } else {
                        yvainAlreadyFound = true;
                        return false;
                    }
                }
                
                static bool TryRemoveMaggotPresence(DeferredAction action, ref bool maggotAlreadyFound) {
                    if (action is not DeferredActionWithPresenceMatch actionWith) {
                        return false;
                    }
                    if (actionWith.PresenceData.richLabelSet.richLabelGuids.Count != 2) {
                        return false;
                    }
                    if (!actionWith.PresenceData.richLabelSet.richLabelGuids[0].Equals("9febb25b-f51f-4983-9f59-5d6867f3ad20")) {
                        return false;
                    }
                    if (!actionWith.PresenceData.richLabelSet.richLabelGuids[1].Equals("3690892f7c666ca42844e2e8adf92534")) {
                        return false;
                    }
                    if (actionWith.Execution is not SActivateNpcPresenceViaRichLabels.StepExecution { Availability: true }) {
                        return false;
                    }

                    if (maggotAlreadyFound) {
                        return true;
                    } else {
                        maggotAlreadyFound = true;
                        return false;
                    }
                }
            }
        }
    }
}