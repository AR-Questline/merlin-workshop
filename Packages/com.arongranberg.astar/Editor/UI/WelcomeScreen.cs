using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Compilation;
using UnityEditor.Scripting.ScriptCompilation;

namespace Pathfinding {
	internal class WelcomeScreen : UnityEditor.EditorWindow {
		[SerializeField]
		private VisualTreeAsset m_VisualTreeAsset = default;

		public bool isImportingSamples;
		private bool askedAboutQuitting;

		[InitializeOnLoadMethod]
		public static void TryCreate () {
        }

        public static void Create () {
        }

        public void CreateGUI () {
        }

        static string FirstSceneToLoad = "Recast3D";

        public void OnEnable()
        {
        }

        public void OnPostImportedSamples()
        {
        }

        void AnimateLogo(VisualElement logo)
        {
        }

        bool GetSamples(out UnityEditor.PackageManager.UI.Sample sample)
        {
            sample = default(UnityEditor.PackageManager.UI.Sample);
            return default;
        }

        private void ImportSamples()
        {
        }

        void OnAssemblyCompilationFinished(string assembly, CompilerMessage[] message)
        {
        }

        private void OpenDocumentation()
        {
        }

        private void OpenGetStarted()
        {
        }

        private void OpenChangelog()
        {
        }
    }
}
