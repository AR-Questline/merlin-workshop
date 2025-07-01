using System;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public class FadeOnSpawnBehaviour : MonoBehaviour {
        [SerializeField] string[] propertiesToFade = Array.Empty<string>();
        
        [SerializeField, InlineButton(nameof(StartFromMaterial), "From Material")] 
        float startValue = 0f;
        [SerializeField, InlineButton(nameof(EndFromMaterial), "From Material")] 
        float endValue = 1f;
        [SerializeField] 
        float duration = 1f;
        
        Material[] _ghostMaterials;
        int[] _ids;

        void Awake() {
            float progress = startValue;
            DOTween.To(() => progress, SetProgress, endValue, duration).SetEase(Ease.InQuad);
            
            _ids = propertiesToFade.Select(Shader.PropertyToID).ToArray();

            _ghostMaterials = transform
                              .GetComponentsInChildren<Renderer>()
                              .SelectMany(r => r.materials)
                              .Where(m => _ids.Any(m.HasProperty))
                              .ToArray();
            
            SetProgress(progress);
            
            void SetProgress(float v) {
                if (!this) return;
                progress = v;
                
                for (var i = 0; i < _ghostMaterials.Length; i++) {
                    Material material = _ghostMaterials[i];
                    
                    for (var index = 0; index < _ids.Length; index++) {
                        int id = _ids[index];
                        if (material.HasProperty(id)) {
                            material.SetFloat(id, progress);
                        }
                    }
                }
            }
        }

        void StartFromMaterial() {
            if (propertiesToFade.Length == 0) return;
            startValue = ValueFromMaterial(propertiesToFade[0]);
        }

        void EndFromMaterial() {
            if (propertiesToFade.Length == 0) return;
            endValue = ValueFromMaterial(propertiesToFade[0]);
        }

        float ValueFromMaterial(string propertyName) {
            _ids = propertiesToFade.Select(Shader.PropertyToID).ToArray();
            var materialWithProp = transform
                                   .GetComponentsInChildren<Renderer>()
                                   .SelectMany(r => r.sharedMaterials)
                                   .FirstOrDefault(m => _ids.Any(m.HasProperty));
            
            if (!materialWithProp) return 0;
            return materialWithProp.GetFloat(propertyName);
        }
    }
}