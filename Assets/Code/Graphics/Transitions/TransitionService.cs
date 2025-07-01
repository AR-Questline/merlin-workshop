using System.Threading;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Graphics.Transitions {
    public class TransitionService : MonoBehaviour, IService {
        public const float DefaultFadeIn = 1f;
        public const float DefaultFadeOut = 1f;
        public const float QuickFadeIn = 1f;
        public const float QuickFadeOut = 1f;
        const float BlockInputThreshold = 0.4f;
        const float BlockInputThresholdReversed = 1f - BlockInputThreshold;
        
        static readonly Color ColorTransparent = new(0, 0, 0, 0);
        static readonly int FadeStep = Shader.PropertyToID("_FadeStep");
        static readonly int FadeRandom = Shader.PropertyToID("_Offset");
        static readonly int FadeDirection = Shader.PropertyToID("_FadeDirection");

        [SerializeField, Required] RawImage transitionImage;
        [SerializeField, Required] Material transitionMaterial;
        [SerializeField, Required] Image fadeToBlackImage;
        [SerializeField, Required] TransitionBlinking transitionBlinking;
        [SerializeField, Required] Canvas canvas;
        
        public bool InTransition => fadeToBlackImage.color.a > 0 || transitionImage.material.GetFloat(FadeStep) < 1;
        bool ShouldBlockInput => fadeToBlackImage.color.a > BlockInputThreshold || transitionImage.material.GetFloat(FadeStep) < BlockInputThresholdReversed || transitionBlinking.IsBlinking;
        
        Material _instanceMaterial;
        RenderTexture _fromBuffer;
        CancellationTokenSource _toImageToken;
        CancellationTokenSource _fadeToBlackToken;
        bool _instanceMaterialAsBlinking;
        TransitionBlocker _blocker;

        void Start() {
            var (width, height) = PlatformUtils.IsXboxScarlettS
                ? (1920, 1080)
                : (Screen.currentResolution.width, Screen.currentResolution.height);

            _fromBuffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32) {
                name = "Runtime_TransitionServiceTexture"
            };

            _instanceMaterial = new Material(transitionMaterial);
            transitionImage.material = _instanceMaterial;
        }

        // === Starting operations
        public void SetToBlack(bool clearTexture = true) {
            KillSequences();

            transitionImage.color = Color.black;
            if (clearTexture) {
                transitionImage.texture = null;
            }

            RandomizeFadeValues();
            transitionImage.material.SetInt(FadeDirection, 0);
            UpdateFadeStep(0);
        }

        public async UniTask ToBlack(float duration, bool clearTexture = true, bool ignoreTimescale = true) {
            transitionImage.material.SetInt(FadeDirection, 0);
            transitionImage.color = Color.black;
            if (clearTexture) {
                transitionImage.texture = null;
            }

            RandomizeFadeValues();
            if (await ToImageSequence(duration, ignoreTimescale)) {
                UpdateFadeStep(0);
            }
        }

        public async UniTask<bool> TransitionToBlack(float duration) {
            return await BlackSequence(duration, true);
        }

        public async UniTask<bool> TransitionFromBlack(float duration) {
            return await BlackSequence(duration, false);
        }

        // === Ending operations

        public async UniTask ToCamera(float duration, float delay = 0, bool ignoreTimescale = true, TransitionBlinking.Data? blinkingData = null) {
            _toImageToken?.Cancel();
            _toImageToken = new CancellationTokenSource();
            
            if (delay > 0 && !await AsyncUtil.DelayTime(gameObject, delay, _toImageToken.Token, ignoreTimescale)) {
                return;
            }

            transitionImage.material.SetInt(FadeDirection, 1);
            if (blinkingData.HasValue) {
                // === Skipping ugly old transition
                UpdateFadeStep(1);
                transitionBlinking.Blink(blinkingData.Value);
                if (!await AsyncUtil.DelayTime(gameObject, blinkingData.Value.duration, _toImageToken.Token, ignoreTimescale)) {
                    transitionBlinking.ResetBlinking();
                    TryUnblockInput();
                }
            } else {
                transitionBlinking.ResetBlinking();
                await ToCameraSequence(duration, ignoreTimescale);
            }
        }

        // === Other operations
        [UnityEngine.Scripting.Preserve]
        void TakeScreenShoot() {
            var cam = World.Only<GameCamera>().MainCamera;
            var prevEnabledState = cam.enabled;
            cam.enabled = true;
            cam.targetTexture = _fromBuffer;
            cam.Render();
            cam.targetTexture = null;
            cam.enabled = prevEnabledState;
        }
        
        public void KillSequences() {
            _toImageToken?.Cancel();
            _toImageToken = null;

            _fadeToBlackToken?.Cancel();
            _fadeToBlackToken = null;

            transitionBlinking.ResetBlinking();

        }

        void RandomizeFadeValues() {
            transitionImage.material.SetVector(FadeRandom, new Vector4(RandomUtil.UniformFloat(0, 1), RandomUtil.UniformFloat(0, 1), 0, 0));
        }

        void UpdateFadeStep(float newValue) {
            transitionImage.material.SetFloat(FadeStep, newValue);
        }

        // === Sequences
        async UniTask<bool> ToImageSequence(float duration, bool ignoreTimescale) {
            _toImageToken?.Cancel();
            _toImageToken = new CancellationTokenSource();
            
            TryBlockInput();
            
            float currentFade = transitionImage.material.GetFloat(FadeStep);
            if (duration > 0) {
                do {
                    currentFade -= (ignoreTimescale ? Time.unscaledDeltaTime : Time.deltaTime) / duration;
                    currentFade = math.clamp(currentFade, 0, 1);
                    UpdateFadeStep(currentFade);
                    if (!await AsyncUtil.DelayFrame(gameObject, cancellationToken: _toImageToken.Token)) {
                        return false;
                    }
                } while (currentFade > 0);
            }
            UpdateFadeStep(0);
            return true;
        }

        async UniTask<bool> ToCameraSequence(float duration, bool ignoreTimescale) {
            if (!ignoreTimescale) {
                TryBlockInput();
            }

            if (duration > 0) {
                float currentFade = transitionImage.material.GetFloat(FadeStep);
                do {
                    currentFade += (ignoreTimescale ? Time.unscaledDeltaTime : Time.deltaTime) / duration;
                    currentFade = math.clamp(currentFade, 0, 1);
                    UpdateFadeStep(currentFade);
                    if (currentFade > BlockInputThresholdReversed) {
                        TryUnblockInput();
                    }
                    if (!await AsyncUtil.DelayFrame(gameObject, cancellationToken: _toImageToken.Token)) {
                        return false;
                    }
                } while (currentFade < 1);
            }
            
            UpdateFadeStep(1);
            
            TryUnblockInput();

            return true;
        }

        async UniTask<bool> BlackSequence(float duration, bool toBlack) {
            _fadeToBlackToken?.Cancel();
            _fadeToBlackToken = new CancellationTokenSource();

            if (toBlack) {
                TryBlockInput();
            }

            if (duration > 0) {
                int sequenceSign = toBlack ? 1 : -1;
                fadeToBlackImage.color = toBlack ? ColorTransparent : Color.black;
                Color currentColor = fadeToBlackImage.color;
                do {
                    currentColor.a += (sequenceSign * Time.unscaledDeltaTime) / duration;
                    currentColor.a = math.clamp(currentColor.a, 0, 1);
                    fadeToBlackImage.color = currentColor;
                    if (!toBlack && currentColor.a < BlockInputThreshold) {
                        TryUnblockInput();
                    }
                    if (!await AsyncUtil.DelayFrame(gameObject, cancellationToken: _fadeToBlackToken.Token)) {
                        return false;
                    }
                } while (toBlack ? currentColor.a < 1 : currentColor.a > 0);
            }
            fadeToBlackImage.color = toBlack ? Color.black : ColorTransparent;
            
            if (!toBlack) {
                TryUnblockInput();
            }
            
            return true;
        }

        void TryBlockInput() {
            if (_blocker != null) {
                return;
            }

            _blocker = World.Add(new TransitionBlocker());
        }

        void TryUnblockInput() {
            if (_blocker == null) {
                return;
            }
            if (ShouldBlockInput) {
                return;
            }
            
            _blocker.Discard();
            _blocker = null;
        }
    }
}