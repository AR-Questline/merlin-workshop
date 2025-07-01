using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Extensions;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroHealthVignette : ViewComponent<Hero> {
        const float Threshold = 0.25f;
        
        public Image healthVignette;
        public GameObject staticHealthVignette;
        public float vignetteFadeDuration;
        Tweener _vignetteFade;
        [SerializeField] AnimationCurve curve;
        [SerializeField] float outOfCombatMultiplier = 0.5f;
        [Title("Audio")]
        [SerializeField] ARFmodEventEmitter emitter;
        [SerializeField] ARFmodEventEmitter snapshotEmitter;

        bool _forceActive;
        bool _isAudioPlaying;
        float _currentAlpha;

        protected override void OnAttach() {
            SetupHealthVignette();
        }

        public static class Events {
            public static readonly Event<Hero, bool> HealthVignetteToggled = new(nameof(HealthVignetteToggled));
        }

        void SetupHealthVignette() {
            Target.ListenTo(ICharacter.Events.CombatEntered, _ => OnHealthChanged(Target.Stat(AliveStatType.Health)), this);
            Target.ListenTo(ICharacter.Events.CombatExited, _ => OnHealthChanged(Target.Stat(AliveStatType.Health)), this);
            Target.ListenTo(Stat.Events.StatChanged(AliveStatType.Health), OnHealthChanged, this);
            Target.ListenTo(Events.HealthVignetteToggled, OnToggled, this);
            healthVignette.gameObject.SetActive(true);
            staticHealthVignette.SetActive(false);
            HideVignette();
        }

        void OnToggled(bool active) {
            _forceActive = active;
            if (!_forceActive) {
                OnHealthChanged(Target.Stat(AliveStatType.Health));
            } else {
                _vignetteFade.KillWithoutCallback();
                _vignetteFade = DOTween.ToAlpha(() => healthVignette.color, c => healthVignette.color = c, 1f, vignetteFadeDuration);
                _vignetteFade.OnKill(HideVignette);
            }
        }

        void OnHealthChanged(Stat stat) {
            if (_forceActive) return;
            if (Target is not {HasBeenDiscarded: false}) return;
            
            var health = Target.Element<HealthElement>().Health;
            if (health.Percentage > Threshold) {
                _vignetteFade.KillWithoutCallback();
                _vignetteFade = null;
                HideVignette();
                staticHealthVignette.SetActive(false);
                StopAudio();
            } else {
                float elapsed = _vignetteFade?.Elapsed() ?? 0;
                float alpha = curve.Evaluate(health.Percentage.Remap(0, Threshold, 0, 1));

                if (alpha != _currentAlpha) {
                    _currentAlpha = alpha;
                    alpha *= Target.HeroCombat.IsHeroInFight ? 1 : outOfCombatMultiplier;
                    HideVignette();
                    
                    _vignetteFade.KillWithoutCallback();
                    _vignetteFade = DOTween
                        .ToAlpha(() => healthVignette.color, c => healthVignette.color = c, alpha, vignetteFadeDuration)
                        .SetLoops(-1, LoopType.Yoyo);
                    _vignetteFade.Goto(elapsed, true);
                    _vignetteFade.OnKill(() => RestartVignette(stat).Forget());
                    
                    staticHealthVignette.SetActive(true);
                    PlayAudio();
                }
            }
        }

        void HideVignette() {
            healthVignette.color = healthVignette.color.WithAlpha(0);
        }
        
        async UniTaskVoid RestartVignette(Stat stat) {
            // Exit callback stack
            if (!await AsyncUtil.DelayFrame(Target)) {
                return;
            }
            OnHealthChanged(stat);
        }

        void PlayAudio() {
            if (_isAudioPlaying) {
                return;
            }
            
            // emitter.Play();
            // snapshotEmitter.Play();
            _isAudioPlaying = true;
        }

        void StopAudio() {
            if (!_isAudioPlaying) {
                return;
            }
            
            // emitter.Stop();
            // snapshotEmitter.Stop();
            _isAudioPlaying = false;
        }
    }
}