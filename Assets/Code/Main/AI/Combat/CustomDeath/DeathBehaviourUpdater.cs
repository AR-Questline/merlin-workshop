using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.CustomDeath {
    public abstract partial class DeathBehaviourUpdater : Element<Location> {
        protected virtual bool RequireCustomDeathController => true;
        protected bool _updated;
        
        protected override void OnInitialize() {
            if (ParentModel.IsVisualLoaded) {
                ParentModel.Trigger(DeathElement.Events.RefreshDeathBehaviours, true);
            }
        }
        
        protected override void OnRestore() { }

        public void UpdateDeathBehaviours(GameObject visualGO) {
            if (_updated) {
                return;
            }

            var customDeathController = GetOrCreateDeathController(visualGO);
            UpdateDeathBehaviours(customDeathController.gameObject, customDeathController);
            _updated = true;
        }

        protected abstract void UpdateDeathBehaviours(GameObject visualGO, CustomDeathController customDeathController);
        
        protected CustomDeathController GetOrCreateDeathController(GameObject visualGO) {
            if (!visualGO.TryGetComponentInChildren(true, out CustomDeathController customDeathController) && RequireCustomDeathController) {
                customDeathController = visualGO.AddComponent<CustomDeathController>();
            }
            return customDeathController;
        }
    }
}
