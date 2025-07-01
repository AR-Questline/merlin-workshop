using Awaken.Kandra.VFXs;
using Awaken.TG.Utility;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.Kandra.Debugging {
    public partial class KandraRendererDebugger {
        bool _expandedVfx;
        Vector2 _vfxScrollPosition;

        void VfxDebug() {
            _expandedVfx = TGGUILayout.Foldout(_expandedVfx, "VFXs:");
            if (!_expandedVfx) {
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(Indent);
            GUILayout.BeginVertical();

            _vfxScrollPosition = GUILayout.BeginScrollView(_vfxScrollPosition, GUILayout.ExpandHeight(false));

            var access = KandraVfxHelper.EditorAccess.Get();
            foreach (var (hash, data) in access.IndicesBuffers) {
                KandraMesh mesh = null;
                if (Resources.InstanceIDIsValid(hash)) {
                    mesh = (KandraMesh)Resources.InstanceIDToObject(hash);
                }

                GUILayout.BeginHorizontal();

#if UNITY_EDITOR
                UnityEditor.EditorGUILayout.ObjectField(mesh, typeof(KandraMesh), false, GUILayout.Width(200));
#else
                GUILayout.Label(mesh ? mesh.name : "null", GUILayout.Width(200));
#endif
                GUILayout.Label($"Ref count: {data.refCount}", GUILayout.Width(100));
                GUILayout.Label(M.HumanReadableBytes(data.buffer.stride * data.buffer.count));

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}
