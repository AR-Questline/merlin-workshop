using System.Threading.Tasks;
using Awaken.TG.Assets;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.InputToText;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Animations;
using Awaken.Utility.Animations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Awaken.TG.Main.Tutorials.Views {
    [ExecuteAlways]
    public class VSmartText : View<IModel> {

        const int BigStringThreshold = 120;
        
        public TMP_Text textMesh;
        public TMP_Text bigTextMesh;
        public bool isUI = true;
        public bool widthChangeAllowed = true;
        public bool pivotChangeAllowed = true;
        public Animator animator;

        public Transform smallFrameParent;
        public Transform bigFrameParent;
        public Transform mediaParent;
        
        Transform _target;
        Vector3 _offset;
        bool _hasAnimationEnded;
        
        // image
        public Image image;
        SpriteReference _imageReference;
        
        // video player
        public GameObject videoPlayerGO;
        public RawImage videoImage;
        public VideoPlayer videoPlayer;
        ARAssetReference _clipReference;

        public override Transform DetermineHost() {
            var hosting = Services.Get<ViewHosting>();
            return isUI ? hosting.OnMainCanvas() : hosting.DefaultHost();
        }

        public void Fill(string text) {
            bool isBig = text.Length > BigStringThreshold;
            text = Services.Get<InputToTextMapping>().ReplaceInText(text);
            text = text.FormatSprite();
            if (smallFrameParent != null) {
                smallFrameParent.gameObject.SetActive(!isBig);
            }
            if (bigFrameParent != null) {
                bigFrameParent.gameObject.SetActive(isBig);
                bigTextMesh.text = text;
            }

            textMesh.text = text;
        }

        public void SetSize(SmartTextSettings.TextWidth textWidth) {
            if (!widthChangeAllowed) {
                return;
            }
            var rectTrans = (RectTransform) transform;
            rectTrans.sizeDelta = new Vector2((int)textWidth, rectTrans.sizeDelta.y);
        }

        public void SetPivot(Vector2 pivot) {
            if (!pivotChangeAllowed) return;
            GetComponent<RectTransform>().pivot = pivot;
        }

        public void SetTarget(Transform target, Vector3 offset = new Vector3()) {
            _target = target;
            _offset = offset;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            transform.position = _target.position + _offset;
        }

        public void SetImage(SpriteReference reference) {
            if (mediaParent != null) {
                mediaParent.gameObject.SetActive(true);
            }
            image.gameObject.SetActive(true);
            _imageReference = reference;
            _imageReference.RegisterAndSetup(this, image);
        }

        public void SetVideo(ARAssetReference reference) {
            if (mediaParent != null) {
                mediaParent.gameObject.SetActive(true);
            }
            videoPlayerGO.SetActive(true);
            _clipReference = reference;
            _clipReference.LoadAsset<VideoClip>().OnComplete(handle => videoPlayer.clip = handle.Result);

            RectTransform rectTrans = videoImage.GetComponent<RectTransform>();
            videoPlayer.targetTexture = new RenderTexture((int) rectTrans.rect.width, (int) rectTrans.rect.height, 0) {name = "Runtime_VSmartTextVideo"};
            videoImage.texture = videoPlayer.targetTexture;
        }

        void Update() {
            if (_target != null) {
                transform.position = _target.position + _offset;
            }
        }
        
        protected override IBackgroundTask OnDiscard() {
            _hasAnimationEnded = animator == null;
            if (animator != null) {
                animator.SetTrigger("Hide");
            }
            return new BackgroundTask(WaitForAnimationEnd());
        }

        async Task WaitForAnimationEnd() {
            while (!_hasAnimationEnded) {
                await Task.Delay(100);
            }
            _clipReference?.ReleaseAsset();
        }

        // Animator trigger
        public void Finish() {
            _hasAnimationEnded = true;
        }
    }
}