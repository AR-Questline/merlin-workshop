using Awaken.Utility;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class CopyInteractabilityToOtherLocations : Element<Location>, IRefreshedByAttachment<CopyInteractabilityToOtherLocationsAttachment> {
        public override ushort TypeForSerialization => SavedModels.CopyInteractabilityToOtherLocations;

        LocationSpec[] _specsCache;
        
        public void InitFromAttachment(CopyInteractabilityToOtherLocationsAttachment spec, bool isRestored) {
            _specsCache = spec.Locations;
        }
        
        protected override void OnInitialize() {
            AsyncInitialize().Forget();
        }

        async UniTaskVoid AsyncInitialize() {
            if (await AsyncUtil.DelayFrame(this)) { // give time for source location to initialize
                UpdateLocations(ParentModel.Interactability);
                ParentModel.ListenTo(Location.Events.InteractabilityChanged, UpdateLocations, this);
            }
        }
        
        void UpdateLocations(LocationInteractability interactability) {
            foreach (var location in _specsCache.Select(spec => World.ByID<Location>(spec.GetLocationId()))) {
                if (location is { HasBeenDiscarded: false }) {
                    location.SetInteractability(interactability);
                }
            }
        }
    }
}