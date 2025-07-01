using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Graphics {
    [ExecuteAlways]
    public class TexelDensityDebug : MonoBehaviour {
        [BoxGroup("Debug")] public float target = 512;
        [BoxGroup("Debug")] public float scale = 3.9f;
        [BoxGroup("Debug")] public Shader shader;

        [BoxGroup("Measure tool")] public Vector3 pointStart = new Vector3(1, 0, 0);
        [BoxGroup("Measure tool")] public Vector3 pointEnd = new Vector3(-1, 0, 0);
        [BoxGroup("Measure tool")] public float pointsSize = 0.5f;
        [BoxGroup("Measure tool")] public Color pointsColor = Color.yellow;

        [HideInInspector] public bool initialized;
        [HideInInspector] public bool measureTool;

        [BoxGroup("Debug")][InfoBox("128px/1m = 1,28px/cm [strategic camera]\n 256px/1m = 2,56px/cm [strategic camera]\n 512px/1m = 5,12px/cm [third person camera]\n" +
                                    "1024px/1m = 10,24px/cm [first person camera]\n2048px/1m = 20,48px/cm [closeup/trailer]\n4096px/1m = 40,96px/cm [closeup/trailer]\n\nRUN IN PLAY MODE!")]
        [Button]
        void DebugView() {
            //FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r => r.materials).ForEach(m => m.shader = shader);
        }
        [BoxGroup("Meansure tool")][InfoBox("Enable Gizmos to see measure tool!\nTiling value = meters * px per meter / texture resolution")]
        [Button]
        void MeasureTool() {
            measureTool = !measureTool;
        }

        void Start() {
            shader = Shader.Find("TG/ScreenSpace/TexelDensityViewer");
        }

        #if UNITY_EDITOR
        void OnEnable() {
            Tools.hidden = false;
        }

        void OnDrawGizmosSelected() {
            if (measureTool) {
                Tools.hidden = true;
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(pointStart, pointsSize);
                Gizmos.DrawSphere(pointEnd, pointsSize);
                Gizmos.DrawLine(pointStart, pointEnd);
            }
        }

        void OnDrawGizmos() {
            if (measureTool) {
                Gizmos.color = pointsColor;
                Gizmos.DrawSphere(pointStart, pointsSize);
                Gizmos.DrawSphere(pointEnd, pointsSize);
                Gizmos.DrawLine(pointStart, pointEnd);
            }
        }
        #endif
    }
}
