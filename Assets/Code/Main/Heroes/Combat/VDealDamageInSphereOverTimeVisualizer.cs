using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    [NoPrefab]
    public class VDealDamageInSphereOverTimeVisualizer : View<DealDamageInSphereOverTime> {
#if UNITY_EDITOR
        void OnDrawGizmos() {
            var originalColor = Gizmos.color;
            Color red = new Color(1, 0, 0, 0.3f);
            Color blue = new Color(0, 1, 1, 0.3f);
            foreach (var sphereToDraw in Target.spheresToDraw) {
                Gizmos.color = Gizmos.color.r >= 1 ? blue : red;
                Gizmos.DrawSphere(sphereToDraw.Origin, sphereToDraw.Radius);
            }
            Gizmos.color = originalColor;
        }
#endif
    }
}