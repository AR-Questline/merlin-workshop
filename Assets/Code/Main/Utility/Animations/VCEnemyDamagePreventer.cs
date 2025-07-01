using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations {
    public class VCEnemyDamagePreventer  : ViewComponent<Location> {
        [SerializeField] new Collider collider;
        
        protected override void OnAttach() {
            Target.TryGetElement<EnemyBaseClass>()?.ListenTo(EnemyBaseClass.Events.TogglePreventDamageState, ToggleDamagePreventer, this);
            collider.enabled = false;
        }

        void ToggleDamagePreventer(bool isActive) {
            collider.enabled = isActive;
        }
    }
}
