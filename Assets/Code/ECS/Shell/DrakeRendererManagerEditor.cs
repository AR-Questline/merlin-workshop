using System;
using System.Collections.Generic;
using Awaken.ECS.DrakeRenderer.Authoring;
using UnityEngine;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public static class DrakeRendererManagerEditor {
        public static readonly HashSet<DrakeMeshRenderer> DrakeMeshRenderers = new HashSet<DrakeMeshRenderer>();
        public static readonly HashSet<DrakeLodGroup> DrakeLodGroups = new HashSet<DrakeLodGroup>();
        public static event Action<HashSet<DrakeLodGroupTransformPair>> AddedDrakeLodGroups;
        public static event Action<HashSet<DrakeLodGroupTransformPair>> RemovedDrakeLodGroups;
        public static event Action<HashSet<DrakeMeshRendererTransformPair>> AddedDrakeMeshRenderers;
        public static event Action<HashSet<DrakeMeshRendererTransformPair>> RemovedDrakeMeshRenderer;

        public static void EDITOR_RuntimeReset() {
            throw new NotImplementedException();
        }

        public static void AfterBootstrap() {
            throw new NotImplementedException();
        }

        public static bool IsPartOfEditingPrefab(GameObject gameObject, GameObject editingPrefab) {
            throw new NotImplementedException();
        }
    }
}