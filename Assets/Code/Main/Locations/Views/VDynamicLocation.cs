using System.Runtime.CompilerServices;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Locations.Views {
    [UsesPrefab("Locations/VLocationDynamic")]
    public class VDynamicLocation : VSpawnedLocation {
        public void UnityUpdate() {
            if (!CheckModelInstance() || !_modelInstanceTransform.hasChanged) {
                return;
            }
            SyncPositionAndRotation();
        }

        public void SyncPositionAndRotation() {
            if (_modelInstanceTransform == null) {
                Log.Important?.Error($"{nameof(_modelInstanceTransform)} is null in {nameof(VDynamicLocation)}");
                return;
            }
            _modelInstanceTransform.GetLocalPositionAndRotation(out var position, out var quaternion);
            var hasChange = position != Location.VectorZero ||
                            quaternion != Location.QuaternionIdentity;

            if (hasChange) {
                _modelInstanceTransform.GetPositionAndRotation(out position, out quaternion);
                Target.MoveAndRotateTo(position, quaternion);
                _modelInstanceTransform.SetLocalPositionAndRotation(Location.VectorZero, Location.QuaternionIdentity);
            }
            _modelInstanceTransform.hasChanged = false;
        }
        
        protected override void OnLocationReady() {
            if (this && _isVisible == true) {
                World.Services.Get<UnityUpdateProvider>().RegisterDynamicLocation(this);
            }
        }

        protected override void OnVisibilityChanged() {
            if (this && _modelInstanceTransform) {
                if (_isVisible == true) {
                    World.Services.Get<UnityUpdateProvider>().RegisterDynamicLocation(this);
                } else {
                    World.Services.Get<UnityUpdateProvider>().UnregisterDynamicLocation(this);
                }
            }
        }

        protected override void OnClearReferences() {
            World.Services.Get<UnityUpdateProvider>().UnregisterDynamicLocation(this);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool CheckModelInstance() {
#if UNITY_EDITOR
            if (ModelInstance == null) {
                if (this == null) {
                    Log.Important?.Error("VDynamicLocation not detached correctly: " + ID);
                } else {
                    Log.Important?.Error($"MEMORY LEAK For {nameof(VDynamicLocation)} {name} ({ID})." +
                                   $" ModelInstance is null but wasn't released from addressables", this);
                }
                ClearReferences();
                return false;
            }
#endif
            return true;
        }
    }
}