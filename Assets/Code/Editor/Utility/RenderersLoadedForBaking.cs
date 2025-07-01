using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.CommonInterfaces;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Utility {
    public readonly struct RenderersLoadedForBaking : IDisposable {
        readonly List<IWithOcclusionCullingTarget.IRevertOcclusion> _revertTargets;

        public RenderersLoadedForBaking(Scene scene) {
            _revertTargets = new List<IWithOcclusionCullingTarget.IRevertOcclusion>();
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots) {
                if (!root.activeSelf) {
                    continue;
                }
                var targets = root.GetComponentsInChildren<IWithOcclusionCullingTarget>();
                _revertTargets.AddRange(targets.Select(static t => t.EnterOcclusionCulling()));
            }
        }

        public void Dispose() {
            foreach (var revertTarget in _revertTargets) {
                revertTarget.Revert();
            }
            _revertTargets.Clear();
        }
    }
}