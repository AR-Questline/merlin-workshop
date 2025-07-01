using System.Collections.Generic;
using System.Reflection;
using Awaken.TG.Debugging;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Editor.Assets {
    public class AddressablesLeaksWatcher : OdinEditorWindow {
        [MenuItem("TG/Addressables/Leaks Watcher")]
        static void OpenWindow() {
            GetWindow<AddressablesLeaksWatcher>().Show();
        }

        [ShowInInspector, ReadOnly, FoldoutGroup("Tracking", order: 1)]
        Dictionary<object, AsyncOperationHandle>.KeyCollection TrackedObjects => AddressablesInfo.Instance.TrackingData?.Keys;

        [ShowInInspector, ReadOnly, FoldoutGroup("Tracking", order: 1)]
        object[] TrackedAssets => AddressablesInfo.Instance.TrackedAssets;
        [ShowInInspector, ReadOnly, FoldoutGroup("Tracking", order: 1)]
        object[] NullAssets => AddressablesInfo.Instance.NullAssets;
        [ShowInInspector, ReadOnly, FoldoutGroup("Tracking", order: 1)]
        object[] NullGameObjectAssets => AddressablesInfo.Instance.NullGameObjectAssets;
        [ShowInInspector, ReadOnly, FoldoutGroup("Tracking", order: 1)]
        object[] OtherNullsAssets => AddressablesInfo.Instance.OtherNullsAssets;

        [ShowInInspector, PropertyOrder(2)] bool IsPostingDiagnostics {
            get {
                var resourcesManager = Addressables.ResourceManager;
                return resourcesManager.GetType()
                    .GetField("postProfilerEvents", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(resourcesManager) as bool? ??
                       false;
            }
        }
        
        /*[ShowInInspector, FoldoutGroup("Counts", order: 0)] int _activeOperationsCount;
        [ShowInInspector, FoldoutGroup("Counts", order: 0)] int _createdOperationsCount;
        [ShowInInspector, FoldoutGroup("Counts", order: 0)] int _destroyedOperationsCount;
        [ShowInInspector, FoldoutGroup("Counts", order: 0)] int _loadedOperationsCount;*/

        protected override void OnEnable() {
            base.OnEnable();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            InitializeDiagnosticTracking();
        }
        
        protected override void OnDisable() {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
            //Addressables.ResourceManager.UnregisterDiagnosticCallback(OnDiagnostics);
            base.OnDisable();
        }

        [Button, FoldoutGroup("Tracking", order: 1)]
        void LoadTrackingData() {
            AddressablesInfo.Instance.LoadTrackingData();
        }

        [Button, FoldoutGroup("Tracking", order: 1)]
        void BakeTracked() {
            AddressablesInfo.Instance.BakeTracked();
        }

        [Button, FoldoutGroup("Tracking", order: 1)]
        void ClearTracked() {
            AddressablesInfo.Instance.Clear();
        }

        /*void OnDiagnostics(ResourceManager.DiagnosticEventContext context) {
            if (context.Type == ResourceManager.DiagnosticEventType.AsyncOperationCreate) {
                ++_activeOperationsCount;
                ++_createdOperationsCount;
            } else if (context.Type == ResourceManager.DiagnosticEventType.AsyncOperationDestroy) {
                --_activeOperationsCount;
                ++_destroyedOperationsCount;
                BakeTracked();
            } else if (context.Type == ResourceManager.DiagnosticEventType.AsyncOperationComplete) {
                ++_loadedOperationsCount;
                BakeTracked();
            }
            Repaint();
        }*/

        void OnPlayModeStateChanged(PlayModeStateChange change) {
            if (change == PlayModeStateChange.EnteredPlayMode) {
                InitializeDiagnosticTracking();
            }
        }
        
        void InitializeDiagnosticTracking() {
            /*_activeOperationsCount = 0;
            _createdOperationsCount = 0;
            _destroyedOperationsCount = 0;
            _loadedOperationsCount = 0;*/
            
            var resourcesManager = Addressables.ResourceManager;
            resourcesManager.GetType()
                .GetField("postProfilerEvents", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(resourcesManager, true);
            //resourcesManager.RegisterDiagnosticCallback(OnDiagnostics);
        }
    }
}
