using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Video.Subtitles {
    [UsesPrefab("UI/Video/VVideoSubtitles")]
    public class VVideoSubtitles : View<VideoSubtitles> {
        [SerializeField] TMP_Text text;

        SubtitlesData.Record _lastRecord;

        public override Transform DetermineHost() => Target.ParentModel.SubtitlesHost;

        protected override void OnInitialize() {
            text.text = string.Empty;
        }

        void LateUpdate() {
            var currentRecord = Target.CurrentRecord;
            if (_lastRecord == currentRecord) {
                return;
            }
            _lastRecord = currentRecord;
            text.text = currentRecord?.text;
        }
    }
}