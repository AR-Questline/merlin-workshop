using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.UI.HeroCreator.ViewComponents;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.UI.HeroCreator {
    [UsesPrefab("UI/" + nameof(VRotator))]
    public class VRotator : View, IUIAware {
        const float RotationSpeed = -500f;
        
        [SerializeField, Required] RectTransform rotatableArea;
        public float gamepadSpeedMultiplier = 0.5f;
        public bool onlyHorizontal;

        Vector3 _lastScreenPosition;
        Camera _camera;
        RotatableObject _targetObject;
        
        protected override bool CanNestInside(View view) => false;
        
        protected override void OnMount() {
            base.OnMount();
            rotatableArea = rotatableArea != null ? rotatableArea : GetComponent<RectTransform>();
        }
        
        public void SetupRotatableArea(RotatableObject rotatable) {
            _targetObject = rotatable;
            RecalculateRotatableArea().Forget();
        }
        
        public async UniTaskVoid RecalculateRotatableArea() {
            // Camera is updated with a Cinemachine, so its proper may not be valid yet.
            if (await AsyncUtil.DelayFrame(this) == false) {
                return;
            }

            if (_targetObject == null) {
                return;
            }
            
            if (_camera == null) {
                _camera = World.Only<CameraStateStack>().MainCamera;
            }
            
            if (_camera == null) return;

            Vector3 screenPos = _camera.WorldToScreenPoint(_targetObject.transform.position);
                    
            if (_lastScreenPosition == screenPos) return;
            _lastScreenPosition = screenPos;
                    
            Vector2 areaPosition = rotatableArea.position;
            rotatableArea.position = new Vector2(screenPos.x, areaPosition.y);
        }
        
        void Update() {
            if (!RewiredHelper.IsGamepad) return;

            float deltaX = 0;//RewiredHelper.Player.GetAxis("CameraHorizontal") * gamepadSpeedMultiplier * Time.unscaledDeltaTime;
            float deltaY = 0;//RewiredHelper.Player.GetAxis("CameraVertical") * gamepadSpeedMultiplier * Time.unscaledDeltaTime;
            Rotate(deltaX, deltaY);
        }

        public UIResult Handle(UIEvent evt) {
            if (_camera == null) {
                _camera = World.Only<CameraStateStack>().MainCamera;
            }
            
            if (evt is UIEMouseDown { IsLeft: true } md) {
                md.TransformIntoDrag(this);
            }
            
            if (evt is UIEDrag drag) {
                float currentX = _camera.ScreenToViewportPoint(drag.Position.screen).x;
                float currentY = _camera.ScreenToViewportPoint(drag.Position.screen).y;
                float previousX = _camera.ScreenToViewportPoint(drag.PreviousPosition.screen).x;
                float previousY = _camera.ScreenToViewportPoint(drag.PreviousPosition.screen).y;
                Rotate(currentX - previousX, currentY - previousY);
            }

            return UIResult.Ignore;
        }

        void Rotate(float horizontalDiff, float verticalDiff) {
            if (!CanRotate()) return;
            
            var targetTransform = _targetObject.transform;
            if (targetTransform) {
                targetTransform.Rotate(Vector3.up, horizontalDiff * RotationSpeed);
                if (!onlyHorizontal) {
                    targetTransform.Rotate(Vector3.right, verticalDiff * RotationSpeed);
                }
            }
        }
        
        bool CanRotate() {
            return _targetObject != null && _targetObject.Rotatable && _targetObject.gameObject.activeInHierarchy;
        }
    }
}