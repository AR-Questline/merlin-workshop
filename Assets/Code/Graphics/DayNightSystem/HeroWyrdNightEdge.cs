using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.DayNightSystem {
    public class HeroWyrdNightEdge : CustomPass {
        static readonly int PosID = Shader.PropertyToID("_ObjectPosition");
        static readonly int RadiusID = Shader.PropertyToID("_Radius");
        static readonly int ThicknessID = Shader.PropertyToID("_Thickness");
        static readonly int MaskIntID = Shader.PropertyToID("_MaskIntensity");
        static readonly int ColorID = Shader.PropertyToID("_Color");
        
        Material _runtimeMaterial;
        
        public Material sourceMaterial; 
        public Transform targetObject;
        [ColorUsage(true, true)] public Color color = Color.yellow;
        [Range(0f, 10f)] public float maskIntensity = 3f;
        [Range(0f, 100f)] public float radius = 32f;
        [Range(0f, 1f)] public float thickness = 0.25f;
        
        public Material GetRuntimeMaterial() {
            if (_runtimeMaterial == null && sourceMaterial != null) {
                _runtimeMaterial = new Material(sourceMaterial);
                _runtimeMaterial.name = $"{sourceMaterial.name} (Runtime Copy)";
            }

            return _runtimeMaterial;
        }

        protected override void Execute(CustomPassContext ctx) {
            Material mat = GetRuntimeMaterial();
            if (mat is null) return;

            if (targetObject != null) {
                mat.SetVector(PosID, targetObject.position);
            }

            mat.SetFloat(RadiusID, radius);
            mat.SetFloat(ThicknessID, thickness);
            mat.SetFloat(MaskIntID, maskIntensity);
            mat.SetColor(ColorID, color);

            CoreUtils.DrawFullScreen(ctx.cmd, mat);
        }

        protected override void Cleanup() {
            CoreUtils.Destroy(_runtimeMaterial);
        }
    }
}