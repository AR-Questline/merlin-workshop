using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items {
    public partial class ItemRigidbody : Element<Location> {
        public override ushort TypeForSerialization => SavedModels.ItemRigidbody;

        const string RecurringId = "ItemRigidbody";
        const float RecurringInterval = 5f;
        const float DroppedItemTorque = 50f;
        const float SleepVelocityThreshold = 0.001f;
        
        Vector3? _initialForce;
        Rigidbody _rigidbody;
        float _timeSinceLastCheck;
        
        static Transform DroppedItemsParent => World.Services.Get<DroppedItemSpawner>().DroppedItemsParent;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public ItemRigidbody() { }

        public ItemRigidbody(Vector3? initialForce = null) {
            _initialForce = initialForce;
        }
        
        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(t => {
                AddRigidbody(t);
                AddInitialForce();
            });
        }

        protected override void OnRestore() {
            ParentModel.OnVisualLoaded(AddRigidbody);
        }

        protected override bool OnSave() {
            Location location = ParentModel;
            location.SetCoordsBeforeSave(location.ViewParent.position);
            return true;
        }

        void AddRigidbody(Transform t) {
            _rigidbody = t.AddComponent<Rigidbody>();
            _rigidbody.mass = 5;
            _rigidbody.linearDamping = 0.4f;
            _rigidbody.angularDamping = 1f;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _rigidbody.AddTorque(Vector3.one * DroppedItemTorque, ForceMode.VelocityChange);
            LocationParent locationParent = t.GetComponentInParent<LocationParent>();
            locationParent.transform.SetParent(DroppedItemsParent);
            Services.Get<RecurringActions>().RegisterAction(RefreshRigidbody, this, RecurringId, RecurringInterval, false);
        }

        void RefreshRigidbody() {
            if (_rigidbody.linearVelocity.sqrMagnitude < SleepVelocityThreshold) {
                _rigidbody.linearVelocity = Vector3.zero; //prevent sliding items on the ground
                _rigidbody.angularVelocity = Vector3.zero;
                ClearRigidbody();
            }
        }
        
        void ClearRigidbody() {
            if (_rigidbody != null) {
                Object.Destroy(_rigidbody);
                _rigidbody = null;
            }
            
            Services.Get<RecurringActions>().UnregisterAction(this, RecurringId);
        }

        void AddInitialForce() {
            if (_initialForce.HasValue) {
                _rigidbody.AddForce(_initialForce.Value * _rigidbody.mass, ForceMode.Impulse);
                _initialForce = null;
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ClearRigidbody();
        }
    }
}