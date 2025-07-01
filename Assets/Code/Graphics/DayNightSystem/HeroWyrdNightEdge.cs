using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.DayNightSystem {
    [ExecuteAlways]
    public class HeroWyrdNightEdge : CustomPass {
        [Header("SHADER PARAMETERS")]
        public Material fullScreenMaterial;
        [SerializeField] Transform _targetObject;
        
        [SerializeField, Range(0f, 100f)] float _radius = 30f; 
        [SerializeField, Range(0f, 1f)] float _thickness = 0.98f;
        [SerializeField, Range(0f, 10f)] float _maskIntensity = 10f;
        [SerializeField, ColorUsage(true, true)] Color _color = Color.red;  

        protected override void Execute(CustomPassContext ctx) {
            if (_targetObject && fullScreenMaterial)
                fullScreenMaterial.SetVector("_ObjectPosition", _targetObject.position);
            
            fullScreenMaterial.SetFloat("_Radius", _radius);
            fullScreenMaterial.SetFloat("_Thickness", _thickness);
            fullScreenMaterial.SetFloat("_MaskIntensity", _maskIntensity);
            fullScreenMaterial.SetColor("_Color", _color);
            
            ctx.cmd.DrawProcedural(Matrix4x4.identity, fullScreenMaterial, 0, MeshTopology.Triangles, 3, 1);
        }
    }
}