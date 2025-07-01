using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.AutoGuards;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Scenes.SceneConstructors.AdditiveScenes;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Combat.Utils {
    public static class CrimeReactionUtils {
        const float DistanceToCallGuard = NpcAIDistancesUtils.ReturnToSpawnBand2;
        const float CooldownBetweenCalls = 1.9f;

        static double s_lastCall;

        public static void EDITOR_RuntimeReset() {
            s_lastCall = -CooldownBetweenCalls;
        }
        
        static bool HasEnemyBaseClass(NpcElement npc, out EnemyBaseClass enemy) => npc.ParentModel.TryGetElement(out enemy);

        public static bool IsGuard(NpcElement npc) => HasEnemyBaseClass(npc, out var enemy) && enemy.IsGuard;
        public static bool IsDefender(NpcElement npc) => HasEnemyBaseClass(npc, out var enemy) && enemy.IsDefender;
        public static bool IsVigilante(NpcElement npc) => HasEnemyBaseClass(npc, out var enemy) && enemy.IsVigilante;
        public static bool IsFleeing(NpcElement npc) => HasEnemyBaseClass(npc, out var enemy) && (enemy.IsFleeingPeasant || enemy.IsAlwaysFleeing);
        public static bool IsFleeingPeasant(NpcElement npc) => HasEnemyBaseClass(npc, out var enemy) && enemy.IsFleeingPeasant;
        public static bool IsAlwaysFleeing(NpcElement npc) => HasEnemyBaseClass(npc, out var enemy) && enemy.IsAlwaysFleeing;

        public static bool ReactsToBounty(NpcElement npc) {
            return HasEnemyBaseClass(npc, out var enemy) && (enemy.IsGuard || enemy.IsVigilante);
        }

        public static bool ShouldReact(NpcElement npc) {
            CrimeOwnerTemplate crimeOwner = npc.GetCurrentCrimeOwnersFor(CrimeArchetype.None).PrimaryOwner;
            return CrimeUtils.HasCommittedUnforgivableCrime(crimeOwner) || (ReactsToBounty(npc) && CrimeUtils.HasBounty(crimeOwner));
        }

        public static void CallGuardsToHero(CrimeOwnerTemplate crimeOwner) {
            if (crimeOwner == null) {
                Log.Important?.Error("Guard was called but no crime owner was provided");
                return;
            }
            Vector3 position = Hero.Current.Coords;
            var time = World.Only<GameRealTime>().PlayRealTime.TotalSeconds;
            if (time - s_lastCall > CooldownBetweenCalls) {
                bool guardCalled;
                var sceneService = World.Services.Get<SceneService>();
                
                if (sceneService.IsAdditiveScene) {
                    guardCalled = CallGuardInAdditiveScene(position, sceneService);
                } else {
                    guardCalled = CallGuardInMainScene(position, crimeOwner);
                }

                crimeOwner.CrimeSavedData.CallForHelp(position);
                
                if (guardCalled) {
                    s_lastCall = time;
                } else {
                    Log.Debug?.Info("[Thievery] Failed to call guard");
                }
            }
        }

        static bool CallGuardInAdditiveScene(Vector3 position, SceneService sceneService) {
            bool guardCalled = false;
            var additiveDomain = sceneService.ActiveDomain;
            foreach (var npc in World.All<NpcElement>()) {
                if (IsGuard(npc) && npc.IsOnSceneWithDomain(additiveDomain)) {
                    guardCalled = true;
                    CallGuard(npc);
                }
            }
            if (!guardCalled) {
                return SpawnGuardInPortal(position, sceneService);
            } else {
                return true;
            }
        }

        static bool SpawnGuardInPortal(Vector3 position, SceneService sceneService) {
            var additiveScene = sceneService.AdditiveSceneRef.RetrieveMapScene<AdditiveScene>();
            if (!additiveScene.TryGetDefaultGuard(out var guardTemplate)) {
                return false;
            }
            var portal = Portal.FindClosestExit(position);
            if (portal == null) {
                return false;
            }
            var guardRotation = Quaternion.LookRotation(portal.ParentModel.Forward(), Vector3.up);
            SpawnGuard(portal.ParentModel.Coords, guardRotation, guardTemplate);
            return true;
        }

        static bool CallGuardInMainScene(Vector3 position, CrimeOwnerTemplate crimeOwner) {
            bool guardCalled = false;
            foreach (var npc in World.Services.Get<NpcGrid>().GetNpcsInSphere(position, DistanceToCallGuard)) {
                if (IsGuard(npc)) {
                    CallGuard(npc);
                    guardCalled = true;
                }
            }

            if (!guardCalled) {
                return SpawnGuardInAutoGuardSpawnPoint(crimeOwner);
            } else {
                return true;
            }
        }
        
        static bool SpawnGuardInAutoGuardSpawnPoint(CrimeOwnerTemplate crimeOwner) {
            Hero hero = Hero.Current;
            Vector3 heroPos = hero.Coords;
            AutoGuardSpawning autoGuardSpawning = World.All<AutoGuardSpawning>().FirstOrDefault(autoGuard => autoGuard.Faction.Template == crimeOwner);

            if (autoGuardSpawning == null) {
                // No auto guard spawning configured for this faction
                return false;
            }

            Vector3? guardPosition = autoGuardSpawning.GetSpawnPoint();
            if (guardPosition == null) {
                // Can't spawn guard anywhere
                return false;
            }

            Quaternion rotation = Quaternion.LookRotation(heroPos - guardPosition.Value, Vector3.up);
            SpawnGuard(guardPosition.Value, rotation, autoGuardSpawning.RandomGuardTemplate);
            return true;
        }

        static void SpawnGuard(Vector3 position, Quaternion rotation, LocationTemplate guardTemplate) {
            Log.Debug?.Info("[Thievery] Guard spawned");
            var guardLocation = guardTemplate.SpawnLocation(position, rotation);
            guardLocation.AddElement(new DiscardLocationWhenNotInVisualBand());
            guardLocation.OnVisualLoaded(_ => {
                if (guardLocation.TryGetElement(out NpcElement npc)) {
                    CallGuardAfterOneFrame(npc).Forget();
                } else {
                    Log.Important?.Error("Guard location has no NPC element");
                }
            });
        }

        static void CallGuard(NpcElement guard) {
            if (guard.NpcAI != null) {
                guard.NpcAI.AlertStack.NewPoi((int) AlertStack.AlertStrength.Max * 2, Hero.Current);
            } else {
                guard.ParentModel.OnVisualLoaded(_ => {
                    CallGuardAfterOneFrame(guard).Forget();
                });
            }
        }

        static async UniTaskVoid CallGuardAfterOneFrame(NpcElement npc) {
            if (await AsyncUtil.DelayFrame(npc)) {
                CallGuard(npc);
            }
        }

        public static bool NPCContainsOwnerFaction(CrimeOwnerTemplate crimeOwner, NpcCrimeReactions reactions, CrimeArchetype archetype) {
            return reactions.ParentModel.GetCurrentCrimeOwnersFor(archetype).Contains(crimeOwner);
        }
    }
}