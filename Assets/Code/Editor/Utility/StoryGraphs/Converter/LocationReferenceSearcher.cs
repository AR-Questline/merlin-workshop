using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Conditions;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.Objectives.Trackers;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;
using UnityEditor;
using XNode;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Converter {
    public static class LocationReferenceSearcher {
        static void ProcessAll(Process process) {
            foreach (var (graph, reference) in FindLocationReferencesInStory()) {
                bool changed = false;
                process(reference, Context.Story, ref changed);
                if (changed) {
                    EditorUtility.SetDirty(graph);
                }
            }

            foreach (var quest in TemplatesProvider.EditorGetAllOfType<QuestTemplate>()) {
                bool changed = false;
                foreach (var objective in quest.GetComponents<ObjectiveSpec>()) {
                    process(objective.TargetLocationReference, Context.Quest, ref changed);
                }
                foreach (var aliveNPCTracker in quest.GetComponentsInChildren<AliveNPCTrackerAttachment>()) {
                    process(aliveNPCTracker.npcLocation, Context.Quest, ref changed);
                }
                foreach (var interactedWithTracker in quest.GetComponentsInChildren<InteractedWithTrackerAttachment>()) {
                    process(interactedWithTracker.Location, Context.Quest, ref changed);
                }
                foreach (var locationSpawnedTracker in quest.GetComponentsInChildren<LocationSpawnedTrackerAttachment>()) {
                    process(locationSpawnedTracker.Location, Context.Quest, ref changed);
                }
                if (changed) {
                    EditorUtility.SetDirty(quest);
                }
            }
            
            // TODO: There are some Item/Location attachments that serialize LocationReference
            
            AssetDatabase.SaveAssets();
        }
        
        // === Story
        
        static IEnumerable<(NodeGraph graph, LocationReference reference)> FindLocationReferencesInStory() {
            return FromSteps<SEditorActivateNpcPresence>(step => step.presences)
                .Concat(FromSteps<SEditorBountyGoToJail>(step => step.guard))
                .Concat(FromSteps<SEditorBountyPayWealth>(step => step.guard))
                .Concat(FromSteps<SEditorBountyReset>(step => step.guard))
                .Concat(FromSteps<SEditorChangeHeroMountOwner>(step => step.locationRef))
                .Concat(FromSteps<SEditorChangeNpcFaction>(step => step.locations))
                .Concat(FromSteps<SEditorLocationAddToStory>(step => step.locations))
                .Concat(FromSteps<SEditorLocationChangeAttachments>(step => step.locations))
                .Concat(FromSteps<SEditorLocationChangeFocus>(step => step.focusTarget))
                .Concat(FromSteps<SEditorLocationChangeInteractability>(step => step.locationReference))
                .Concat(FromSteps<SEditorLocationClear>(step => step.locations))
                .Concat(FromSteps<SEditorLocationDiscard>(step => step.locationReference))
                .Concat(FromSteps<SEditorLocationInteract>(step => step.locationReference))
                .Concat(FromSteps<SEditorLocationMakeBusy>(step => step.locationReference))
                .Concat(FromSteps<SEditorLocationRemoveBusy>(step => step.locationReference))
                .Concat(FromSteps<SEditorLocationRunUnobserved>(step => step.locationReference))
                .Concat(FromSteps<SEditorLocationStartStory>(step => step.locationReference))
                .Concat(FromSteps<SEditorNpcKill>(step => step.locations))
                .Concat(FromSteps<SEditorNpcLookAt>(step => new[]{ step.source, step.target }))
                .Concat(FromSteps<SEditorNpcMove>(step => new[] { step.targets, step.target }))
                .Concat(FromSteps<SEditorNpcRefreshCurrentBehaviour>(step => step.locationReference))
                .Concat(FromSteps<SEditorNpcChangeCrimeOverride>(step => step.locations))
                .Concat(FromSteps<SEditorNpcTurnFriendly>(step => step.locations))
                .Concat(FromSteps<SEditorNpcTurnHostile>(step => step.locations))
                .Concat(FromSteps<SEditorNpcTurnIntoGhost>(step => step.locations))
                .Concat(FromSteps<SEditorNpcTurnKillPrevention>(step => step.locations))
                .Concat(FromSteps<SEditorOpenShop>(step => step.locationRef))
                .Concat(FromSteps<SEditorPerformInteraction>(step => step.locations))
                .Concat(FromSteps<SEditorPerformInteractionAndWait>(step => step.locations))
                .Concat(FromSteps<SEditorPlaySFX>(step => step.locationRef))
                .Concat(FromSteps<SEditorSetAnimatorParameter>(step => step.location))
                .Concat(FromSteps<SEditorStatChange>(step => step.locationRef))
                .Concat(FromSteps<SEditorStopInteraction>(step => step.locations))
                .Concat(FromSteps<SEditorTeleportHero>(step => step.locRef))
                .Concat(FromSteps<SEditorTeleportNpc>(step => new[] { step.npcToTeleport, step.locRef }))
                .Concat(FromSteps<SEditorTrespassingIgnoreTimeToCrime>(step => step.guard))
                .Concat(FromSteps<SEditorTrespassingResetTimeToCrime>(step => step.guard))
                .Concat(FromSteps<SEditorTriggerLocationSpawners>(step => step.locationReference))
                .Concat(FromSteps<SEditorTriggerLocationVS>(step => step.locationReference))
                .Concat(FromSteps<SEditorTriggerPortal>(step => step.locRef))
                .Concat(FromConditions<CEditorIsLocationBusy>(condition => condition.locationReference))
                .Concat(FromConditions<CEditorCanPayBounty>(condition => condition.guard))
                ;
        }

        static IEnumerable<(NodeGraph graph, LocationReference reference)> FromSteps<TStep>(Func<TStep, LocationReference> getReference) where TStep : EditorStep {
            return AllElements<ChapterEditorNode, TStep>().Select(trio => (trio.graph, getReference(trio.element)));
        }
        static IEnumerable<(NodeGraph graph, LocationReference reference)> FromSteps<TStep>(Func<TStep, LocationReference[]> getReference) where TStep : EditorStep {
            return AllElements<ChapterEditorNode, TStep>().SelectMany(trio => getReference(trio.element).Select(reference => (trio.graph, reference)));
        }
        static IEnumerable<(NodeGraph graph, LocationReference reference)> FromConditions<TCondition>(Func<TCondition, LocationReference> getReference) where TCondition : EditorCondition {
            return AllElements<ConditionsEditorNode, TCondition>().Select(trio => (trio.graph, getReference(trio.element)));
        }
        
        enum Context {
            Story,
            Quest,
        }
        
        delegate void Process(LocationReference reference, Context context, ref bool changed);
    }
}