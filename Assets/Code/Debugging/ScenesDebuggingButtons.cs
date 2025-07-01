using System.Linq;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Compass = Awaken.TG.Main.Maps.Compasses.Compass;

namespace Awaken.TG.Debugging {
    public class ScenesDebuggingButtons {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterButtons() {
            ScenesDebuggingWindow.DefinedButtons.Add(new("Rmv npcs", DiscardNpcs, true, true));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Rmv locations", DiscardLocations, true, true));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Dis npcs anim", DisableNpcsAnimations, true, true));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Dis npcs render", DisableNpcsRenderers, true, true));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Dis npcs colliders", DisableNpcsColliders, true, true));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Tgl npcs AI", ToggleNpcsAI, true, true));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Tgl npcs GO", ToggleNpcsGO, true, true));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Tgl npcs hairs", ToggleNpcsHairs, true, true));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Rmv compass", DiscardCompass, false, true));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Rmv animals", DiscardAnimals, true, true));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Rmv monsters", DiscardMonsters, true, true));
            ScenesDebuggingWindow.DefinedButtons.Add(new("Rmv humans", DiscardHumans, true, true));

            ScenesDebuggingWindow.AdditionalHeaderDrawers.Add(DrawCounts);
        }

        static void DiscardNpcs(Scene scene) {
            World.All<NpcElement>()
                .Where(npc => IsOnScene(npc, scene))
                .ToArray()
                .ForEach(static npc => npc.ParentModel.Discard());
        }

        static void DiscardLocations(Scene scene) {
            World.All<Location>()
                .Where(location => IsOnScene(location, scene))
                .ToArray()
                .ForEach(static location => location.Discard());
        }

        static void DisableNpcsAnimations(Scene scene) {
            World.All<NpcElement>()
                .Where(npc => IsOnScene(npc, scene))
                .Select(static npc => npc.Element<NpcGeneralFSM>())
                .Select(static fsm => fsm.Animator)
                .ForEach(static anim => anim.enabled = false);
        }

        static void DisableNpcsRenderers(Scene scene) {
            World.All<NpcElement>()
                .Where(npc => IsOnScene(npc, scene))
                .SelectMany(static npc => npc.ParentTransform.GetComponentsInChildren<Renderer>())
                .ForEach(static renderer => renderer.enabled = false);
        }

        static void DisableNpcsColliders(Scene scene) {
            World.All<NpcElement>()
                .Where(npc => IsOnScene(npc, scene))
                .SelectMany(static npc => npc.ParentTransform.GetComponentsInChildren<Collider>())
                .ForEach(static collider => collider.enabled = false);
        }

        static OnDemandCache<int, bool> s_npcActiveDebugging = new(static _ => false);
        static void ToggleNpcsAI(Scene scene) {
            var debuggingActive = s_npcActiveDebugging[scene.handle];
            s_npcActiveDebugging[scene.handle] = !debuggingActive;
            World.All<NpcElement>()
                .Where(npc => IsOnScene(npc, scene))
                .ForEach(npc => {
                    var behaviours = npc.NpcAI.Behaviour;
                    var idles = npc.Behaviours;
                    var enemy = npc.ParentModel.TryGetElement<EnemyBaseClass>();
                    if (debuggingActive) {
                        idles.DEBUG_Disable = false;
                        if (enemy != null) {
                            enemy.DEBUG_Disable = false;
                        }
                        behaviours.DEBUG_OverrideShouldWorking = null;
                    } else {
                        idles.DEBUG_Disable = true;
                        if (enemy != null) {
                            enemy.DEBUG_Disable = true;
                        }
                        behaviours.DEBUG_OverrideShouldWorking = false;
                    }
                });
        }

        static OnDemandCache<int, bool> s_npcActiveGo = new(static _ => true);
        static void ToggleNpcsGO(Scene scene) {
            var activeGo = s_npcActiveGo[scene.handle];
            s_npcActiveGo[scene.handle] = !activeGo;
            var nextGoState = !activeGo;
            World.All<NpcElement>()
                .Where(npc => IsOnScene(npc, scene))
                .ForEach(npc => npc.ParentModel.ViewParent.gameObject.SetActive(nextGoState));
        }

        static OnDemandCache<int, bool> s_npcActiveHairs = new(static _ => true);
        static void ToggleNpcsHairs(Scene scene) {
            var activeHairs = s_npcActiveHairs[scene.handle];
            s_npcActiveHairs[scene.handle] = !activeHairs;
            var nextHairsState = !activeHairs;
            World.All<NpcElement>()
                .Where(npc => IsOnScene(npc, scene))
                .SelectMany(static npc => npc.ParentTransform.GetComponentsInChildren<MeshCoverSettings>())
                .Where(static mc => !mc.IsCover && CoverType.Head.HasFlagFast(mc.Type))
                .SelectMany(static mc => mc.GetComponentsInChildren<Renderer>(true))
                .ForEach(r => r.enabled = nextHairsState);
        }

        static void DiscardCompass(Scene scene) {
            World.Any<Compass>()?.Discard();
            World.All<ICompassMarker>().ToArraySlow().ForEach(static cm => cm.Discard());
        }

        static void DiscardAnimals(Scene scene) {
            World.All<NpcElement>()
                .Where(npc => IsOnScene(npc, scene) && IsAnimal(npc))
                .ToArray()
                .ForEach(static npc => npc.ParentModel.Discard());
        }

        static void DiscardMonsters(Scene scene) {
            World.All<NpcElement>()
                .Where(npc => IsOnScene(npc, scene) && IsMonster(npc))
                .ToArray()
                .ForEach(static npc => npc.ParentModel.Discard());
        }

        static void DiscardHumans(Scene scene) {
            World.All<NpcElement>()
                .Where(npc => IsOnScene(npc, scene) && IsHuman(npc))
                .ToArray()
                .ForEach(static npc => npc.ParentModel.Discard());
        }

        static void DrawCounts(Scene? scene) {
            var npcCount = World.All<NpcElement>().Count(npc => scene == null || IsOnScene(npc, scene.Value));
            var locationCount = World.All<Location>().Count(location => scene == null || IsOnScene(location, scene.Value));
            GUILayout.Label($"Location: {locationCount}; NPCs: {npcCount}");
        }

        // -- Helpers
        static bool IsOnScene(Location location, Scene scene) {
            var npc = location.TryGetElement<NpcElement>();
            if (npc == null) {
                return location.ViewParent.gameObject.scene == scene;
            } else {
                return IsOnScene(npc, scene);
            }
        }

        static bool IsOnScene(NpcElement npc, Scene scene) {
            var presence = npc.NpcPresence;
            if (presence == null) {
                return npc.ParentModel.ViewParent.gameObject.scene == scene;
            } else {
                return presence.ParentModel.ViewParent.gameObject.scene == scene;
            }
        }

        static bool IsAnimal(NpcElement npcElement) {
            var service = World.Services.Get<FactionService>();
            var provider = World.Services.Get<FactionProvider>();
            var root = service.FactionByTemplate(provider.Root);
            var animalFaction = root.SubFactions.FirstOrDefault(f => f.Template.name.Contains("Animal"));
            return npcElement.Faction.Is(animalFaction);
        }

        static bool IsMonster(NpcElement npcElement) {
            var service = World.Services.Get<FactionService>();
            var provider = World.Services.Get<FactionProvider>();
            var root = service.FactionByTemplate(provider.Root);
            var animalFaction = root.SubFactions.FirstOrDefault(f => f.Template.name.Contains("Monster"));
            return npcElement.Faction.Is(animalFaction);
        }

        static bool IsHuman(NpcElement npcElement) {
            var service = World.Services.Get<FactionService>();
            var provider = World.Services.Get<FactionProvider>();
            var root = service.FactionByTemplate(provider.Root);
            var animalFaction = root.SubFactions.FirstOrDefault(f => f.Template.name.Contains("Human"));
            return npcElement.Faction.Is(animalFaction);
        }
    }
}
