using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Locations {
    public abstract partial class LocationInitializer {
        public abstract ushort TypeForSerialization { get; }
        
        public LocationSpec Spec { get; protected set; }
        public LocationTemplate Template { get; protected set; }
        public Vector3 SpecInitialPosition { get; protected set; }
        public Quaternion SpecInitialRotation { get; protected set; } = Quaternion.identity;
        public Vector3 SpecInitialScale { get; protected set; }
        
        [Saved] public ARAssetReference OverridenLocationPrefab { get; set; }
        
        public virtual bool ShouldBeSaved => OverridenLocationPrefab?.IsSet == true;

        public abstract void PrepareSpec(Location location);
        public abstract Transform PrepareViewParent(Location location);
        
        public void Init(Location location) {
            SpawnView(location);
        }
        
        void SpawnView(Location location) {
            if (Spec.IsHidableStatic) {
                var vStatic = location.ViewParent.gameObject.AddComponent<VHidableStaticLocation>();
                World.BindView(location, vStatic, true, true);
            } else if (location.IsStatic) {
                var vStatic = location.ViewParent.gameObject.AddComponent<VStaticLocation>();
                World.BindView(location, vStatic, true, true);
            } else if (location.IsNonMovable) {
                World.SpawnView<VSpawnedLocation>(location, true, forcedParent: location.ViewParent, forcedToBeFirstChild: true);
            } else {
                World.SpawnView<VDynamicLocation>(location, true, forcedParent: location.ViewParent, forcedToBeFirstChild: true);
            }
        }
        
        public virtual void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            if (OverridenLocationPrefab?.IsSet == true) {
                JsonUtils.JsonWrite(jsonWriter, serializer, nameof(OverridenLocationPrefab), OverridenLocationPrefab);
            }
        }
    }
}
