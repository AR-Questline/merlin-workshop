using Awaken.TG.Main.Character;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Statuses {
    /// <summary>
    /// Marks a character as muted (silenced). Muted character cannot cast spells.
    /// </summary>
    public partial class MutedMarker : Element<ICharacter> {
        public override ushort TypeForSerialization => SavedModels.MutedMarker;

        [JsonConstructor, UnityEngine.Scripting.Preserve] MutedMarker() { }
        
        public MutedMarker(Status status) {
            status.ListenTo(Events.AfterDiscarded, _ => Discard(), this);
        }
    }
}