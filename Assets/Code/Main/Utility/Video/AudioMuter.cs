using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Utility.Video {
    /// <summary>
    /// Mutes groups that should not be playing while in a video or loading screen:
    /// - Music
    /// - SFX
    /// - Voice-Overs
    /// </summary>
    [SpawnsView(typeof(VAudioMuter))]
    public partial class AudioMuter : Element<Model> {
        public sealed override bool IsNotSaved => true;
    }
}