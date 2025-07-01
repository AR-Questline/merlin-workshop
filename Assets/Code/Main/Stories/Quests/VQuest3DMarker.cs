using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.UI.Stickers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories.Quests {
    [UsesPrefab("Quest/VQuest3DMarker")]
    public class VQuest3DMarker : View<Quest3DMarker> {
        [SerializeField] Image markerImage;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TextMeshProUGUI orderNumber;
        
        Sticker _sticker;
        Camera _mainCamera;
        Transform _heroFollowCamera;
        Transform _reference;
        Vector3 _offset;
        float _minDistance;
        float _maxDistance;

        float MinSqrDistance => _minDistance * _minDistance;
        float MaxSqrDistance => _maxDistance * _maxDistance;
        
        // === Initialization
        protected override void OnMount() {
            GameCamera gameCamera = World.Only<GameCamera>();
            GameConstants constants = World.Services.Get<GameConstants>();
            
            _mainCamera = gameCamera.MainCamera;
            _heroFollowCamera = gameCamera.CinemachineVirtualCamera.transform;
            
            _minDistance = constants.questMarkerMinDistance;
            _maxDistance = constants.questMarkerMaxDistance;
            
            UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStateChanged, state => gameObject.SetActive(state.IsMapInteractive), this);
            Target.questIcon.RegisterAndSetup(this, markerImage);
            SetOrderNumber();
            SetupSticker();
            UpdateVisibility(false);
        }

        void SetOrderNumber() {
            orderNumber.text = RichTextUtil.ToRomanNumeral(Target.orderNumber);
            orderNumber.gameObject.SetActive(Target.isNumberVisible);
        }
        
        void SetupSticker() {
            if (Target.groundedTarget is IQuest3DMarkerLocationTarget markerTarget) {
                var stickToRef = markerTarget.StickToReference;
                if (stickToRef == null) {
                    return;
                }
                if (!stickToRef.Target.IsVisualLoaded) {
                    stickToRef.Target.ListenTo(Location.Events.VisualLoaded, SetupSticker, this);
                    return;
                }
                _reference = stickToRef.transform;
            } else {
                _reference = Target.groundedTarget.MainView?.transform; 
            }
            if (_reference == null) {
                return;
            }
            Bounds bounds = TransformBoundsUtil.FindBounds(_reference, true);

            _offset = new Vector3(0f, bounds.size.y, 0f);

            if (_sticker != null) {
                _sticker.anchor = _reference;
                var position = _sticker.positioning;
                position.worldOffset = _offset;
                _sticker.positioning = position;
                Services.Get<MapStickerUI>().RealignStickers();
            } else {
                _sticker = Services.Get<MapStickerUI>().StickTo(_reference, new StickerPositioning {
                    pivot = new Vector2(0.5f, 0),
                    worldOffset = _offset,
                    underneath = false
                });
                transform.SetParent(_sticker, false);
            }
        }

        void Update() {
            if (_reference == null || _heroFollowCamera == null || _mainCamera == null) {
                markerImage.enabled = false;
                return;
            }

            Vector3 refPosition = _reference.position;
            Vector3 offsetPosition = refPosition + _offset;
            Vector3 camForward = _heroFollowCamera.forward;
            Vector3 questDirection = offsetPosition - _heroFollowCamera.position;
            questDirection.Normalize();

            float dot = Vector3.Dot(camForward, questDirection);

            Vector2 viewportPoint = _mainCamera.WorldToViewportPoint(offsetPosition);
            bool isVisible = dot > 0 && viewportPoint.x >= 0f && viewportPoint.x <= 1f && viewportPoint.y >= 0f && viewportPoint.y <= 1f;
            UpdateVisibility(isVisible);
        }

        void UpdateVisibility(bool isVisible) {
            markerImage.enabled = isVisible;
            orderNumber.enabled = isVisible;

            bool isStickerValid = _sticker is { anchor: not null };
            if (!isVisible || !isStickerValid) {
                return;
            }

            float sqrDistance = (_sticker.anchor.position - _heroFollowCamera.position).sqrMagnitude;
            float factor = Mathf.InverseLerp(MaxSqrDistance, MinSqrDistance, sqrDistance);
            canvasGroup.alpha = factor;
        }
    }
}
