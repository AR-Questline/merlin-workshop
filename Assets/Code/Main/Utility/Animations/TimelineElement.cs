using Awaken.TG.Assets;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Utility.Animations {
    public partial class TimelineElement : Element<Location>, IRefreshedByAttachment<TimelineAttachment> {
        public override ushort TypeForSerialization => SavedModels.TimelineElement;

        PlayableDirector _playableDirector;
        ARAssetReference _playedTimelineReference;

        public void InitFromAttachment(TimelineAttachment spec, bool isRestored) {
            if (spec.playOnVisualLoaded && spec.initialTimeline.IsSet) {
                _playedTimelineReference = spec.initialTimeline.Get();
            }
        }
        
        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(OnVisualLoaded);
        }
        
        void OnVisualLoaded(Transform transform) {
            _playableDirector = transform.GetComponentInChildren<PlayableDirector>(true);
            if (_playableDirector == null) {
                Log.Important?.Error($"Timeline Attachment {ParentModel.ViewParent.name} has no PlayableDirector component");
                Discard();
            }

            if (_playedTimelineReference != null) {
                LoadAndPlayCurrentTimeline().Forget();
            }
        }
        
        public async UniTask PlayTimelineAsset(ShareableARAssetReference playableAssetReference) {
            UnloadCurrentTimeline();
            
            if (!playableAssetReference.IsSet) {
                return;
            }
            
            _playedTimelineReference = playableAssetReference.Get();
            await LoadAndPlayCurrentTimeline();
        }

        async UniTask LoadAndPlayCurrentTimeline() {
            var playableAsset = await _playedTimelineReference.LoadAsset<PlayableAsset>();
            
            if (playableAsset) {
                PlayTimeline(playableAsset);
            }
        }
        
        void PlayTimeline(PlayableAsset asset) {
            _playableDirector.Play(asset);
        }
        
        void UnloadCurrentTimeline() {
            _playableDirector.playableAsset = null;
            
            _playedTimelineReference?.ReleaseAsset();
            _playedTimelineReference = null;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            UnloadCurrentTimeline();
            _playableDirector = null;
        }
    }
}