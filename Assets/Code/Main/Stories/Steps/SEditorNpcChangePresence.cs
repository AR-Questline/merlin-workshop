using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.Main.Utility.RichLabels.SO;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    
    [Element("NPC/NPC: Change Presence")]
    public class SEditorNpcChangePresence : EditorStep {
        [InfoBox("Activates all matching presences in a current scene and moves their NPCs to them.\n" +
                 "Can be used to quickly move NPCs to different presences within the same scene.")]
        
        public RichLabelUsage richLabelUsage = new(RichLabelConfigType.Presence);
        
        [Tooltip("Defines how the presence change should be handled.")]
        public SNpcChangePresence.ChangeType changeType = SNpcChangePresence.ChangeType.SwitchPermanently;
        [Tooltip("Matching presences will move physically or will be teleported to the destination.")]
        public Travel travelMode = Travel.Teleport;
        [ShowIf(nameof(ShouldChangeTemporarily))]
        [Tooltip("When story is finished, affected NPCs will be teleported or moved back to their original positions.")]
        public Travel returnTravelMode = Travel.Teleport; 
        [Tooltip("Should the NPC be invulnerable during the story?")]
        public bool invulnerability = true;
        [Tooltip("Should the NPC be involved in Story (play story loop animation and look at their dialogue target)?")]
        public bool involve = true;
        [ShowIf(nameof(involve)), Tooltip("Should the NPC instantly rotate to the Hero?")]
        public bool rotateToHero = false;
        [Tooltip("Should the story wait for every NPC involved? If not, NPCs might not be ready for next steps.")]
        public bool waitForAllFinished = false;
        
        bool ShouldChangeTemporarily => changeType is SNpcChangePresence.ChangeType.SwitchTemporarily;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcChangePresence {
                richLabelUsage = richLabelUsage,
                changeType = changeType,
                travelMode = travelMode,
                returnTravelMode = returnTravelMode,
                invulnerability = invulnerability,
                involve = involve,
                rotateToHero = rotateToHero,
                waitForAllFinished = waitForAllFinished
            };
        }
    }

    public partial class SNpcChangePresence : StoryStep {
        public RichLabelUsage richLabelUsage;
        public ChangeType changeType;
        public Travel travelMode;
        public Travel returnTravelMode; 
        public bool invulnerability;
        public bool involve;
        public bool rotateToHero;
        public bool waitForAllFinished;
        
        bool ShouldChangeTemporarily => changeType is ChangeType.SwitchTemporarily;
        bool ShouldSwitchPermanently => changeType is ChangeType.SwitchPermanently;
        bool TeleportIn => travelMode is Travel.Teleport;
        bool TeleportBack => returnTravelMode is Travel.Teleport;
        
        public override StepResult Execute(Story story) {
            var result = new StepResult();
            ChangeAllMatchingPresences(story, result).Forget();
            return result;
        }
        
        async UniTaskVoid ChangeAllMatchingPresences(Story api, StepResult result) {
            var presences = GetAllMatchingPresencesPerNpc();
            if (presences.Count == 0) {
                result.Complete();
                return;
            }

            var changePresenceTasks = waitForAllFinished 
                ? new UniTask[presences.Count]
                : Array.Empty<UniTask>();
            int taskIndex = 0;
            
            foreach ((LocationTemplate key, FrugalList<NpcPresence> presenceList) in presences) {
                if (presenceList.Count > 1) {
                    Log.Minor?.Error($"Multiple matching presences for single NPC {key} {presenceList[0].AliveNpc} in step {this}." +
                                       $"This will yield unexpected results and should be resolved.");
                }

                var task = ChangePresence(new PresenceChangeStepInfo {
                    presencesToSet = presenceList,
                    api = api,
                });
                
                if (waitForAllFinished) {
                    changePresenceTasks[taskIndex] = task;
                    taskIndex++;
                }
            }
            
            if (waitForAllFinished) {
                await AsyncUtil.WaitForAll(api, changePresenceTasks);
            }
            
            result.Complete();
        }
        
        Dictionary<LocationTemplate, FrugalList<NpcPresence>> GetAllMatchingPresencesPerNpc() {
            Dictionary<LocationTemplate, FrugalList<NpcPresence>> presenceMapping = new();
            var presences = GetAllMatchingPresences();
            foreach (var presence in presences) {
                var key = presence.Template;
                if (!presenceMapping.TryGetValue(key, out var presenceList)) {
                    presenceList = new FrugalList<NpcPresence>();
                }
                presenceList.Add(presence);
                presenceMapping[key] = presenceList;
            }
            return presenceMapping;
        }

        List<NpcPresence> GetAllMatchingPresences() {
            List<NpcPresence> presences = new();
            var richLabelGuids = richLabelUsage.RichLabelUsageEntries;
            foreach (var location in World.All<Location>()) {
                var presence = location.TryGetElement<NpcPresence>();
                if (presence is null or { IsManual: false }) {
                    continue;
                }
                if (!RichLabelUtilities.IsMatchingRichLabel(presence.RichLabelSet, richLabelGuids)) {
                    continue;
                }
                presences.Add(presence);
            }
            return presences;
        }

        async UniTask ChangePresence(PresenceChangeStepInfo stepInfo) {
            var npc = stepInfo.presencesToSet[0].AliveNpc;
            
            if (npc != null) {
                await stepInfo.api.SetupLocation(npc.ParentModel, false, false, true, false);
                if (travelMode is Travel.Move) {
                    npc.Interactor.Stop(InteractionStopReason.ChangeInteraction, false);
                }
            }

            if (ShouldChangeTemporarily) {
                SwitchPresenceTemporarily(stepInfo);
            } else {
                SwitchPresence(stepInfo, ShouldSwitchPermanently);
            }

            if (npc != null) {
                if (!await WaitForNpcToFullyEnterInteraction(stepInfo.api, npc)) {
                    return;
                }
                await stepInfo.api.SetupLocation(npc.ParentModel, invulnerability, involve, true, rotateToHero);
            }
        }

        void SwitchPresenceTemporarily(PresenceChangeStepInfo stepInfo) {
            var temporaryPresences = StoryBasedNpcPresences.GetOrCreate(stepInfo.api);
            foreach (var presence in stepInfo.presencesToSet) {
                temporaryPresences.AddPresence(presence, TeleportIn, TeleportBack);
            }
        }
        
        void SwitchPresence(PresenceChangeStepInfo stepInfo, bool disablePrevious) {
            var lastPresence = stepInfo.presencesToSet[0].AliveNpc?.NpcPresence;
            
            foreach (var presence in stepInfo.presencesToSet) {
                presence.SetManualAvailability(true, TeleportIn);
            }
            
            bool canDisableLastPresence = lastPresence is { Available: true } && !stepInfo.presencesToSet.Contains(lastPresence);
            if (disablePrevious && canDisableLastPresence) {
                lastPresence.SetManualAvailability(false, TeleportIn);
            }
        }

        async UniTask<bool> WaitForNpcToFullyEnterInteraction(Story api, NpcElement npc) {
            return await AsyncUtil.WaitUntil(api, () =>
                npc.Interactor is {
                    IsFullyInteracting: true,
                    CurrentInteraction: not ITempInteraction
                }
            );
        }

        struct PresenceChangeStepInfo {
            public FrugalList<NpcPresence> presencesToSet;
            public Story api;
        }

        public enum ChangeType : byte {
            [UnityEngine.Scripting.Preserve] SwitchPermanently,
            [UnityEngine.Scripting.Preserve] SwitchTemporarily,
            [UnityEngine.Scripting.Preserve] SetAvailableOnly,
        }
    }
}