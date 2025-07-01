using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Shaders
{
    public class TreeBillboardShaderGUI : StandardShaderGUI {
        MaterialProperty _billboardUVScale;
        MaterialProperty _billboardGeometryScale;
        MaterialProperty _billboardNormalSkew;
        MaterialProperty _windStrength;

        public override void FindProperties(MaterialProperty[] props) {
            base.FindProperties(props);
            _billboardGeometryScale = FindProperty("_GeometryScale", props);
            _billboardUVScale = FindProperty("_UVScale", props);
            _billboardNormalSkew = FindProperty("_NormalSkew", props);
            _windStrength = FindProperty("_WindStrength", props);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            base.OnGUI(materialEditor, props);
            
            GUILayout.Label("Billboard properties", EditorStyles.boldLabel);
            m_MaterialEditor.FloatProperty(_billboardGeometryScale, "Billboard Size");
            m_MaterialEditor.FloatProperty(_billboardUVScale, "Billboard UV Scale");
            m_MaterialEditor.FloatProperty(_billboardNormalSkew, "Billboard Normal Skew");
            m_MaterialEditor.FloatProperty(_windStrength, "Wind Strength");
        }
    }
}
