using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Video.Subtitles {
    [UsesPrefab("UI/Video/" + nameof(VSimpleSubtitlesHost))]
    public class VSimpleSubtitlesHost : View<Model> {
        public Transform subtitlesHost;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        public static VSimpleSubtitlesHost BindToModel(Model model) {
            return World.SpawnView<VSimpleSubtitlesHost>(model, removeAutomatically: true);
        }
    }
}