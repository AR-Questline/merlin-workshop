using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Awaken.TG.VisualScripts.Units.Listeners.Events;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Heroes.Items.Weapons {
    public partial class PlacedMine : ItemBasedLocationMarker, ICharacterLimitedLocation {
        public override ushort TypeForSerialization => SavedModels.PlacedMine;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public PlacedMine() { }

        public PlacedMine(ICharacter owner, Item sourceItem) : base(owner, sourceItem) { }

        protected override void OnFullyInitialized() {
            AsyncOnFullyInitialized().Forget();
        }

        protected async UniTaskVoid AsyncOnFullyInitialized() {
            base.OnFullyInitialized();
            if (Owner == null) {
                await AsyncUtil.DelayFrame(this);
                if (!HasBeenDiscarded) {
                    Destroy();
                }
            }
        }

        // === ICharacterLimitedLocation
        public CharacterLimitedLocationType Type => CharacterLimitedLocationType.PlacedMine;

        public int LimitForCharacter(ICharacter character) {
            return character is Hero ? GameConstants.Get.heroMineLimit : GameConstants.Get.npcMineLimit;
        }

        public void OwnerDiscarded() { }

        public void Destroy() {
            ParentModel.Trigger(Events.BeforeExploded, this);
            Discard();
        }

        public new static class Events {
            public static readonly Event<Location, PlacedMine> BeforeExploded = new(nameof(BeforeExploded));
        }
    }
    
    [UnitCategory("AR/General/Events/Character Limited Locations")]
    [UnityEngine.Scripting.Preserve]
    public class EvtPlacedMineTriggered : GraphEvent<Location, PlacedMine> {
        protected override Event<Location, PlacedMine> Event => PlacedMine.Events.BeforeExploded;
        protected override Location Source(IListenerContext context) => context.Location;
    }
}