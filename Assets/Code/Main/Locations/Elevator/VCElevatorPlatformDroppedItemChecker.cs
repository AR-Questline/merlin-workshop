using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Elevator {
    public class VCElevatorPlatformDroppedItemChecker : ViewComponent<Location> {
        [SerializeField] Transform platformItemsParent;
        static Transform DroppedItemsParent => World.Services.Get<DroppedItemSpawner>().DroppedItemsParent;
        
        void OnTriggerEnter(Collider other) {
            SetupItem(other.gameObject, platformItemsParent);
        }

        void OnTriggerExit(Collider other) {
            SetupItem(other.gameObject, DroppedItemsParent);
        }

        static void SetupItem(GameObject gameObj, Transform newParent) {
            IModel model = VGUtils.GetModel(gameObj);
            if (model?.HasElement<PickItemAction>() ?? false) {
                LocationParent locationParent = gameObj.GetComponentInParent<LocationParent>();
                if (locationParent != null) {
                    locationParent.transform.SetParent(newParent);
                }
            }
        }
    }
}