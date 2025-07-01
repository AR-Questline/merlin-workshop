using System;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Templates.Specs {
    /// <summary> Component being cached in SpecRegistry. </summary>
    public abstract class SceneSpec : MonoBehaviour {
        [SerializeField, ReadOnly, HideInInspector] bool isCached;
        [SerializeField, ReadOnly, ShowIf(nameof(isCached))] SpecId sceneId;
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
        [NonSerialized] SpecId _playModeCached;
#endif

        [ShowInInspector, HideIf(nameof(isCached)), LabelText("")] string NotCachedInfo => isCached ? string.Empty : "Not cached spec id";

        public SpecId SceneId => isCached ? sceneId : GetSceneSpecId();

        public void CacheSceneId() {
            if (isCached) {
                return;
            }
            sceneId = GetSceneSpecId();
            isCached = true;
        }
        
        public void ForceSceneId(SpecId id) {
            sceneId = id;
            isCached = true;
        }

        SpecId GetSceneSpecId() {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                if (TryGetValidId(this, out var createdSceneId)) {
                    return createdSceneId;
                }
            }
#endif
#if UNITY_EDITOR && !SCENES_PROCESSED && !ADDRESSABLES_BUILD
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && _playModeCached.IsValid) {
                return _playModeCached;
            }
#endif
            var go = gameObject;
            var path = go.PathInSceneHierarchy(true);
            var shortHash = path.GetHashCode();
            Span<byte> hashBytes = stackalloc byte[8];
            BitConverter.TryWriteBytes(hashBytes, shortHash);
            var guidUlong = BitConverter.ToUInt64(hashBytes);

            var id = new SpecId(go.scene.name, 0, guidUlong, 0);
#if SCENES_PROCESSED
            Log.Critical?.Error($"No baked scene id for {go.name} in {go.scene.name}! Scene id: {id.FullId}");
#endif
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
            _playModeCached = id;
#endif
            return id;
        }

#if UNITY_EDITOR
        static bool TryGetValidId(SceneSpec spec, out SpecId id) {
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null || string.IsNullOrWhiteSpace(spec.gameObject.scene.name)) {
                id = default;
                return false;
            }

            var globalObjectId = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(spec.gameObject);
            id = new SpecId(spec.gameObject.scene.name, globalObjectId.targetPrefabId, globalObjectId.targetObjectId, 1);
            return id.IsValid;
        }
#endif
    }
}