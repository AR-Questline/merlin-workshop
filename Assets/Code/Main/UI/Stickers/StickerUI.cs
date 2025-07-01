using System;
using System.Collections.Generic;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Cinemachine;
using UnityEngine;

namespace Awaken.TG.Main.UI.Stickers {
    public class StickerUI : MonoBehaviour {
        
        // === Editor properties

        [SerializeField] GameObject stickerPrefab;
        [SerializeField] GameObject overlayStickerPrefab;

        CameraHandle _cameraHandle;
        Camera _overridenCamera;
        
        Canvas _referenceCanvas;
        
        // === State

        HashSet<Sticker> _activeStickers = new();
        HashSet<Sticker> _stickersToRemove = new();
        
        // === Events
        public static class Events {
            public static readonly Event<IModel, Sticker> OnBeforeDestroy = new(nameof(OnBeforeDestroy));
        }

        public void AssignCameraOverride(Camera camera) {
            _overridenCamera = camera;
        }
        
        // === Keeping alignment

        public void Init() {
            _referenceCanvas = World.Services.Get<CanvasService>().StickersCanvas;
            CinemachineCore.CameraUpdatedEvent.AddListener(_ => RealignStickers());
        }

        void RealignStickers() {
            _cameraHandle ??= World.Only<CameraStateStack>().MainHandle;
            
            foreach (Sticker s in _activeStickers) {
                s.Realign(_overridenCamera ?? _cameraHandle, _referenceCanvas);
            }

            foreach (Sticker s in _stickersToRemove) {
                _activeStickers.Remove(s);
                ReleaseViews(s);
                if (s != null && s.gameObject != null) {
                    Destroy(s.gameObject);
                }
            }
            _stickersToRemove.Clear();
        }

        void ReleaseViews(Sticker s) {
            if (s == null) {
                return;
            }

            View view = s.gameObject.GetComponentInChildren<View>();
            if (view is { HasBeenDiscarded: false }) {
                view.GenericTarget.Trigger(Events.OnBeforeDestroy, s);
                if (view != null) {
                    view.Discard();
                }
            }
        }

        // === Creating stickers

        Sticker StickTo(Model model, StickerPositioning positioning) {
            return StickTo(model.MainView.transform, positioning);
        }

        Sticker StickTo(View view, StickerPositioning positioning) {
            return StickTo(view.transform, positioning);
        }

        Sticker StickTo(Transform anchor, StickerPositioning positioning) {
            // instantiate and move
            Sticker s = Sticker.Create(stickerPrefab, anchor, positioning);
            s.transform.SetParent(transform, false);
            if (positioning.underneath) {
                s.transform.SetAsFirstSibling();
            }

            if (_cameraHandle != null) {
                s.Realign(_cameraHandle, _referenceCanvas);
            }

            // register and watch
            _activeStickers.Add(s);
            s.WhenDone += RemoveSticker;
            // done
            return s;
        }

        OverlaySticker AddOverlaySticker() {
            OverlaySticker s = OverlaySticker.Create(overlayStickerPrefab);
            s.transform.SetParent(transform, false);
            return s;
        }

        void SetActive(bool active) {
            gameObject.SetActive(active);
        }

        void RemoveSticker(Sticker s) {
            _stickersToRemove.Add(s);
        }

        [Serializable]
        public abstract class Wrapper {
            [SerializeField] StickerUI stickers;

            public void Init() => stickers.Init();
            public Sticker StickTo(Model model, StickerPositioning positioning) => stickers.StickTo(model, positioning);
            public Sticker StickTo(View view, StickerPositioning positioning) => stickers.StickTo(view, positioning);
            public Sticker StickTo(Transform anchor, StickerPositioning positioning) => stickers.StickTo(anchor, positioning);
            public OverlaySticker AddOverlaySticker() => stickers.AddOverlaySticker();
            public void SetActive(bool active) => stickers.SetActive(active);
            public void RealignStickers() => stickers.RealignStickers();
            public void AssignCameraOverride(Camera camera) => stickers.AssignCameraOverride(camera);
        }
    }
    
    [Serializable]
    public class MapStickerUI : StickerUI.Wrapper, IService { }
    
    [Serializable]
    public class UISpecificStickerUI : StickerUI.Wrapper { }
}
