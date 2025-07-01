using Awaken.Utility;
using Awaken.Utility.Maths;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Graphics.ScriptedEvents {
    [ExecuteInEditMode]
    public class ScriptedEventEditMode : MonoBehaviour {
#if UNITY_EDITOR
        const HideFlags SpawnedHideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.NotEditable;
        
        ScriptedEvent _scriptedEvent;

        Matrix4x4 _editorPreviousMatrix;
        GameObject _editorSpawnedPrefab;
        GameObject _editorSpawnedInstance;
        
        ScriptedEvent ScriptedEvent => _scriptedEvent ??= GetComponent<ScriptedEvent>();
        
        void LateUpdate() {
            if (PlatformUtils.IsPlaying) {
                RemoveSpawnedInstance();
                return;
            }
            var accesor = ScriptedEvent.EditorAccessor;
            if (accesor.Asset is not { IsSet: true }) {
                RemoveSpawnedInstance();
                return;
            }
            var prefab = accesor.Asset.EditorLoad<GameObject>();
            if (!prefab) {
                RemoveSpawnedInstance();
                return;
            }
            var myTransform = transform;
            if (_editorSpawnedPrefab == prefab) {
                if (_editorPreviousMatrix != myTransform.localToWorldMatrix) {
                    ResetSpawnedTransform();
                }
            } else {
                RemoveSpawnedInstance();
                _editorSpawnedPrefab = prefab;
                _editorSpawnedInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, myTransform);
                PrefabUtility.GetPrefabInstanceHandle(_editorSpawnedInstance).hideFlags = SpawnedHideFlags;
                ResetSpawnedTransform();
            }

            void RemoveSpawnedInstance() {
                if (_editorSpawnedInstance) {
                    DestroyImmediate(_editorSpawnedInstance);
                }

                _editorSpawnedPrefab = null;
                _editorSpawnedInstance = null;
            }
            
            void ResetSpawnedTransform() {
                _editorPreviousMatrix = myTransform.localToWorldMatrix;
                var spawnedTransform = _editorSpawnedInstance.transform;
                spawnedTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                spawnedTransform.localScale = myTransform.lossyScale.Invert();
            }
        }
#endif
    }
}