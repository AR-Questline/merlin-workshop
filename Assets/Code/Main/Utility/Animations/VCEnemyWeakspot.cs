using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations {
    public class VCEnemyWeakspot : ViewComponent<Location> {
        [SerializeField] Collider weakspot;
        
        protected override void OnAttach() {
            Target.TryGetElement<EnemyBaseClass>()?.ListenTo(EnemyBaseClass.Events.ToggleWeakSpot, ToggleWeakSpot, this);
        }

        void ToggleWeakSpot(bool isActive) {
            weakspot.enabled = isActive;
        }
    }
}
