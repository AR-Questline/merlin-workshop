using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.Utility.Cameras {
    [Serializable]
    public struct UIFullscreenRenderer {
        [SerializeField, Required] Transform slot;
        [SerializeField, Required] RawImage image;
        
        Camera _camera;
        RenderTexture _texture;
        Tween _fadeTween;
        
        public Transform Slot => slot;

        public void ChangeCamera(Camera camera) {
            if (_camera != null) {
                _camera.targetTexture = null;
            }

            _camera = camera;
            
            if (_camera != null) {
                if (_texture == null || _texture.width != Screen.width || _texture.height != Screen.height) {
                    _texture?.Release();
                    _texture = new RenderTexture(Screen.width, Screen.height, 0);
                    image.texture = _texture;
                }
                _camera.targetTexture = _texture;
            }
        }

        public void Release() {
            _fadeTween?.Kill();
            _fadeTween = null;
            _texture?.Release();
            _texture = null;
            image.texture = null;
            if (_camera != null) {
                _camera.targetTexture = null;
            }
        }
    }
}