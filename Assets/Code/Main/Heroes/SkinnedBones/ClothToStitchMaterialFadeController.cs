using System.Collections.Generic;
using System.Threading;
using Awaken.Kandra;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.SkinnedBones {
    [RequireComponent(typeof(ClothToStitch)), DefaultExecutionOrder(10)]
    public class ClothToStitchMaterialFadeController : MonoBehaviour {
        [SerializeField] bool startFadedOut;
        [SerializeField] bool fadeOutIsOne = true;
        [SerializeField] float fadeDuration = 4;
        [SerializeField] string transitionShaderPropertyName = "_Transition";
        
        ClothToStitch _cloth;
        KandraRenderer[] _renderers;
        [ShowInInspector, TableList, ReadOnly]
        List<Material[]> _materialInstances;
        
        CancellationTokenSource _cts;
        int _shaderPropertyId;

        float FadeInValue => fadeOutIsOne ? 0 : 1;
        float FadeOutValue => fadeOutIsOne ? 1 : 0;

        void Awake() {
            _cloth = GetComponent<ClothToStitch>();
            _shaderPropertyId = Shader.PropertyToID(transitionShaderPropertyName);
        }

        void OnEnable() {
            if (_cloth.Instance == null) return;
            
            _renderers = _cloth.Instance.GetComponentsInChildren<KandraRenderer>();
            _materialInstances = new List<Material[]>(_renderers.Length);
            
            for (int i = 0; i < _renderers.Length; i++) {
                _renderers[i].EnsureInitialized();
                _materialInstances.Add(_renderers[i].UseInstancedMaterials());
#if UNITY_EDITOR
                for (int j = 0; j < _materialInstances[i].Length; j++) {
                    _materialInstances[i][j].name = $"{_materialInstances[i][j].name} (Instance for {gameObject.name})";
                }
#endif
            }
            
            if (startFadedOut) {
                SetValue(FadeOutValue);
            }
        }
        
        void OnDisable() {
            _cts?.Cancel();
            _cts = null;
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++) {
                if (_renderers[i] == null) continue;
                _renderers[i].UseOriginalMaterials();
            }
            _renderers = null;
            _materialInstances = null;
        }

        [Button, DisableInEditorMode]
        public void FadeIn(bool instant = false) {
            if (instant) {
                _cts?.Cancel();
                _cts = null;
                SetValue(FadeInValue);
                return;
            }
            Fade(FadeOutValue, FadeInValue).Forget();
        }

        [Button, DisableInEditorMode]
        public void FadeOut(bool instant = false) {
            if (instant) {
                _cts?.Cancel();
                _cts = null;
                SetValue(FadeOutValue);
                return;
            }
            Fade(FadeInValue, FadeOutValue).Forget();
        }

        async UniTask Fade(float startValue, float endValue) {
            if (!isActiveAndEnabled) return;
            if (_renderers.Length == 0) return;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            
            float timeLeft = fadeDuration;
            while (timeLeft > 0) {
                if (!await AsyncUtil.DelayFrame(this, 1, _cts.Token)) {
                    return;
                }
                float deltaTime = Time.deltaTime;
                timeLeft -= deltaTime;

                float newValue = timeLeft.Remap(0, fadeDuration, endValue, startValue);
                SetValue(newValue);
            }
        }
        
        void SetValue(float value) {
            for (int i = 0; i < _materialInstances.Count; i++) {
                for (int j = 0; j < _materialInstances[i].Length; j++) {
                    _materialInstances[i][j].SetFloat(_shaderPropertyId, value);
                }
            }
        }
    }
}