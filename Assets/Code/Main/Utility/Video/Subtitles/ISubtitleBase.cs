using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Video.Subtitles {
    public interface ISubtitleBase : IElement {
        public SubtitlesData.Record CurrentRecord { get; }
        public Transform SubtitlesHost { get; }
    }
}