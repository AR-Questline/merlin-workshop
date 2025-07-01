using System;
using Awaken.Kandra;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class VCMagicGauntlet : VCCharacterMagicVFX {
        // === Shader Properties
        static readonly int VFXRelease = Shader.PropertyToID("Release");
        static readonly int VFXCharge = Shader.PropertyToID("Charge");
        static readonly int EffectValueId = Shader.PropertyToID("_EffectValue");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        const float SequencesDuration = 0.8f;
        
        float _defaultGlowValue;
        float _lowGlowValue;
        float _highGlowValue;

        float _currentGlow;
        Sequence _glowReleaseSequence;

        bool _useHighGlowOnCharge;
        bool _noGlowOnRelease;

        KandraRenderer _renderer;
        Gradient _gradient;
        Color _color;
        Material[] _materials = Array.Empty<Material>();

        // === Initialization
        public void Init(Gradient gradient, Color color, bool useHighGlowOnCharge, bool noGlowOnRelease, float defaultGlow, float lowGlow, float highGlow) {
            _defaultGlowValue = defaultGlow;
            _lowGlowValue = lowGlow;
            _highGlowValue = highGlow;
            _useHighGlowOnCharge = useHighGlowOnCharge;
            _noGlowOnRelease = noGlowOnRelease;
            _gradient = gradient;
            _color = color;
        }

        protected override void EnsureOnEnableEvent() { }

        protected override void Initialize() {
            base.Initialize();
            _renderer = GetComponentInChildren<KandraRenderer>();
            _materials = _renderer.UseInstancedMaterials();
            foreach (var material in _materials) {
                material.SetColor(ColorId, _color);
            }

            if (_visualEffect != null) {
                if (_visualEffect.HasGradient(ColorId)) {
                    _visualEffect.SetGradient(ColorId, _gradient);
                }
                _visualEffect.Play();
            }

            GlowReset();
        }

        protected override void OnDiscard() {
            _glowReleaseSequence.Kill();
            _materials = Array.Empty<Material>();
            if (_renderer != null) {
                _renderer.UseOriginalMaterials();
            }
        }

        protected override void AttachVFX() {
            // This script is attached from CharacterMagic so we don't change it's parent here.
        }
        
        // === Casting Lifecycle
        protected override void OnCastingCanceled() {
            GlowReset();
        }

        protected override void OnCastingSuccessfullyEnded() {
            GlowRelease();
        }

        protected override void OnCastingSuccessfullyBegun() {
            ChargeStart();
        }

        void GlowReset() {
            // reset VFX
            if (_visualEffect != null) {
                _visualEffect.Play();
            }
            LerpGlow(_defaultGlowValue, SequencesDuration);
        }

        void ChargeStart() {
            if (_visualEffect != null) {
                _visualEffect.SendEvent(VFXCharge);
            }
            LerpGlow(_useHighGlowOnCharge? _highGlowValue : _lowGlowValue, SequencesDuration);
        }

        // === Showing effects
        void GlowRelease() {
            _glowReleaseSequence.Kill();
            _glowReleaseSequence = DOTween.Sequence();
            
            if (_visualEffect != null) {
                _glowReleaseSequence.AppendCallback(() => _visualEffect.SendEvent(VFXRelease));
            }
            
            if (_noGlowOnRelease) {
                _glowReleaseSequence.Append(DOTween.To(() => _currentGlow, UpdateGlow, _defaultGlowValue, SequencesDuration));
            } else {
                _glowReleaseSequence.Append(DOTween.To(() => _currentGlow, UpdateGlow, _highGlowValue, SequencesDuration / 3f));
                _glowReleaseSequence.AppendInterval(SequencesDuration / 3f);
                _glowReleaseSequence.Append(DOTween.To(() => _currentGlow, UpdateGlow, _defaultGlowValue, SequencesDuration / 3f));
                _glowReleaseSequence.Play();
            }

        }

        void LerpGlow(float targetValue, float duration) {
            _glowReleaseSequence.Kill();
            _glowReleaseSequence = DOTween.Sequence();
            _glowReleaseSequence.Append(DOTween.To(() => _currentGlow, UpdateGlow, targetValue, duration));
            _glowReleaseSequence.Play();
        }

        void UpdateGlow(float glow) {
            _currentGlow = glow;

            foreach (var material in _materials) {
                material.SetFloat(EffectValueId, _currentGlow);
            }
        }
    }
}
