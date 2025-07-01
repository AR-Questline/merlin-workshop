using System;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
    class OverridesWizard : OdinEditorWindow {
        Action<string[], string> _onFinished;

        readonly BuildArgument[] _arguments = PrepareArguments();

#if !ADDRESSABLES_BUILD
        [InfoBox("Remember to manually do things below:" +
                 "\n1. Set ADDRESSABLES_BUILD define", InfoMessageType.Error)]
#endif
        [ShowInInspector, ListDrawerSettings(IsReadOnly = true, ShowFoldout = false, ShowPaging = false), HideReferenceObjectPicker, OnValueChanged(nameof(UpdateActiveArguments), true)]
        BuildArgument[] _availableArguments;

        [ShowInInspector, ListDrawerSettings(IsReadOnly = true, ShowFoldout = false, ShowPaging = false), HideReferenceObjectPicker, OnValueChanged(nameof(UpdateActiveArguments), true)]
        BuildDefine[] _defines;

        internal static void ShowForOverrides(Action<string[], string> onFinished) {
            var wizard = GetWindow<OverridesWizard>(true, "Build arguments overrides", true);
            wizard.UpdateActiveArguments();
            wizard._defines = PrepareDefines();
            wizard._onFinished = onFinished;
        }

        [Button("Cancel"), HorizontalGroup("Buttons", MarginLeft = 10, MarginRight = 10)]
        void OnCancelButton() {
            Close();
        }

        [Button("Build"), HorizontalGroup("Buttons", MarginLeft = 10, MarginRight = 10)]
        void OnBuildCreate() {
            var overrides = _arguments.Where(static a => a.Enabled).Select(static a => a.name).ToArray();
            var defines = _defines.Where(static d => d.enabled).Select(static d => d.name).ToArray();
            var extraDefines = BuildTools.ExtraDefinesPrefix + string.Join(",", defines);
            _onFinished.Invoke(overrides, extraDefines);
        }

        void UpdateActiveArguments() {
            _availableArguments = _arguments.Where(static a => a.Available).ToArray();
        }

        static BuildArgument[] PrepareArguments() {
            var buildAddressables = new BuildArgument() { name = "build_addressables", enabled = true };
            var actuallyBuild = new BuildArgument() { name = "actually_build", enabled = true };
#if !UNITY_GAMECORE && !UNITY_PS5
            var il2cpp = new BuildArgument { name = "il2cpp", enabled = true, dependsOn = new[] { actuallyBuild } };
#endif
            var debug = new BuildArgument { name = "debug", dependsOn = new[] { actuallyBuild } };
            return new[] {
#if UNITY_GAMECORE || UNITY_PS5
                new() { name = "submission", enabled = false },
#endif
#if UNITY_GAMECORE
                new() { name = "master", enabled = false },
#elif UNITY_PS5
                new() { name = "il2cpp_master", enabled = true },
#else
                il2cpp,
                new() { name = "il2cpp_master", enabled = true, dependsOn = new[] { il2cpp } },
#endif
                buildAddressables,
                new() { name = "process_hos_only", enabled = false, dependsOn = new[] { buildAddressables } },
                new() { name = "strip_unused_addressables", enabled = true, dependsOn = new[] { buildAddressables } },
                new() { name = "process_scenes_and_assets", enabled = true, dependsOn = new[] { buildAddressables } },
                new() { name = "update_ar_addressables", enabled = true, dependsOn = new[] { buildAddressables } },
                actuallyBuild,
                debug,
                new() { name = "deep_profiling", dependsOn = new[] { debug } },
                new() { name = "connect_profiler", dependsOn = new[] { debug } },
                new() { name = "script_debugging", dependsOn = new[] { debug } },
                new() { name = "wait_for_managed_debugger", dependsOn = new[] { debug } },
                new() { name = "with_pdb", dependsOn = new[] { actuallyBuild } },
                new() { name = "clean_build", dependsOn = new[] { actuallyBuild } },
                new() { name = "refresh_guid_cache" },
                new() { name = "shutdown_after" },
            };
        }

        static BuildDefine[] PrepareDefines() {
            return new BuildDefine[] {
                new() { name = "HLOD_DEBUGGING" },
                new() { name = "NPC_HISTORIAN" },
            };
        }

        [Serializable]
        class BuildArgument {
            [ShowInInspector, ReadOnly, HideLabel, HorizontalGroup("Argument")]
            public string name;
            [ShowInInspector, HideLabel, HorizontalGroup("Argument")]
            public bool enabled;

            [HideInInspector]
            public BuildArgument[] dependsOn = Array.Empty<BuildArgument>();

            public bool Enabled => Available && enabled;
            public bool Available => dependsOn.All(static d => d.Enabled);
        }

        [Serializable]
        class BuildDefine {
            [ShowInInspector, ReadOnly, HideLabel, HorizontalGroup("Define")]
            public string name;
            [ShowInInspector, HideLabel, HorizontalGroup("Define")]
            public bool enabled;
        }
    }
}
