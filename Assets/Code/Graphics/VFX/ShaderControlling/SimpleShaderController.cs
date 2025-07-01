using System;
using System.Collections.Generic;
using Awaken.Kandra;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace Awaken.TG.Graphics.VFX.ShaderControlling {
    public class SimpleShaderController : MonoBehaviour {

        enum RendererType {
            Renderer,
            Decal,
            UIGraphic,
            CustomPass,
            KandraRenderer
        }
    
        [Header("GetComponentsInChildren if left empty")] [SerializeField]
        Renderer[] _renderers = Array.Empty<Renderer>();
        [SerializeField] DecalProjector[] _decals = Array.Empty<DecalProjector>();
        [SerializeField] Graphic[] _graphics = Array.Empty<Graphic>();
        [SerializeField] CustomPassVolume[] _customPasses = Array.Empty<CustomPassVolume>();
        [SerializeField] KandraRenderer[] _kandraRenderers = Array.Empty<KandraRenderer>();

        public float Duration = 1.5f;
        [SerializeField] protected bool _useUnscaledTime;
        [SerializeField] string _floatName = "_DissolveTransition";
        [SerializeField] protected Ease _ease = Ease.OutBack;
        [SerializeField] protected bool _useCurve;
        [SerializeField] protected AnimationCurve _curve;
        [SerializeField] protected float _delay;
        [SerializeField] float _startValue;
        [SerializeField] protected float _targetValue;
        [SerializeField] RendererType _rendererType;
        [SerializeField] bool _tweenAtStart = true;
        [SerializeField] bool _startEachEnable;
        [SerializeField] bool _useStartValue = true;

        protected bool _initialized = false;
        protected List<Material> _materials = new();
        protected float _defaultValue;
        protected int _shaderPropertyId;
        protected int _delayTweenID;
        
        void Awake() {
            TryInitialize();
            if (_tweenAtStart) {
                StartEffect(true);
            }
        }

        void OnEnable() {
            TryInitialize();
            if (_startEachEnable) {
                StartEffect(true);
            }
        }

        void TryInitialize() {
            if (_initialized) {
                return;
            }
            _delayTweenID = GetInstanceID();
            if (_useStartValue) {
                _defaultValue = _startValue;
                ResetValues();
            }
            _shaderPropertyId = Shader.PropertyToID(_floatName);
            
            Prepare();
            _initialized = true;
        }

        void OnDestroy() {
            ResetValues();
            _materials.Clear();
        }

        void Prepare() {
            var otherComponent = GetComponent<SimpleShaderController>();
            if (!ReferenceEquals(otherComponent, null) && otherComponent._materials.Count > 0) {
                _materials = otherComponent._materials;
                return;
            }

            switch (_rendererType) {
                default:
                case RendererType.Renderer:
                    if (_renderers.Length == 0)
                        _renderers = GetComponentsInChildren<Renderer>();
                    for (int i = 0; i < _renderers.Length; i++) {
                        Renderer r = _renderers[i];
                        for (int j = 0; j < r.materials.Length; j++) {
                            Material rMat = r.materials[j];
                            var mat = new Material(rMat);
                            _materials.Add(mat);
                        }

                        r.materials = _materials.ToArray();
                    }

                    break;
                case RendererType.Decal:
                    for (int i = 0; i < _decals.Length; i++) {
                        DecalProjector decal = _decals[i];
                        var mat = new Material(decal.material);
                        _materials.Add(mat);
                        decal.material = mat;
                    }

                    break;
                case RendererType.UIGraphic:
                    for (int i = 0; i < _graphics.Length; i++) {
                        Graphic graphic = _graphics[i];
                        var mat = new Material(graphic.material);
                        _materials.Add(mat);
                        graphic.material = mat;
                    }

                    break;
                case RendererType.CustomPass: {
                    for (int i = 0; i < _customPasses.Length; i++) {
                        for (int j = 0; j < _customPasses[i].customPasses.Count; j++) {

                            var pass = _customPasses[i].customPasses[j];
                            if (pass is FullScreenCustomPass fullScreenPass) {
                                var mat = new Material(fullScreenPass.fullscreenPassMaterial);
                                _materials.Add(mat);
                                fullScreenPass.fullscreenPassMaterial = mat;
                            }
                        }
                    }

                    break;
                }
                case RendererType.KandraRenderer: {
                    if (_kandraRenderers.Length == 0)
                        _kandraRenderers = GetComponentsInChildren<KandraRenderer>();
                    for (int i = 0; i < _kandraRenderers.Length; i++) {
                        var kr = _kandraRenderers[i];
                        for (int j = 0; j < kr.rendererData.materialsInstances.Length; j++) {
                            Material rMat = kr.rendererData.materialsInstances[j];
                            _materials.Add(rMat);
                        }

                        //kr.rendererData.materialsInstances = _materials.ToArray();
                    }

                    break;
                }
            }

            if (!_useStartValue && _materials.Count > 0)
                _defaultValue = _materials[0].GetFloat(_shaderPropertyId);
        }

        public void StartEffect(bool positiveDirection) {
            ResetValues();
            if (_delay > 0) {
                DOTween.Kill(_delayTweenID);
                DOVirtual.DelayedCall(_delay, () => DoStartEffect(positiveDirection)).SetId(_delayTweenID);
            } else {
                DoStartEffect(positiveDirection);
            }
        }
        
        protected virtual void DoStartEffect(bool positiveDirection) { }
        protected virtual void ResetValues() { }
        
    }
}