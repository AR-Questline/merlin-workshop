using System;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.Statues {
    [SelectionBase]
    [ExecuteAlways]
    public class Statue : MonoBehaviour {
        [SerializeField, OnValueChanged(nameof(RegenerateEditableModel))]
        GameObject body;

        [SerializeField, OnValueChanged(nameof(RegenerateEditableModel))]
        GameObject[] parts = Array.Empty<GameObject>();

        [SerializeField, OnValueChanged(nameof(RegenerateEditableModel))]
        Material material;

        [SerializeField, OnValueChanged(nameof(RegenerateEditableModel))]
        AnimationClip clip;

        [SerializeField, HideInInspector] int frame;
        [SerializeField, HideInInspector] SerializableGuid[] props = Array.Empty<SerializableGuid>();
#if UNITY_EDITOR
        [SerializeField] float screenRelativeTransitionHeight = 0.02f;
        GameObject _editableInstance;
        GameObject _staticInstance;
        bool _doNotDestroyBakedStaticInstance;
        public static event Action<Statue> OnRegenerateEditableModelRequest;
#endif
        int Frames => Mathf.RoundToInt(clip.frameRate * clip.length);

#if UNITY_EDITOR
        void OnEnable() {
            if (transform.GetComponentInParent<StatueRoot>() == null) {
                LogStatueShouldHaveStaticRootParent(this);
            }
            
            if (Application.isPlaying == false) 
            {
                RegenerateEditableModel();
            }
        }

        void OnDestroy() {
#if !SIMULATE_BUILD
            if (_doNotDestroyBakedStaticInstance == false && Application.isPlaying == false) {
                TryDestroyStaticInEditorInstance(_staticInstance);
            }
#endif
        }
#endif
        void RegenerateEditableModel() {
#if UNITY_EDITOR
            OnRegenerateEditableModelRequest?.Invoke(this);
#endif
        }

#if UNITY_EDITOR
        public static void TryDestroyStaticInEditorInstance(GameObject staticInstance) {
            if (staticInstance == null) {
                return;
            }

            var meshFilters = staticInstance.GetComponentsInChildren<MeshFilter>(true);
            foreach (var meshFilter in meshFilters) {
                if (meshFilter.sharedMesh != null) {
                    DestroyImmediate(meshFilter.sharedMesh, true);
                }
            }
            GameObjects.DestroySafely(staticInstance);
        }
        public static void LogStatueShouldHaveStaticRootParent(Statue statue) {
            Log.Important?.Error($"Statue {statue.name} does not have parent gameObject with {nameof(StatueRoot)} component. It will not be baked. Each statue should have a parent gameObject with {nameof(StatueRoot)} component. GameObject with {nameof(StatueRoot)} should contain all gameObjects with statue visuals under it (as transform children) {statue.gameObject.HierarchyPath()}", statue);
        }
        public readonly struct EditorAccess {
            public Statue Statue { get; }

            public int Frames => Statue.Frames;
            public ref GameObject Body => ref Statue.body;
            public ref GameObject[] Parts => ref Statue.parts;
            public ref Material Material => ref Statue.material;
            public ref AnimationClip Clip => ref Statue.clip;
            public ref int Frame => ref Statue.frame;
            public ref SerializableGuid[] Props => ref Statue.props;
            public ref GameObject EditableInstance => ref Statue._editableInstance;
            public ref GameObject StaticInstance => ref Statue._staticInstance;
            public float ScreenRelativeTransitionHeight => Statue.screenRelativeTransitionHeight;
            public ref bool DoNotDestroyBakedStaticInstance => ref Statue._doNotDestroyBakedStaticInstance;
            public Transform RootTransform => Statue.GetComponentInParent<StatueRoot>(true)?.transform;
            public EditorAccess(Statue statue) {
                Statue = statue;
            }
        }
#endif
    }
}