using System;
using System.Threading;
using Awaken.Kandra;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.Utils;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Fights.NPCs {
    public class NpcWyrdConversionMarker : MonoBehaviour {
        [SerializeField] GameObject[] objectsToEnabledOnConversion = Array.Empty<GameObject>();
        [SerializeField, BoxGroup("Eyes")] RenderersMarkers markers;
        [SerializeField, BoxGroup("Eyes"), ShowIf(nameof(ShowEyesSettings))] string shaderName = "_Iris_Emission_Intensity";
        [SerializeField, BoxGroup("Eyes"), ShowIf(nameof(ShowEyesSettings))] float eyesDefaultEmission = 0;
        [SerializeField, BoxGroup("Eyes"), ShowIf(nameof(ShowEyesSettings))] float eyesWyrdEmission = 10;
        [SerializeField, BoxGroup("Eyes"), ShowIf(nameof(ShowEyesSettings))] float lerpSpeed = 1f;

        KandraRenderer _rendererWithEyes;
        int _eyesIndex;
        Material _instancedMaterial;
        CancellationTokenSource _cts;

        KandraRenderer RendererWithEyes {
            get {
                if (_rendererWithEyes != null) {
                    return _rendererWithEyes;
                }

                foreach (var marker in markers.KandraMarkers) {
                    if (marker.MaterialType == RendererMarkerMaterialType.Eyes) {
                        _rendererWithEyes = marker.Renderer;
                        _eyesIndex = marker.Index;
                        return _rendererWithEyes;
                    }
                }

                return null;
            }
        }

        bool ShowEyesSettings => markers != null;
        
        void Awake() {
            DisableWyrdObjects();
        }

        [Button]
        public void EnableWyrdObjects() {
            foreach (var go in objectsToEnabledOnConversion) {
                if (go != null) {
                    go.SetActive(true);
                } else {
                    Log.Important?.Error($"Missing object to enable on conversion: {gameObject.PathInSceneHierarchy()}", gameObject);
                }
            }
            ConvertEyesToWyrd().Forget();
        }

        [Button]
        public void DisableWyrdObjects() {
            foreach (var go in objectsToEnabledOnConversion) {
                if (go != null) {
                    go.SetActive(false);
                } else {
                    Log.Important?.Error($"Missing object to disable on conversion: {gameObject.PathInSceneHierarchy()}", gameObject);
                }
            }
            ConvertEyesFromWyrd().Forget();
        }
        
        // Eyes

        async UniTaskVoid ConvertEyesToWyrd() {
            if (_instancedMaterial != null || RendererWithEyes == null) {
                return;
            }
            
            ChangeMaterials();
            
            float lerp = 0f;
            var shaderNameId = Shader.PropertyToID(shaderName);
            
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            while (lerp < 1f && !token.IsCancellationRequested && this) {
                UpdateMaterial(shaderNameId, math.lerp(eyesDefaultEmission, eyesWyrdEmission, lerp));
                lerp += Time.deltaTime * lerpSpeed;
                if (!await AsyncUtil.DelayFrame(this)) {
                    return;
                }
            }

            if (!token.IsCancellationRequested && this) {
                UpdateMaterial(shaderNameId, eyesWyrdEmission);
            }
        }
        
        async UniTaskVoid ConvertEyesFromWyrd() {
            if (_instancedMaterial == null || RendererWithEyes == null) {
                return;
            }
            
            float lerp = 0f;
            var shaderNameId = Shader.PropertyToID(shaderName);
            
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            while (lerp < 1f && !token.IsCancellationRequested && this) {
                UpdateMaterial(shaderNameId, math.lerp(eyesWyrdEmission, eyesDefaultEmission, lerp));
                lerp += Time.deltaTime * lerpSpeed;
                if (!await AsyncUtil.DelayFrame(this)) {
                    return;
                }
            }

            if (!token.IsCancellationRequested && this) {
                UpdateMaterial(shaderNameId, eyesDefaultEmission);
                RestoreMaterials();
            }
        }
        
        void ChangeMaterials() {
            RendererWithEyes.EnsureInitialized();
            _instancedMaterial = RendererWithEyes.UseInstancedMaterial(_eyesIndex);
        }

        void RestoreMaterials() {
            RendererWithEyes.UseOriginalMaterial(_eyesIndex);
            _instancedMaterial = null;
        }

        void UpdateMaterial(int shaderNameId, float value) {
            _instancedMaterial.SetFloat(shaderNameId, value);
        }
    }
}
