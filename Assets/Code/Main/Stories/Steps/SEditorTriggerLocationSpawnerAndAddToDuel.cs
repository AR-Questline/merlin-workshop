using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Duels;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Trigger Location Spawners and adds npcs to duel"), NodeSupportsOdin]
    public class SEditorTriggerLocationSpawnerAndAddToDuel : EditorStep {
        public LocationReference locationReference;
        public int groupId = 1;
        [LabelWidth(130)]
        public bool overrideDuelistSettings;
        [LabelWidth(140)]
        [ShowIf(nameof(overrideDuelistSettings))] public DuelistSettings settings = DuelistSettings.Default;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STriggerLocationSpawnerAndAddToDuel {
                locationReference = locationReference,
                groupId = groupId,
                overrideDuelistSettings = overrideDuelistSettings,
                settings = settings,
            };
        }
    }

    public partial class STriggerLocationSpawnerAndAddToDuel : StoryStep {
        public LocationReference locationReference;
        public int groupId;
        public bool overrideDuelistSettings;
        public DuelistSettings settings;
        
        public override StepResult Execute(Story story) {
            var result = new StepResult();
            ExecuteAsync(story, result).Forget();
            return result;
        }

        async UniTask ExecuteAsync(Story story, StepResult result) {
            var duelController = World.Any<DuelController>();
            if (duelController == null) {
                Log.Minor?.Error("No duel in progress, so can't add new participants to it");
                result.Complete();
                return;
            }
            await TriggerSpawner(locationReference.FirstOrDefault(story), duelController, groupId, overrideDuelistSettings ? settings : null);
            result.Complete();
        }

        static async UniTask TriggerSpawner(Location location, DuelController duelController, int groupId, DuelistSettings? settings) {
            var spawner = location?.TryGetElement<BaseLocationSpawner>();
            if (spawner == null) {
                Log.Minor?.Error($"No spawner on location: {LogUtils.GetDebugName(location)}");
                return;
            }
            var manualSpawner = spawner.TryGetElement<ManualSpawner>();
            if (manualSpawner == null) {
                Log.Minor?.Error($"Spawner on location is not a manual spawner: {LogUtils.GetDebugName(location)}");
                return;
            }

            List<NpcElement> spawnedNpcs = new();
            
            var listener = spawner.ListenTo(BaseLocationSpawner.Events.LocationSpawned, l => AddToDuel(l, duelController, groupId, settings, ref spawnedNpcs));
            await manualSpawner.TriggerSpawner();
            if (!spawner.HasBeenDiscarded) {
                World.EventSystem.TryDisposeListener(ref listener);
            }

            float abortTime = Time.time + 5f;
            await AsyncUtil.WaitUntil(duelController, () => AllNpcsInitialized(spawnedNpcs, abortTime));
            
            static bool AllNpcsInitialized(List<NpcElement> npcs, float abortTime) {
                if (Time.time > abortTime) {
                    return true;
                }
                foreach (var npc in npcs) {
                    if (!npc.HasCompletelyInitialized) {
                        return false;
                    }
                }
                return true;
            }
        }

        static void AddToDuel(Location l, DuelController duelController, int groupId, DuelistSettings? settings, ref List<NpcElement> spawnedNpcs) {
            if (!l.TryGetElement<NpcElement>(out var npc)) {
                return;
            }

            spawnedNpcs.Add(npc);
            duelController.AddDuelist(npc, groupId, settings);
        }
    }
}