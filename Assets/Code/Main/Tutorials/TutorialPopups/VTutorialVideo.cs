using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC.Attributes;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Tutorials.TutorialPopups {
    [UsesPrefab("UI/Tutorials/" + nameof(VTutorialVideo))]
    public class VTutorialVideo : VTutorialMultimedia<TutorialVideo>, IVideoHost {
        [SerializeField] RawImage rawImage;
        
        public RawImage VideoDisplay => rawImage;
        public GameObject VideoTextureHolder => rawImage.gameObject;
        public Transform SubtitlesHost => null;

        public void OnVideoStarted() {
            loadingIcon.SetActive(false);
        }

        protected override void ShowContent() {
            base.ShowContent();
            InitVideo().Forget();
        } 

        async UniTaskVoid InitVideo() {
            // We need to wait to allow layout elements to place themselves correctly
            await UniTask.DelayFrame(2);
            Target.InitVideo();
        }
    }
}