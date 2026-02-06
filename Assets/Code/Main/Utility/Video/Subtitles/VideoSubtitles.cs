using Awaken.TG.MVC.Attributes;
using UnityEngine;
using UnityEngine.Video;

namespace Awaken.TG.Main.Utility.Video.Subtitles {
    [SpawnsView(typeof(VSubtitles))]
    public partial class VideoSubtitles : SubtitlesBase<Video> {
        readonly VideoPlayer _videoPlayer;

        public override Transform SubtitlesHost => ParentModel.SubtitlesHost;
        protected override SubtitlesData.Record[] Records => ParentModel.CurrentSubtitles?.records;
        protected override float Time => (float) _videoPlayer.time;

        public VideoSubtitles(VideoPlayer videoPlayer) {
            _videoPlayer = videoPlayer;
        }
    }
}