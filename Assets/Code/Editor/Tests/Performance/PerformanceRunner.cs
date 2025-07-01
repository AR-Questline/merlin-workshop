using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Awaken.TG.Code.Editor.Tests.Performance {
    public static class PerformanceRunner {
        /// <summary>
        /// Runs all play mode tests.
        /// </summary>
        [MenuItem("TG/Tests/Run play mode tests", false, 5000)]
        public static void RunPlayModeTests() {
            var filter = new Filter {
                testMode = TestMode.PlayMode
            };
            RunTests(filter);
        }
        
        /// <summary>
        /// Runs play mode, rogue performance tests.
        /// </summary>
        [MenuItem("TG/Tests/Run rogue performance tests", false, 5001)]
        public static void RunRoguePerformanceTests() {
            var filter = new Filter {
                testMode = TestMode.PlayMode,
                categoryNames = new []{"Performance.Rogue"},
            };
            RunTests(filter);
        }
 
        static void RunTests(Filter filter) {
            var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            testRunnerApi.Execute(new ExecutionSettings(filter));
        }
    }
}