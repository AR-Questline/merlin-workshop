using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class CopyInteractabilityFromOtherLocation : Element<Location>, IRefreshedByAttachment<CopyInteractabilityFromOtherLocationAttachment> {
        public override ushort TypeForSerialization => SavedModels.CopyInteractabilityFromOtherLocation;

        LocationSpec _specSource;
        
        public void InitFromAttachment(CopyInteractabilityFromOtherLocationAttachment spec, bool isRestored) {
            _specSource = spec.Location;
        }
        
        protected override void OnInitialize() {
            AsyncInitialize().Forget();
        }
        
        async UniTaskVoid AsyncInitialize() {
            if (await AsyncUtil.DelayFrame(this)) { // give time for source location to initialize
                var source = World.ByID<Location>(_specSource.GetLocationId());
                ParentModel.SetInteractability(source.Interactability);
                source.ListenTo(Location.Events.InteractabilityChanged, ParentModel.SetInteractability, this);
            }
        }
    }
}