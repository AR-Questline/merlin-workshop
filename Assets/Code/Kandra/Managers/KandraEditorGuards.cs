using System.Collections.Generic;
using System.Diagnostics;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.Kandra.Managers {
    [Il2CppEagerStaticClassConstruction]
    public static class KandraEditorGuards {
#if UNITY_EDITOR
        static List<KandraTrisCullee> s_awakenCullees = new List<KandraTrisCullee>();
        static List<KandraTrisCullee> s_enabledCullees = new List<KandraTrisCullee>();
        static List<KandraTrisCuller> s_awakenCullers = new List<KandraTrisCuller>();
#endif

        [Conditional("UNITY_EDITOR")]
        public static void CulleeAwaken(KandraTrisCullee cullee) {
#if UNITY_EDITOR
            s_awakenCullees.Add(cullee);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void CulleeEnabled(KandraTrisCullee cullee) {
#if UNITY_EDITOR
            s_enabledCullees.Add(cullee);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void CanCulleeDisable(KandraTrisCullee cullee, ref bool canDisable) {
#if UNITY_EDITOR
            canDisable = s_enabledCullees.Contains(cullee);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void CanCulleeDestroy(KandraTrisCullee cullee, ref bool canDestroy) {
#if UNITY_EDITOR
            canDestroy = s_awakenCullees.Contains(cullee);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void CulleeDisabled(KandraTrisCullee cullee) {
#if UNITY_EDITOR
            s_enabledCullees.Remove(cullee);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void CulleeDestroyed(KandraTrisCullee cullee) {
#if UNITY_EDITOR
            s_awakenCullees.Remove(cullee);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void CullerAwaken(KandraTrisCuller culler) {
#if UNITY_EDITOR
            s_awakenCullers.Add(culler);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void CanCullerDestroy(KandraTrisCuller culler, ref bool canDestroy) {
#if UNITY_EDITOR
            canDestroy = s_awakenCullers.Contains(culler);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void CullerDestroyed(KandraTrisCuller culler) {
#if UNITY_EDITOR
            s_awakenCullers.Remove(culler);
#endif
        }

#if UNITY_EDITOR
        public static void EDITOR_ExitPlaymodeCleanup() {
            while (s_awakenCullers.Count > 0) {
                s_awakenCullers[0].OnDestroy();
            }

            while (s_enabledCullees.Count > 0) {
                s_enabledCullees[0].OnDisable();
            }

            while (s_awakenCullees.Count > 0) {
                s_awakenCullees[0].OnDestroy();
            }
        }
#endif
    }
}
