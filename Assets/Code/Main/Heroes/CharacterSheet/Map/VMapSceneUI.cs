using Awaken.TG.Assets;
using Awaken.TG.Graphics.MapServices;
using Awaken.TG.Main.FastTravel;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility.Graphics;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map {
    [UsesPrefab("CharacterSheet/Map/" + nameof(VMapSceneUI))]
    public class VMapSceneUI : View<MapSceneUI>, IUIAware, IAutoFocusBase, IFocusSource {
        static readonly int MaskTextureID = Shader.PropertyToID("_MaskTexture");
        
        [SerializeField] AspectRatioFitter aspectRatioFitter;
        [SerializeField] RectTransform prerenderedMapTransform;
        [SerializeField] RawImage markersImage;
        [SerializeField] Image mapImage;
        
        SpriteReference _mapSprite;
        FogOfWar _fogOfWar;
        RenderTexture _markersTexture;
        RenderTexture _mapMaskTexture;
        float _viewportAspectRatio;
        
        Rect _parentScreenRect;
        Rect _markersRect;
        Vector2 _parentCanvasSize;
        float _maxOrthoSize;
        
        RaycastHit[] _markerUnderRaycastHits = new RaycastHit[4];
        
        public bool ForceFocus => true;
        public Component DefaultFocus => this;
        
        Camera MarkersCamera => Target.MarkersCamera;
        SceneReference Scene => Target.Scene;
        MapSceneData Data => Target.Data;

        protected override void OnInitialize() {
            aspectRatioFitter.aspectRatio = Data.AspectRatio;
        }

        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            _mapSprite = Data.Sprite.Get();
            _mapSprite.SetSprite(mapImage);
            
            _fogOfWar = World.Services.Get<MapService>().LoadFogOfWar(Scene);
            ApplyFogOfWar().Forget();
            
            AfterFirstCanvasCalculate().Forget();
            
            _maxOrthoSize = VMapCamera.GetOrthoSize(1, Data.Bounds.size, Data.AspectRatio);
        }
        
        protected override IBackgroundTask OnDiscard() {
            World.Services.Get<MapService>().ReleaseFogOfWar(_fogOfWar);
            _fogOfWar = null;
            _mapSprite?.Release();
            _mapSprite = null;
            if (_mapMaskTexture != null) {
                _mapMaskTexture.Release();
            }
            markersImage.texture = null;
            if (_markersTexture) {
                _markersTexture.Release();
            }
            _markersTexture = null;
            return base.OnDiscard();
        }
        
        async UniTaskVoid ApplyFogOfWar() {
            await UniTask.WaitUntil(_fogOfWar.IsInitialized);
            if (HasBeenDiscarded) {
                return;
            }
            _mapMaskTexture = _fogOfWar.CreateMaskTexture();
            if (_mapMaskTexture != null && MapUI.FogOfWarEnabled) {
                mapImage.material.SetTexture(MaskTextureID, _mapMaskTexture);
            } else {
                mapImage.material = null;
            }
            
            foreach (var marker in World.All<MapMarker>()) {
                if (marker.IsFromScene(Scene)) {
                    marker.SpawnView(Target);
                }
            }
            Target.ParentModel.Trigger(MapSceneUI.Events.ParametersChanged, Target);
            Target.ParentModel.Trigger(MapSceneUI.Events.SelectedMarkerChanged, null);
            Physics.SyncTransforms();
        }
        
        async UniTaskVoid AfterFirstCanvasCalculate() {
            if (!await AsyncUtil.WaitForEndOfFrame(this)) {
                return;
            }
            
            _markersTexture = TextureUtils.CreateRenderTextureFor(markersImage.rectTransform);
            MarkersCamera.targetTexture = _markersTexture;
            markersImage.texture = _markersTexture;
            _viewportAspectRatio = _markersTexture.width / (float)_markersTexture.height;
            
            var parent = (RectTransform)prerenderedMapTransform.parent;
            var bottomLeft = parent.WorldBottomLeftCorner();
            var topRight = parent.WorldTopRightCorner();
            _parentScreenRect = Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
            _parentCanvasSize = _parentScreenRect.size/parent.GetComponentInParent<Canvas>().transform.localScale.x;

            var markersTransform = (RectTransform)markersImage.transform;
            bottomLeft = markersTransform.WorldBottomLeftCorner();
            topRight = markersTransform.WorldTopRightCorner();
            _markersRect = Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);

            UpdateZoomAndTranslation();
            Target.ParentModel.ListenTo(MapSceneUI.Events.ParametersChanged, _ => UpdateZoomAndTranslation(), this);
        }

        void UpdateZoomAndTranslation() {
            var currentOrthoSize = VMapCamera.GetOrthoSize(Target.Zoom, Data.Bounds.size, Data.AspectRatio);
            var scale = Vector3.one * (_maxOrthoSize / currentOrthoSize);
            prerenderedMapTransform.localScale = scale;

            var viewportTranslation = -WorldToViewportTranslation(Target.WorldPosition);
            var extents = _parentCanvasSize * viewportTranslation;
            var translation = extents*scale.x;
            prerenderedMapTransform.localPosition = translation;
        }

        Vector2 WorldToViewportTranslation(Vector3 worldTranslation) {
            worldTranslation -= Data.Bounds.center;
            var maxHeight = _maxOrthoSize * 2;
            var maxWidth = maxHeight * _viewportAspectRatio;
            var xDiff = worldTranslation.x/maxWidth;
            var yDiff = worldTranslation.z/maxHeight;

            return new(xDiff, yDiff);
        }
        
        public UIResult Handle(UIEvent evt) {
            if (TryHandleMouse(evt, out var uiResult)) {
                return uiResult;
            }
            
            if (TryHandleGamepad(evt, out uiResult)) {
                return uiResult;
            }
            
            if (evt is UIEPointTo pointTo) {
                var marker = MarkerUnder(pointTo.Position);
                Target.PointingTo(marker);
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }

        bool TryHandleMouse(UIEvent evt, out UIResult uiResult) {
            if (evt is UIEMouseScroll mouseScroll) {
                Target.ChangeZoom(mouseScroll.Value);
                uiResult = UIResult.Accept;
                return true;
            }
            if (evt is UIEMouseDown mouseDown) {
                if (mouseDown.IsLeft) {
                    mouseDown.TransformIntoDrag(this);
                }
                uiResult = UIResult.Accept;
                return true;
            }

            if (MarkersCamera == null) {
                uiResult = UIResult.Ignore;
                return false;
            }
            
            if (evt is UIEDrag drag) {
                var previousPosition = ScreenToWorld(drag.PreviousPosition.screen, MarkersCamera);
                var currentPosition = ScreenToWorld(drag.Position.screen, MarkersCamera);
                Target.ChangeTranslation(previousPosition-currentPosition);
                uiResult = UIResult.Accept;
                return true;
            }
            if (evt is UIEMouseUp mouseUp) {
                if (mouseUp.IsRight) {
                    var marker = MarkerUnder(mouseUp.Position);
                    if (marker) {
                        Target.MarkerClicked(marker);
                    } else {
                        Target.GroundClicked(ScreenToWorld(mouseUp.Position.screen, MarkersCamera).XZ());
                    }
                    uiResult = UIResult.Accept;
                    return true;
                }

                if (mouseUp.IsLeft) {
                    var marker = MarkerUnder(mouseUp.Position);
                    if (marker) {
                        Target.TryFastTravel(marker);
                    }
                }
            }
            
            uiResult = UIResult.Ignore;
            return false;
        }

        bool TryHandleGamepad(UIEvent evt, out UIResult uiResult) {
            if (evt is UIKeyDownAction keyUp && keyUp.Name == KeyBindings.UI.Map.PlaceCustomMarker) {
                var marker = MarkerUnder(keyUp.Position);
                if (marker) {
                    if (!Target.TryFastTravel(marker)) {
                        Target.MarkerClicked(marker);
                    }
                } else {
                    Target.GroundClicked(ScreenToWorld(keyUp.Position.screen, MarkersCamera).XZ());
                }
                uiResult = UIResult.Accept;
                return true;
            }
            if (evt is not UIAxisAction axisAction) {
                uiResult = UIResult.Ignore;
                return false;
            }
            if (axisAction.Name == KeyBindings.UI.Generic.ScrollVertical) {
                Target.ChangeZoom(axisAction.Value*Services.Get<GameConstants>().mapGamepadScrollSpeed);
                uiResult = UIResult.Accept;
                return true;
            }
            var isMove = axisAction.Name == KeyBindings.Gameplay.Horizontal ||
                         axisAction.Name == KeyBindings.Gameplay.Vertical;
            if (isMove) {
                var composedMove = Vector2.zero;
                var moveSpeed = Target.GamepadTranslationSpeed;
                composedMove.x = 0;//RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.Horizontal)*moveSpeed;
                composedMove.y = 0;//RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.Vertical)*moveSpeed;
                if (composedMove != Vector2.zero) {
                    Target.ChangeTranslation(composedMove.X0Y());
                    uiResult = UIResult.Accept;
                    return true;
                }
            }

            uiResult = UIResult.Ignore;
            return false;
        }
        
        MapMarker MarkerUnder(UIPosition uiPosition) {
            var uv = Rect.PointToNormalized(_markersRect, uiPosition.screen);
            if (uv.x is < 0 or > 1 || uv.y is < 0 or > 1) {
                return null;
            }

            var ray = MarkersCamera.ViewportPointToRay(uv);
            var size = Physics.RaycastNonAlloc(ray, _markerUnderRaycastHits, 4096, RenderLayers.Mask.MapMarker);
            for (int index = 0; index < size; index++) {
                MapMarker hit = _markerUnderRaycastHits[index].collider.transform.GetComponentInParent<IVMapMarker>().Target;
                if (hit.Grounded is not Hero) {
                    return hit;
                }
            }

            return null;
        }

        Vector3 ScreenToWorld(Vector2 screenPoint, Camera markersCamera) {
            var viewport = Rect.PointToNormalized(_markersRect, screenPoint);
            return markersCamera.ViewportToWorldPoint(viewport).X0Z();
        }
    }
}