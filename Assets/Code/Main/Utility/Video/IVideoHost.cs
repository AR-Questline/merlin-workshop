using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Utility.Video {
    public interface IVideoHost {
        RawImage VideoDisplay { get; }
        GameObject VideoTextureHolder { get; }
        Transform SubtitlesHost { get; }
        void OnVideoStarted();
    }
}