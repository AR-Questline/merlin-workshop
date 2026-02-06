using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Trigger Location Spawners"), NodeSupportsOdin]
    public class SEditorTriggerLocationSpawners : EditorStep {
        public LocationReference locationReference;
        public bool waitForSpawn = false;
        public bool forceCombatWithHero = false;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STriggerLocationSpawners {
                locationReference = locationReference,
                waitForSpawn = waitForSpawn,
                forceCombatWithHero = forceCombatWithHero,
            };
        }
    }

    public partial class STriggerLocationSpawners : StoryStepWithLocationRequirementAllowingWait {
        public LocationReference locationReference;
        public bool waitForSpawn;
        public bool forceCombatWithHero;
        
        protected override LocationReference RequiredLocations => locationReference;
        
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(waitForSpawn, forceCombatWithHero);
        }

        public partial class StepExecution : DeferredLocationExecutionAllowingWait {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_TriggerLocationSpawners;
            
            bool _forceCombatWithHero;
            
            public override bool ShouldPerformAndWait { get; }

            public StepExecution(bool shouldWait, bool forceCombatWithHero) {
                ShouldPerformAndWait = shouldWait;
                _forceCombatWithHero = forceCombatWithHero;
            }
            
            public override void Execute(Location location) {
                TriggerSpawner(location, _forceCombatWithHero).Forget();
            }

            public override async UniTask ExecuteAndWait(Location location, Story api) {
                await TriggerSpawner(location, _forceCombatWithHero);
            }

            static UniTask TriggerSpawner(Location location, bool forceCombatWithHero) {
                var spawner = location.TryGetElement<BaseLocationSpawner>();
                if (spawner == null) {
                    Log.Minor?.Error($"No spawner on location: {LogUtils.GetDebugName(location)}");
                    return UniTask.CompletedTask;
                }
                var manualSpawner = spawner.TryGetElement<ManualSpawner>();
                if (manualSpawner == null) {
                    Log.Minor?.Error($"Spawner on location is not a manual spawner: {LogUtils.GetDebugName(location)}");
                    return UniTask.CompletedTask;
                }
                
                if (forceCombatWithHero) {
                    return TriggerSpawnerAndStartCombat(spawner, manualSpawner);
                }

                return manualSpawner.TriggerSpawner();
            }

            static async UniTask TriggerSpawnerAndStartCombat(BaseLocationSpawner spawner, ManualSpawner manualSpawner) {
                var listener = spawner.ListenTo(BaseLocationSpawner.Events.LocationSpawned, TryStartCombatWithHero);
                await manualSpawner.TriggerSpawner();
                if (spawner.HasBeenDiscarded) {
                    return;
                }
                World.EventSystem.TryDisposeListener(ref listener);
                
                static void TryStartCombatWithHero(Location l) {
                    if (l.TryGetElement<NpcElement>(out var npc)) {
                        npc.OnCompletelyInitialized(StartCombatWithHero);
                    }
                }

                static void StartCombatWithHero(NpcElement npc) {
                    npc.TurnHostileTo(AntagonismLayer.Story, Hero.Current);
                    npc.NpcAI.EnterCombatWith(Hero.Current);
                }
            }
        }
    }
}