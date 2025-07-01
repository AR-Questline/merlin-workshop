using Awaken.TG.Main.Locations;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Interactions {
    [RequireComponent(typeof(Collider))]
    public class TriggerVolume : MonoBehaviour {
        [SerializeField] Target target;
        
        // === Events
        public static class Events {
            public static readonly Event<Location, Collider> TriggerVolumeEntered = new(nameof(TriggerVolumeEntered));
            public static readonly Event<Location, Collider> TriggerVolumeExited = new(nameof(TriggerVolumeExited));
        }

        void Start() {
            if (target != Target.Hero && !TryGetComponent<Rigidbody>(out var rb)) {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
        
        void OnTriggerEnter(Collider other) {
            Trigger(other, Enter.Name);
        }

        void OnTriggerExit(Collider other) {
            Trigger(other, Exit.Name);
        }

        void OnTriggerStay(Collider other) {
            Trigger(other, Stay.Name);
        }

        void Trigger(Collider collider, string evt) {
            int layer = collider.gameObject.layer;
            if (layer == RenderLayers.Player && target is Target.Hero or Target.All) {
                TriggerFor(Target.Hero);
                TriggerFor(Target.All);
            } else if (layer == RenderLayers.AIs && target is Target.AI or Target.All) {
                TriggerFor(Target.AI);
                TriggerFor(Target.All);
            }

            void TriggerFor(Target target) {
                TriggerEvent.Trigger(gameObject, evt, target, collider);
                
                if (evt is Enter.Name or Exit.Name) {
                    Location location = VGUtils.TryGetModel<Location>(gameObject);
                    location?.Trigger(evt == Enter.Name ? Events.TriggerVolumeEntered : Events.TriggerVolumeExited, collider);
                }
            }
        }

        void OnValidate() {
            GetComponent<Collider>().isTrigger = true;
            if (gameObject.layer != RenderLayers.TriggerVolumes) {
                gameObject.layer = RenderLayers.TriggerVolumes;
                Log.Important?.Error("Invalid layer on TriggerVolume. Changing to TriggerVolumes.", gameObject);
            }
        }

        public enum Target {
            All,
            Hero,
            AI,
        }
    }
}