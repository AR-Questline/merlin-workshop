using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public static class HeroUnstuck {
        public static async UniTask Unstuck() {
            var popup = PopupUI.SpawnNonInteractablePopup(typeof(VSmallPopupUI),
                LocTerms.PopupUnstuckMessage.Translate(),
                LocTerms.PopupUnstuckTitle.Translate());
            
            var hero = Hero.Current;
            Log.Marking?.Warning($"Unstuck from {hero.Coords} started.");
            bool success = false;
            var fastTravels = World.All<LocationDiscovery>()
                .Where(ld => ld.IsFastTravel)
                .WhereNotNull()
                .OrderBy(ld => Vector3.Distance(ld.FastTravelPoint, hero.Coords));

            foreach (LocationDiscovery t in fastTravels) {
                var path = ABPath.Construct(t.FastTravelPoint, hero.Coords);
                AstarPath.StartPath(path);
                await path.WaitForPath().ToUniTask();
                if (path is { error: false }) {
                    TeleportHero(path, hero);
                    success = true;
                    break;
                }
            }

            if (!success) {
                var portal = Portal.FindClosestExit(hero.Coords);
                if (portal != null) {
                    var path = ABPath.Construct(portal.ParentModel.Coords, hero.Coords);
                    AstarPath.StartPath(path);
                    await path.WaitForPath().ToUniTask();
                    if (path is { error: false }) {
                        TeleportHero(path, hero);
                        success = true;
                    }
                }
            }
            
            if (!success) {
                Log.Marking?.Warning("Unstuck failed. Running fallback");
                Portal.FastTravel.To(hero, Portal.FindDefaultEntry() ?? Portal.FindAnyEntry()).Forget();
            }

            popup.Discard();
        }

        static void TeleportHero(Path path, Hero hero) {
            var targetPosition = path.vectorPath.ElementAt(path.vectorPath.Count - 1);
            Log.Marking?.Warning($"Unstuck to {targetPosition} finished.");
            hero.TeleportTo(targetPosition);
        }
    }
}