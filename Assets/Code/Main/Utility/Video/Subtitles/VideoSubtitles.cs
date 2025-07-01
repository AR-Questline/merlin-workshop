using System.Linq;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine.Video;

namespace Awaken.TG.Main.Utility.Video.Subtitles {
    [SpawnsView(typeof(VVideoSubtitles))]
    public partial class VideoSubtitles : Element<Video> {
        readonly VideoPlayer _videoPlayer;

        int? _currentRecordIndex;

        public SubtitlesData.Record CurrentRecord {
            get {
                if (Records.IsNullOrEmpty()) {
                    return null;
                }
                Update();
                return _currentRecordIndex.HasValue ? Records[_currentRecordIndex.Value] : null;
            }
        }

        SubtitlesData.Record[] Records => ParentModel.CurrentSubtitles?.records;

        public VideoSubtitles( VideoPlayer videoPlayer) {
            _videoPlayer = videoPlayer;
        }
        
        void Update() {
            if (Records == null || Records.Length == 0) return;
            var time = (float) _videoPlayer.time;
            var locale = LocalizationHelper.SelectedLocale;
            var current = Records.FirstOrDefault(r => r.Time(locale).Contains(time));
            _currentRecordIndex = current != null ? Records.IndexOf(current) : null;
        }
    }
}