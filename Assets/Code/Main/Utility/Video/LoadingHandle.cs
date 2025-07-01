using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Utility.Video.Subtitles;
using Cysharp.Threading.Tasks;
using FMOD;
using FMODUnity;
using UnityEngine.Video;

namespace Awaken.TG.Main.Utility.Video {
    [Serializable]
    public class LoadingHandle {
        [ARAssetReferenceSettings(new []{typeof(VideoClip)}, true, AddressableGroup.IntroVideo)]
        public ARAssetReference video;
        [ARAssetReferenceSettings(new []{typeof(SubtitlesData)}, true, AddressableGroup.IntroVideo)]
        public ARAssetReference subtitlesReference;
        public EventReference videoAudio;
        
        [NonSerialized] public ARAsyncOperationHandle<VideoClip> videoRequest;
        [NonSerialized] public ARAsyncOperationHandle<SubtitlesData> subtitlesRequest;
        [UnityEngine.Scripting.Preserve] public bool HasAudio => !videoAudio.Guid.IsNull && false;//RuntimeManager.StudioSystem.getEventByID(videoAudio.Guid, out _) == RESULT.OK;
        public bool IsSet => video is {IsSet: true};

        public async UniTask<VideoClip> GetClip() {
            if (videoRequest.IsValid()) {
                return await videoRequest;
            }
            if (video.IsSet) {
                videoRequest = video.LoadAsset<VideoClip>();
                return await videoRequest;
            }

            return null;
        }

        public async UniTask<SubtitlesData> GetSubtitles() {
            if (subtitlesRequest.IsValid()) {
                return await subtitlesRequest;
            }
            if (subtitlesReference.IsSet) {
                subtitlesRequest = subtitlesReference.LoadAsset<SubtitlesData>();
                return await subtitlesRequest;
            }

            return null;
        }

        public void Release() {
            videoRequest.Release();
            videoRequest = new ARAsyncOperationHandle<VideoClip>();

            subtitlesRequest.Release();
            subtitlesRequest = new ARAsyncOperationHandle<SubtitlesData>();
        }
    }
}