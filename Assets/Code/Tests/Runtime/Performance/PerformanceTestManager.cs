using Awaken.Tests.Performance.Preprocessing;
using Awaken.Tests.Performance.Profilers;
using Awaken.Tests.Performance.TestCases;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Slack;
using Cysharp.Threading.Tasks;
using UnityEngine;
using SceneReference = Awaken.TG.Assets.SceneReference;

namespace Awaken.Tests.Performance {
    public class PerformanceTestManager {
        const string SlackChannelName = "performance";
        readonly IPerformancePreprocessorVariant[][] _variations;
        readonly IPerformanceTestCase[] _tests;
        readonly IPerformanceMatrix[] _profilers;

        readonly SlackMessenger _slackMessenger;

        public PerformanceTestManager(IPerformancePreprocessorVariant[][] variants, IPerformanceTestCase[] tests, IPerformanceMatrix[] profilers) {
            _variations = M.CartesianProduct(variants);
            _tests = tests;
            _profilers = profilers;
            _slackMessenger = new SlackMessenger(SlackChannelName);
        }

        public async UniTask Run() {
            DebugReferences.ImmediateStory = true;
            
            _slackMessenger?.StartThread($"{_slackMessenger.GetMachineName()} starts performance tests...");
            
            foreach (var test in _tests) {
                foreach (var variation in _variations) {
                    await LoadScene(test.Scene);
                    await UniTask.Delay(5000);
                    await Run(variation, test);
                }
            }
            DebugReferences.ImmediateStory = false;
            
            if (_slackMessenger != null) {
                await _slackMessenger.PostMessage("Performance tests have finished!");
            }
        }

        async UniTask LoadScene(SceneReference scene) {
            bool loadingScene = true;
            var listener = World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.AfterSceneStoriesExecuted, null, data => {
                if (data.SceneReference == scene) {
                    loadingScene = false;
                }
            });
            ScenePreloader.StartNewGame(scene);
            await UniTask.WaitWhile(() => loadingScene);
            World.EventSystem.RemoveListener(listener);
        }

        async UniTask Run(IPerformancePreprocessorVariant[] preprocessors, IPerformanceTestCase test) {
            var runner = new GameObject("TestRunner").AddComponent<PerformanceTestRunner>();
            await runner.Run(preprocessors, test, _profilers, _slackMessenger);
        }
    }
}