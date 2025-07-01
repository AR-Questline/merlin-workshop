using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Timing;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.DayNightSystem {
    public class DayNightSystemFloatController : StartDependentView<GameRealTime> {
        
        [SerializeField] string materialProperty = "_Alpha";
        [SerializeField] AnimationCurve animationCurve;
        [SerializeField] bool global;
        [Header("Custom Pass")]
        [SerializeField] CustomPassVolume customPassVolume;
        [SerializeField] bool customPass;
        [SerializeField] bool heroWyrdNightEdgePass; 

        List<Material> _materials = new();
        Renderer _renderer;
        Material _customPassMaterial;
        Material _heroWyrdNightEdgeMaterial;

        static int s_propertyID = -1;

        float TimeOfDay => GenericTarget != null ? Target.WeatherTime.DayTime : 0;

        void Start() {
            if (!string.IsNullOrEmpty(materialProperty)) {
                s_propertyID = Shader.PropertyToID(materialProperty);
            } else {
                Debug.LogError("Material property name is empty!");
            }

            if (customPass || heroWyrdNightEdgePass) {
                if (customPassVolume != null) {
                    if (customPass) {
                        var fullScreenCustomPass = customPassVolume.customPasses.OfType<FullScreenCustomPass>().FirstOrDefault();
                        if (fullScreenCustomPass != null) {
                            _customPassMaterial = fullScreenCustomPass.fullscreenPassMaterial;
                        } else {
                            Debug.LogError("FullScreenCustomPass not found in CustomPassVolume custom passes.");
                        }
                    }

                    if (heroWyrdNightEdgePass) {
                        var heroWyrdCustomPass = customPassVolume.customPasses.OfType<HeroWyrdNightEdge>().FirstOrDefault();
                        if (heroWyrdCustomPass != null) {
                            _heroWyrdNightEdgeMaterial = heroWyrdCustomPass.fullScreenMaterial; 
                        } else {
                            Debug.LogError("HeroWyrdNightEdge not found in CustomPassVolume custom passes.");
                        }
                    }
                }
            } else {
                _renderer = GetComponent<Renderer>();
                if (_renderer != null) {
                    if (global) {
                        _renderer.GetSharedMaterials(_materials);
                    } else {
                        _renderer.GetMaterials(_materials);
                    }
                } else {
                    Debug.LogError("No renderer component");
                }
            }
        }

        void Update() {
            if (s_propertyID == -1) return; 

            float evaluatedValue = animationCurve.Evaluate(TimeOfDay);

            if (customPass && _customPassMaterial != null) {
                _customPassMaterial.SetFloat(s_propertyID, evaluatedValue);
                return; 
            }

            if (heroWyrdNightEdgePass && _heroWyrdNightEdgeMaterial != null) {
                _heroWyrdNightEdgeMaterial.SetFloat(s_propertyID, evaluatedValue);
                return;
            }

            if (_renderer != null && _materials != null) {
                foreach (var material in _materials) {
                    material.SetFloat(s_propertyID, evaluatedValue);
                }
            }
        }
    }
}
