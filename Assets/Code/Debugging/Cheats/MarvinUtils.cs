using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Debugging.Cheats {
    public static class MarvinUtils {
        public static string ThrowRaycastToFindModelId() {
            var heroRaycasterTransform = Hero.Current?.VHeroController?.Raycaster?.transform;
            if (!heroRaycasterTransform) {
                return string.Empty;
            }
            
            RaycastHit[] raycastResults = new RaycastHit[5];
            var size = Physics.RaycastNonAlloc(heroRaycasterTransform.position, heroRaycasterTransform.forward, raycastResults, Mathf.Infinity);
            for (int i = 0; i < size; i++) {
                var hitInfo = raycastResults[i];
                var hitView = hitInfo.collider.GetComponentInParent<IView>();
                if (hitView != null) {
                    return hitView.GenericTarget.ID;
                }
            }

            return string.Empty;
        }
    }
}