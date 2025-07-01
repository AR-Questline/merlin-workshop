using System;
using Awaken.TG.Assets;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Maths;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Maps.Compasses {
    public class VCompassElement : MonoBehaviour {
        public const float MinFactorValue = 0.5f;
        const float MaxVisibleAngle = 26.7f;

        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI topText;
        [SerializeField] TextMeshProUGUI bottomText;
        [SerializeField] TextMeshProUGUI orderNumber;
        [SerializeField] CanvasGroup group;
        [ARAssetReferenceSettings(new []{typeof(Sprite), typeof(Texture2D)}, true)]
        public ShareableSpriteReference defaultIcon;

        VMapCompass _vMapCompass;
        RectTransform _rectTransform;
        bool _updateTopText;
        bool _wasVisibleDistanceText;
        float _groupAlpha;
        float _anchoredPositionX;
        float _maxVisiblePositionX;
        IEventListener _updateIconListener;
        SpriteReference _iconSpriteReference;

        public int UpdateIndex { get; set; } = -2; // -2 not initialized, -1 not registered
        
        CompassElement Target { get; set; }
        Compass Compass => Target.ParentModel;
        float CompassAngle => Vector2.SignedAngle(Target.Direction(Compass.Position).ToHorizontal2(), Compass.Forward.ToHorizontal2());
        
        public void Setup(CompassElement target) {
            group.alpha = 0;
            Target = target;
            
            World.EventSystem.TryDisposeListener(ref _updateIconListener);
            _updateIconListener = Target.ListenTo(CompassElement.Events.IconUpdated, UpdateIcon);
            
            _vMapCompass = Compass.View<VMapCompass>();
            _rectTransform = (RectTransform)transform;
            var markerParentRect = (RectTransform)_vMapCompass.MarkerParent;
            _maxVisiblePositionX = markerParentRect.rect.width * 0.5f + _rectTransform.rect.width;

            UpdateIcon();

            _updateTopText = topText.isActiveAndEnabled;
            _groupAlpha = group.alpha;
            _anchoredPositionX = _rectTransform!.anchoredPosition.x;

            if (Target is CompassMarker { ShowDistance: true }) {
                bottomText.gameObject.SetActive(true);
                _wasVisibleDistanceText = true;
            } else {
                bottomText.gameObject.SetActive(false);
                _wasVisibleDistanceText = false;
            }
            
            SetOrderNumber();
        }
        
        public void CleanUp() {
            _iconSpriteReference?.Release();
            _iconSpriteReference = null;
            World.EventSystem.TryDisposeListener(ref _updateIconListener);
            Target = null;
        }

        public void UnityUpdate(float deltaTime) {
            if (!Target.ParentModel.CompassEnabled) {
                return;
            }
            
            var factor = Target.CalculateAlpha(Compass.Position);
            switch (factor.type) {
                case CompassElement.AlphaValueType.FullyOpaque:
                    if (_groupAlpha != 1) {
                        _groupAlpha = 1;
                        group.alpha = 1;
                        transform.localScale = Vector3.one;
                    }
                    UpdateVisual();
                    break;
                case CompassElement.AlphaValueType.FullyTransparent:
                    if (_groupAlpha != 0) {
                        _groupAlpha = 0;
                        group.alpha = 0;
                    }
                    // do not UpdateVisual if it's not visible
                    break;
                case CompassElement.AlphaValueType.Blended:
                    if (math.abs(_groupAlpha - factor.value) > 0.01f) {
                        _groupAlpha = factor.value;
                        group.alpha = factor.value;
                        transform.localScale = factor.value.UniformVector3();
                    }
                    UpdateVisual();
                    break;
            }
        }

        void UpdateVisual() {
            var originalAngle = CompassAngle;
            var compassAngle = Target.IgnoreAngleRequirement ? AlwaysVisibleAngle(originalAngle) : originalAngle;
            float anchoredPositionX = compassAngle * _vMapCompass.RangeMultiplier;
            if (Math.Abs(_anchoredPositionX - anchoredPositionX) > 0.1f) {
                _anchoredPositionX = anchoredPositionX;
                _rectTransform.anchoredPosition = new Vector2(anchoredPositionX, 0);
            }
            if (_updateTopText) {
                topText.text = Target.TopText;
            }

            if (Target is CompassMarker { ShowDistance: true } compassMarker && originalAngle == compassAngle) {
                if (!_wasVisibleDistanceText) {
                    bottomText.gameObject.SetActive(true);
                    _wasVisibleDistanceText = true;
                }
                bottomText.text = $"{compassMarker.Distance(Compass.ParentModel.Coords):F0} m";
            } else if (_wasVisibleDistanceText) {
                bottomText.gameObject.SetActive(false);
                _wasVisibleDistanceText = false;
            }

            bool gameObjectActive = anchoredPositionX > -_maxVisiblePositionX && anchoredPositionX < _maxVisiblePositionX;
            gameObject.SetActive(gameObjectActive);
        }

        void UpdateIcon() {
            SpriteReference incomingIcon = Target.Icon is {IsSet: true} ? Target.Icon.Get() : defaultIcon.Get();
            _iconSpriteReference?.Release();
            _iconSpriteReference = incomingIcon;
            _iconSpriteReference.SetSprite(icon);
        }

        void SetOrderNumber() {
            orderNumber.gameObject.SetActive(Target.IsNumberVisible);
            if (Target.IsNumberVisible) {
                orderNumber.text = RichTextUtil.ToRomanNumeral(Target.OrderNumber);
            }
        }

        static float AlwaysVisibleAngle(float angle) {
            return Mathf.Clamp(angle, -MaxVisibleAngle, MaxVisibleAngle);
        }
    }
}