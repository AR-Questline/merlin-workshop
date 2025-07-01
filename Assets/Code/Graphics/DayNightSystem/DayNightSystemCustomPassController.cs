using System;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.Utility;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.DayNightSystem {
    [RequireComponent(typeof(CustomPassVolume))]
    public class DayNightSystemCustomPassController : StartDependentView<Hero> {
        
        [SerializeField]
        string materialProperty = "_Alpha";
        [SerializeField]
        AnimationCurve animationCurve;
        
        CustomPassVolume _customPassVolume;
        Material _customPassMaterial;
        static int s_propertyID;
        Tween _tween;
        
        void Start() {
            Init();
        }

        void Init() {
            _customPassVolume = gameObject.GetComponent<CustomPassVolume>();
            
            if (_customPassVolume != null) {
                var fullScreenCustomPass = _customPassVolume.customPasses.OfType<FullScreenCustomPass>().FirstOrDefault();
                if (fullScreenCustomPass != null) {
                    _customPassMaterial = fullScreenCustomPass.fullscreenPassMaterial;
                } else {
                    Debug.LogError("FullScreenCustomPass not found in CustomPassVolume custom passes.");
                }
            }
            s_propertyID = Shader.PropertyToID(materialProperty);
        }

        protected override void OnMount() {
            base.OnMount();
            Target.ListenTo(HeroWyrdNight.Events.StatusChanged, OnWyrdNightStatusChanged, this);
        }

        void OnWyrdNightStatusChanged(bool change) {
            if (change) {
                _tween.Kill();
                _tween = DOTween.To(() => _customPassMaterial.GetFloat(s_propertyID), x => _customPassMaterial.SetFloat(s_propertyID, x), 1, 3);
            } else {
                _tween.Kill();
                _tween = DOTween.To(() => _customPassMaterial.GetFloat(s_propertyID), x => _customPassMaterial.SetFloat(s_propertyID, x), 0, 3);
            }
        }
    }
}
