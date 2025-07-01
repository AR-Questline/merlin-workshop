using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.CharacterCreators;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Maths;
using DG.Tweening;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes {
    [UsesPrefab("Hero/VHeroStaminaUsedUpEffect")]
    public class VHeroStaminaUsedUpEffect : View<HeroStaminaUsedUpEffect> {
        [SerializeField] Image vignette;
        [SerializeField] float vignetteFadeDuration;
        [SerializeField] float vignetteFadeStrength;
        [SerializeField, FoldoutGroup("Audio")] ARFmodEventEmitter emitter;
        [SerializeField, FoldoutGroup("Audio")] ARFmodEventEmitter snapshotEmitter;
        [SerializeField, FoldoutGroup("Audio/Gender")] public EventReference maleEventReference;
        [SerializeField, FoldoutGroup("Audio/Gender")] public EventReference maleSnapshotEventReference;
        [SerializeField, FoldoutGroup("Audio/Gender")] public EventReference femaleEventReference;
        [SerializeField, FoldoutGroup("Audio/Gender")] public EventReference femaleSnapshotEventReference;
        Tweener _vignetteFade;
        bool _isFlashing;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnHUD();

        protected override void OnInitialize() {
            vignette.gameObject.SetActive(true);
            vignette.color = vignette.color.WithAlpha(0);
        }

        public void ChangeGenderAudio(Gender gender) {
            // emitter.ChangeEvent(gender switch {
            //     Gender.Female => femaleEventReference,
            //     _ => maleEventReference
            // });
            // snapshotEmitter.ChangeEvent(gender switch {
            //     Gender.Female => femaleSnapshotEventReference,
            //     _ => maleSnapshotEventReference
            // });
        }

        public void StartFlash() {
            if (_isFlashing) {
                return;
            }

            _isFlashing = true;
            
            // emitter.Play();
            // snapshotEmitter.Play();
            
            vignette.color = vignette.color.WithAlpha(0);
            _vignetteFade.Kill();
            _vignetteFade = DOTween
                .ToAlpha(() => vignette.color, c => vignette.color = c, vignetteFadeStrength, vignetteFadeDuration)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public void StopFlash() {
            _isFlashing = false;
            
            // emitter.Stop();
            // snapshotEmitter.Stop();
            
            _vignetteFade.Kill();
            vignette.color = vignette.color.WithAlpha(0);
        }
    }
}