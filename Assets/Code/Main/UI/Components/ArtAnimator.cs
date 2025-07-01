using Awaken.TG.Assets;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    public class ArtAnimator : MonoBehaviour {
        static readonly int MaskStep = Shader.PropertyToID("_FadeStep");
        static readonly int Tint = Shader.PropertyToID("_Color");
        static readonly int SaturatedAlpha = Shader.PropertyToID("_SaturatedAlpha");
        static readonly int EdgeColor = Shader.PropertyToID("_EdgeColor");

        // === Fields

        public LayoutElement storyArtLayoutElement;
        public Image storyArtImage;

        float _storyArtPreferredHeight;
        Material _storyArtMaterial;
        SpriteReference _art;
        LazyImage _lazyImageLoader;

        Tween _activeTween;

        // === Start

        public void Setup() {
            _storyArtPreferredHeight = storyArtLayoutElement.preferredHeight;
            storyArtLayoutElement.preferredHeight = 0;
            storyArtImage.sprite = null;
            _storyArtMaterial = storyArtImage.material;
            _storyArtMaterial.SetFloat(MaskStep, 0);
            _lazyImageLoader = storyArtImage.GetComponent<LazyImage>();
        }

        public void SetDissolve(Color edgeColor, float saturateAlpha) {
            _storyArtMaterial.SetFloat(SaturatedAlpha, saturateAlpha);
            _storyArtMaterial.SetColor(EdgeColor, edgeColor);
        }

        // === Change Art
        
        public void SetArt(SpriteReference art, bool withAnimation = true) {
            _activeTween?.Complete();
            bool hasArt = art?.IsSet ?? false;

            if (!withAnimation) {
                if (!hasArt) {
                    _storyArtMaterial.SetFloat(MaskStep, 0);
                } else {
                    AssignSprite(art);
                    _storyArtMaterial.SetFloat(MaskStep, 1);
                }
            } else {
                SetArtWithAnimation(art, hasArt);
            }
        }

        void SetArtWithAnimation(SpriteReference art, bool hasArt) {
            Sequence sequence = DOTween.Sequence();
            bool previouslyHadArt = _art?.IsSet ?? false;
            // disable art
            if (!hasArt) {
                sequence.Append(DOTween.To(() => _storyArtMaterial.GetFloat(MaskStep), x => _storyArtMaterial.SetFloat(MaskStep, x), 0, 0.3f));
                sequence.Insert(0.2f, DOTween.To(() => storyArtLayoutElement.preferredHeight, x => storyArtLayoutElement.preferredHeight = x, 0, 0.3f));
                sequence.AppendCallback(() => AssignSprite(null));
            }
            // no art -> new art
            else if (!previouslyHadArt) {
                AssignSprite(art);
                sequence.Append(DOTween.To(() => storyArtLayoutElement.preferredHeight, x => storyArtLayoutElement.preferredHeight = x, _storyArtPreferredHeight, 0.3f));
                sequence.Insert(0.1f, DOTween.To(() => _storyArtMaterial.GetFloat(MaskStep), x => _storyArtMaterial.SetFloat(MaskStep, x), 1, 0.9f));
            }
            // change existing art
            else if (_art.arSpriteReference.Address != art.arSpriteReference.Address) {
                sequence.Append(DOTween.To(() => _storyArtMaterial.GetFloat(MaskStep), x => _storyArtMaterial.SetFloat(MaskStep, x), 0, 0.25f));
                sequence.AppendCallback(() => AssignSprite(art));
                sequence.Append(DOTween.To(() => _storyArtMaterial.GetFloat(MaskStep), x => _storyArtMaterial.SetFloat(MaskStep, x), 1, 0.6f));
            }
            sequence.Play();
            _activeTween = sequence;
        }

        void AssignSprite(SpriteReference art) {
            _art?.Release();
            _art = art;

            if (_lazyImageLoader != null) {
                _lazyImageLoader.enabled = _art == null;
            }
            _art?.SetSprite(storyArtImage);
        }

        public void AnimateDissolve(float duration, float destination, Color destinatinoColor, Color tint) {
            _activeTween?.Complete();
            var oldTint = _storyArtMaterial.GetColor(Tint);
            Sequence sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(() => _storyArtMaterial.GetFloat(MaskStep), x => _storyArtMaterial.SetFloat(MaskStep, x), destination, duration));
            sequence.Insert(0f, DOTween.To(() => _storyArtMaterial.GetColor(Tint), x => _storyArtMaterial.SetColor(Tint, x), tint, duration * 0.5f));
            sequence.Insert(duration - 0.1f, DOTween.To(() => _storyArtMaterial.GetColor(Tint), x => _storyArtMaterial.SetColor(Tint, x), oldTint, 0.1f));
            sequence.Play();
            _activeTween = sequence;
        }

        void OnDestroy() {
            _activeTween.Kill();
            AssignSprite(null);
        }
    }
}