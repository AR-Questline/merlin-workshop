using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.WyrdStalker {
    internal sealed class ActiveWyrdStalkerControllerBase : WyrdStalkerControllerBase {
        const float WalkToRadius = 2f;
        const float ExitRadiusSq = 2f * 2f;
        
        protected override LocationTemplate Template {
            get {
                var templates = CommonReferences.Get.WyrdStalkerActiveTemplates;
                int index = SoulsPickedUpCount > templates.Length ? templates.Length - 1 : SoulsPickedUpCount - 1;
                return templates[index].Get<LocationTemplate>();
            }
        }
        protected override float SpawnChance => GameConstants.Get.wyrdStalkerActiveSpawnChance;

        public ActiveWyrdStalkerControllerBase(HeroWyrdStalker controller) : base(controller) {
            UpdateFoV(HeroCamera.fieldOfView);
        }
        
        protected override void OnScreenUpdate(float deltaTime, Location wyrdStalker, float dotToCamera) { }
        protected override void OffScreenUpdate(float deltaTime, Location wyrdStalker) { }
        
        protected override bool ShouldSpawn() {
            if (!Element.IsHeroInWyrdness) return false;
            if (SoulsPickedUpCount < HeroWyrdStalker.ActiveWyrdStalkerThreshold) {
                Element.RefreshController();
                return false;
            }
            if (Element.WyrdStalkerDead) return false;
            return true;
        }
        
        protected override void OnSpawned(Location wyrdStalker) {
            if (wyrdStalker.TryGetElement<NpcElement>() is { } npcElement) {
                npcElement.ListenTo(IAlive.Events.AfterDeath, Element.OnWyrdStalkerDeath, wyrdStalker);
                var finder = new InteractionFollowLocationFinder(Hero, false, WalkToRadius, ExitRadiusSq);
                var interactionOverride = new InteractionOverride(finder, null);
                npcElement.Behaviours.AddOverride(interactionOverride);
                int activeWyrdStalkerLevel = SoulsPickedUpCount - HeroWyrdStalker.ActiveWyrdStalkerThreshold;
                if (activeWyrdStalkerLevel > 0) {
                    foreach (var modifier in GameConstants.Get.wyrdStalkerStatsModifierPerSoulCount) {
                        modifier.RunEffectAtLevel(activeWyrdStalkerLevel, npcElement, npcElement);
                    }
                    npcElement.Health.SetToFull(); // If MaxHealth has been modified;
                }
            }
        }

        protected override bool ShouldHide() => false;
        protected override void OnHidden() { }
    }
}