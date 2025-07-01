using System.Collections.Generic;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Awaken.TG.Graphics.Saturation {
    public class SaturationStack {
        static readonly int GlobalSaturationAdjustmentID = Shader.PropertyToID("_GlobalSaturationAdjustment");
        public static SaturationStack Instance { get; private set; } = new();

        float _saturationAdjustment;
        readonly List<SaturationController> _stack = new();
        readonly SaturationReferences _references;

        public float Saturation => _saturationAdjustment + 1.0f;
        
        SaturationStack() {
            _references = CommonReferences.Get.SaturationReferences;
            _references.Init();
            RefreshSaturation();
        }
        
        public void Add(SaturationController controller) {
            _stack.Add(controller);
            RefreshSaturation();
        }

        public void Remove(SaturationController controller) {
            _stack.Remove(controller);
            RefreshSaturation();
        }

        public void OnSaturationChanged(SaturationController controller) {
            RefreshSaturation();
        }
        
        void RefreshSaturation() {
            float saturationAdjustment = _stack.Count == 0 ? 0.0f : _stack[^1].SaturationAdjustment;
            if (math.abs(_saturationAdjustment - saturationAdjustment) < 0.001f) {
                return;
            }
            _saturationAdjustment = saturationAdjustment;
            Shader.SetGlobalFloat(GlobalSaturationAdjustmentID, saturationAdjustment);
            _references.OnSaturationChanged(Saturation);
        }
        
        public static Color Saturate(Color color, float saturation) {
            float luminance = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
            return new Color(
                Mathf.Lerp(luminance, color.r, saturation),
                Mathf.Lerp(luminance, color.g, saturation),
                Mathf.Lerp(luminance, color.b, saturation),
                color.a 
            );
        }
    }
}