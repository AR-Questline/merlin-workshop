using System.Collections;
using System.Collections.Generic;
using Awaken.Tests.Performance.Preprocessing;
using Awaken.Tests.Performance.Profilers;
using Awaken.Tests.Performance.TestCases;
using Awaken.TG.Assets;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.Tests.Performance {
    public class PerformanceTestWindow : UGUIWindowDisplay<PerformanceTestWindow> {
        ARAsyncOperationHandle<PerformanceTestRegistry> _registryHandle;
        PerformanceTestRegistry _registry;

        BitArray _preprocessorToggles;
        BitArray[] _preprocessorVariantsToggles;
        BitArray _testCaseToggles;
        BitArray _profilerToggles;

        Vector2 _preprocessorScroll;
        Vector2 _testCaseScroll;
        Vector2 _profilerScroll;
        
        PerformanceTestRunner _runner;
        
        void Start() {
            _registryHandle = PerformanceTestRegistry.Load();
        }

        void OnDestroy() {
            _registryHandle.Release();
            _registryHandle = default;
            _registry = null;
        }

        protected override void DrawWindow() {
            if (!_registryHandle.IsValid()) {
                GUILayout.Label("Cannot find test registry");
                return;
            }

            if (_registry is null) {
                if (!_registryHandle.IsDone) {
                    GUILayout.Label("Loading test registry...");
                    return;
                }
                
                _registry = _registryHandle.Result;
                if (_registry == null) {
                    _registryHandle.Release();
                    _registryHandle = default;
                    return;
                }
                
                _registry.Init();
                _preprocessorToggles = new BitArray(_registry.Preprocessors.Count);
                _preprocessorVariantsToggles = new BitArray[_registry.Preprocessors.Count];
                _testCaseToggles = new BitArray(_registry.TestCases.Count);
                _profilerToggles = new BitArray(_registry.Matrices.Count);
            }
            
            GUILayout.BeginHorizontal();
            DrawPreprocessors();
            DrawTestCases();
            DrawProfilers();
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Run")) {
                RunTests().Forget();
            }
            
            if (GUILayout.Button("Save as Preset")) {
                Log.Important?.Info(AutomatedPerformanceTest.ToCommand(GetPreprocessors(), GetPreprocessorVariants(), GetTestCases(), GetMatrices()));
            }
        }

        void DrawPreprocessors() {
            GUILayout.BeginVertical();
            _preprocessorScroll = GUILayout.BeginScrollView(_preprocessorScroll);
            for (int i=0; i<_registry.Preprocessors.Count; i++) {
                _preprocessorToggles[i] = GUILayout.Toggle(_preprocessorToggles[i], _registry.Preprocessors[i].Name);
                if (_preprocessorToggles[i]) {
                    _preprocessorVariantsToggles[i] ??= new BitArray(_registry.Preprocessors[i].Variants.Count);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUILayout.BeginVertical();
                    for (int j = 0; j < _preprocessorVariantsToggles[i].Length; j++) {
                        _preprocessorVariantsToggles[i][j] = GUILayout.Toggle(_preprocessorVariantsToggles[i][j], _registry.Preprocessors[i].Variants[j].Name);
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        void DrawTestCases() {
            GUILayout.BeginVertical();
            _testCaseScroll = GUILayout.BeginScrollView(_testCaseScroll);
            for (int i = 0; i < _registry.TestCases.Count; i++) {
                _testCaseToggles[i] = GUILayout.Toggle(_testCaseToggles[i], _registry.TestCases[i].Name);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        void DrawProfilers() {
            GUILayout.BeginVertical();
            _profilerScroll = GUILayout.BeginScrollView(_profilerScroll);
            for (int i=0; i<_registry.Matrices.Count; i++) {
                _profilerToggles[i] = GUILayout.Toggle(_profilerToggles[i], _registry.Matrices[i].Name);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        async UniTaskVoid RunTests() {
            var manager = new PerformanceTestManager(GetPreprocessorVariants(), GetTestCases(), GetMatrices());
            Close();
            MarvinMode.HideView();
            await manager.Run();
            MarvinMode.ShowView();
        }


        IPerformancePreprocessor[] GetPreprocessors() {
            int preprocessorsCount = 0;
            for (int i = 0; i < _preprocessorToggles.Length; i++) {
                if (_preprocessorToggles[i]) {
                    int variants = _preprocessorVariantsToggles[i].CountOnes();
                    if (variants > 0) {
                        preprocessorsCount++;
                    }
                }
            }

            var preprocessors = new IPerformancePreprocessor[preprocessorsCount];
            int preprocessorIndex = 0;

            for (int i = 0; i < _preprocessorToggles.Length; i++) {
                if (_preprocessorToggles[i]) {
                    int variants = _preprocessorVariantsToggles[i].CountOnes();
                    if (variants > 0) {
                        preprocessors[preprocessorIndex++] = _registry.Preprocessors[i];
                    }
                }
            }
            
            return preprocessors;
        }

        IPerformancePreprocessorVariant[][] GetPreprocessorVariants() {
            int preprocessorsCount = 0;
            for (int i = 0; i < _preprocessorToggles.Length; i++) {
                if (_preprocessorToggles[i]) {
                    int variants = _preprocessorVariantsToggles[i].CountOnes();
                    if (variants > 0) {
                        preprocessorsCount++;
                    }
                }
            }
            
            var preprocessors = new IPerformancePreprocessorVariant[preprocessorsCount][];
            int preprocessorIndex = 0;

            for (int i = 0; i < _preprocessorToggles.Length; i++) {
                if (_preprocessorToggles[i]) {
                    int variantsCount = _preprocessorVariantsToggles[i].CountOnes();
                    if (variantsCount > 0) {
                        var variants = new IPerformancePreprocessorVariant[variantsCount];
                        int variantIndex = 0;
                        for (int j = 0; j < _preprocessorVariantsToggles[i].Length; j++) {
                            if (_preprocessorVariantsToggles[i][j]) {
                                variants[variantIndex++] = _registry.Preprocessors[i].Variants[j];
                            }
                        }
                        preprocessors[preprocessorIndex++] = variants;
                    }
                }
            }

            return preprocessors;
        }

        IPerformanceTestCase[] GetTestCases() {
            var testCases = new IPerformanceTestCase[_testCaseToggles.CountOnes()];
            int index = 0;
            for (int i = 0; i < _testCaseToggles.Length; i++) {
                if (_testCaseToggles[i]) {
                    testCases[index++] = _registry.TestCases[i];
                }
            }
            return testCases;
        }
        
        IPerformanceMatrix[] GetMatrices() {
            var profilers = new IPerformanceMatrix[_profilerToggles.CountOnes()];
            int index = 0;
            for (int i = 0; i < _profilerToggles.Length; i++) {
                if (_profilerToggles[i]) {
                    profilers[index++] = _registry.Matrices[i];
                }
            }
            return profilers;
        }
    }

    internal static class PerformanceTestWindowMarvin {
        [StaticMarvinButton(state: nameof(IsPerformanceTestShown))]
        static void ShowPerformanceTest() {
            PerformanceTestWindow.Toggle(UGUIWindowUtils.WindowPosition.TopLeft);
        }
            
        static bool IsPerformanceTestShown() {
            return PerformanceTestWindow.IsShown;
        }
    }
}