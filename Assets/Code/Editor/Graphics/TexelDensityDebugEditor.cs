using Awaken.TG.Graphics;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics {
    [CustomEditor(typeof(TexelDensityDebug))]
    public class TexelDensityDebugEditor : OdinEditor {
        
        private TexelDensityDebug _tdd;
        private readonly GUIStyle _style = new GUIStyle();

        protected override void OnEnable() {
            _style.fontStyle = FontStyle.Bold;
            _style.normal.textColor = Color.white;
            _tdd = (TexelDensityDebug) target;
            if(!_tdd.initialized){
                _tdd.initialized = true;
            }
        }

        void OnSceneGUI(){
            if (_tdd.measureTool) {
                //Lables and handles:
                var distance = Vector3.Distance(_tdd.pointStart, _tdd.pointEnd);
                var scalePerPixel = Mathf.RoundToInt(distance * (_tdd.target / _tdd.scale));
                var middlePoint = (_tdd.pointStart + _tdd.pointEnd)/2;
                var tiling = Mathf.RoundToInt(scalePerPixel / _tdd.target);
                Handles.Label( middlePoint,  "Density  : " + _tdd.target + "px/m" + "\nDistance: " + distance + "\nScale/px: " + scalePerPixel + "px" + "\nTiling      : " + tiling, _style);

                //Allow adjustment undo:
                _tdd.pointStart = Handles.PositionHandle(_tdd.pointStart, Quaternion.identity);
                _tdd.pointEnd = Handles.PositionHandle(_tdd.pointEnd, Quaternion.identity);
            }
        }
    }
}
