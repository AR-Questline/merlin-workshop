using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Awaken.TG.Main.Grounds;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Graphics {
    public class SnapSelectedToGroundEditor : OdinEditorWindow {

        [HorizontalGroup("Options")]
        [BoxGroup("Options/Scale"), LabelWidth (100)] public bool changeScale;
        [BoxGroup("Options/Scale"), LabelWidth (80)] public float scaleMin = 1.0f;
        [BoxGroup("Options/Scale"), LabelWidth (80)] public float scaleMax = 1.0f;
        [BoxGroup("Options/Options"), LabelWidth (100)] public bool align;
        [BoxGroup("Options/Options"), LabelWidth (100)] public bool randomRotation;
        
        [Button]
        void SnapToGround() {
            SnapTo(true);
        }
        [Button]
        void SnapToCollider() {
            SnapTo(false);
        }

        [MenuItem("ArtTools/Snap Selected To Ground")]
        static void OpenWindow() {
            GetWindow<SnapSelectedToGroundEditor>().Show();
        }

        public void SnapTo(bool ground) {
            foreach (var transform in Selection.transforms) {
                Undo.RecordObject(transform, "Snap Selected To Ground");
                
                Vector3 position;
                if (ground) {
                    position = Ground.SnapToGround(transform.position, transform);
                } else {
                    position = transform.position;
                    position.y = Ground.HeightAt(transform.position, ignoreRoot: transform, raycastMask: (RenderLayers.Mask.Walkable | RenderLayers.Mask.Terrain | RenderLayers.Mask.Objects));
                }
                transform.position = position;
                
                if (align) {
                    var transformUp = transform.up;

                    RaycastHit hit;
                    LayerMask layerMask = (RenderLayers.Mask.Walkable | RenderLayers.Mask.Terrain | RenderLayers.Mask.Objects);
                    Physics.Raycast(position + Vector3.up, Vector3.down, out hit, 100f, layerMask);
                    Vector3 hitNormal = hit.normal;
                    Log.Important?.Info(hitNormal.ToString());
                    var rotateAbout = Quaternion.FromToRotation(transformUp, hitNormal);
                    transform.rotation = rotateAbout * transform.rotation;
                }
                
                if (randomRotation) {
                    transform.Rotate(Vector3.up, Random.Range(0f, 360f));
                }
                
                if (changeScale) {
                    float newScale = Random.Range(scaleMin, scaleMax);
                    transform.localScale = new Vector3(newScale,newScale,newScale);
                }
            }
        }
    }
}
