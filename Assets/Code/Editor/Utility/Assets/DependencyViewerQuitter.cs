using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.Search;

namespace Awaken.TG.Editor.Plugins {
    /// <summary>
    /// Unity Search Extension crashes Unity Editor on compile while DependencyViewer is open. This circumvents the problem.
    /// </summary>
    [InitializeOnLoad]
    public static class DependencyViewerQuitter {
        const string DependencyViewerLaunched = "OnCompilation: Clean DependencyViewer";

        static DependencyViewerQuitter() {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            EditorApplication.wantsToQuit += OnQuitting;
            EditorApplication.update += Update;
        }

        static void Update() {
            if (!SessionState.GetBool("FirstInitDone", false)) {
                // if (EditorWindow.HasOpenInstances<DependencyViewer>()) {
                //     CompilationPipeline.RequestScriptCompilation();
                // } else {
                //     OnCompilationFinished(null);
                // }

                SessionState.SetBool("FirstInitDone", true);
            }
            EditorApplication.update -= Update;
        }

        static bool OnQuitting() {
            OnCompilationStarted(null);
            return true;
        }

        static void OnCompilationFinished(object obj) {
            if (EditorPrefs.GetBool(DependencyViewerLaunched)) {
                // EditorWindow.CreateWindow<DependencyViewer>();
            }
        }

        static void OnCompilationStarted(object obj) {
            EditorPrefs.SetBool(DependencyViewerLaunched, false);

            // while (EditorWindow.HasOpenInstances<DependencyViewer>()) {
            //     EditorWindow.GetWindow<DependencyViewer>().Close();
            //     EditorPrefs.SetBool(DependencyViewerLaunched, true);
            // }
        }
    }
}