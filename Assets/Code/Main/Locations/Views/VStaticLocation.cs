using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Locations.Views {
    [UsesPrefab("Locations/VLocationStatic")]
    public class VStaticLocation : VLocation {
        GameObject _staticReference;
        
        // === Initialization
        public override Transform DetermineHost() => Target.ViewParent;

        protected override void OnInitialize() {
            base.OnInitialize();
            Target.ListenTo(Model.Events.AfterFullyInitialized, AssignStaticPrefab, this);
        }

        // === Operations
        void AssignStaticPrefab() {
            if (Target.Spec == null) {
                Log.Important?.Warning($"Null location spec for {Target.DisplayName} ({name}), ID: {Target.ID}", gameObject);
                Target.VisualLoadingFailed();
                return;
            }

            _staticReference = Target.Spec.gameObject;
            if (_staticReference == null) {
                Log.Important?.Error($"Null location prefab for {Target.DisplayName} ({name}), ID: {Target.ID}", gameObject);
                Target.VisualLoadingFailed();
                return;
            }

            SetupPrefab();
        }
        
        void SetupPrefab() {
            InitializeViewComponents(_staticReference.transform);
            VSTriggerOnVisualLoaded();
            ChangeRenderLayer(_staticReference);

            Target.Trigger(Location.Events.BeforeVisualLoaded, _staticReference);
            Target.VisualLoaded(_staticReference.transform, LocationVisualSource.FromScene);
        }
    }
}