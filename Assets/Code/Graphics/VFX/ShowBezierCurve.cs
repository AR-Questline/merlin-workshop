using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX
{
    public class ShowBezierCurve : MonoBehaviour {
        private bool _display = true;
        
        [FoldoutGroup("Display", true)][Button("Display")]
        private void DefaultSizedButton()
        {
            this._display = !this._display;
        }
        [FoldoutGroup("Display", true)] public float gizmoRadius = 0.2f;
        [FoldoutGroup("Display", true)] public float lineThickness = 10.0f;
        [FoldoutGroup("Display", true)] public Color color = Color.green;
        [FoldoutGroup("Positions", true)] public GameObject bezierA;
        [FoldoutGroup("Positions", true)] public GameObject bezierB;
        [FoldoutGroup("Positions", true)] public GameObject bezierC;
        [FoldoutGroup("Positions", true)] public GameObject bezierD;

        private void OnDrawGizmos() {
#if UNITY_EDITOR
            if(_display){
                Gizmos.color = color;
                var bezierAPos = bezierA.transform.position;
                var bezierBPos = bezierB.transform.position;
                var bezierCPos = bezierC.transform.position;
                var bezierDPos = bezierD.transform.position;

                Gizmos.DrawSphere(bezierAPos, gizmoRadius);
                Gizmos.DrawSphere(bezierBPos, gizmoRadius);
                Gizmos.DrawSphere(bezierCPos, gizmoRadius);
                Gizmos.DrawSphere(bezierDPos, gizmoRadius);

                Gizmos.DrawLine(bezierAPos, bezierBPos);
                Gizmos.DrawLine(bezierDPos, bezierCPos);

                Handles.DrawBezier(bezierAPos, bezierDPos, bezierBPos, bezierCPos, color, null, lineThickness);
            }
#endif
        }
    }
}
