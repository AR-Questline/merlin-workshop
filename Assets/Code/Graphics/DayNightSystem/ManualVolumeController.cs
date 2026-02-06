using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Graphics.DayNightSystem {
    public class ManualVolumeController : StartDependentView<GameRealTime>, ITagged {
        [SerializeField, Tags(TagsCategory.Location)] string[] tags = Array.Empty<string>();
        [SerializeField] VolumeState[] states = Array.Empty<VolumeState>();
        [SerializeField] Volume volume;
        
        [ShowInInspector, ReadOnly] int _currentState;
        CancellationTokenSource _cts;
        
        public ICollection<string> Tags => tags;
        string UniqueID => $"{nameof(ManualVolumeController)}_{SceneName}_{string.Join("_", tags)}";
        string SceneName => gameObject.scene.name;
        static string ActiveSceneName => World.Services.TryGet<SceneService>()?.ActiveSceneRef?.Name ?? "Unknown";
        
        public static IEnumerable<ManualVolumeController> GetControllersWithTags(string[] tags) {
            bool foundAny = false;
            foreach (var view in World.Any<GameRealTime>().Views) {
                if (view is ManualVolumeController controller) {
                    if (TagUtils.HasRequiredTagsWithChecks(controller.Tags, tags)) {
                        foundAny = true;
                        yield return controller;
                    }
                }
            }
            if (!foundAny) {
                Log.Important?.Error($"No ManualVolumeController found on scene {ActiveSceneName} with tags: {string.Join(", ", tags)}");
            }
        }

        protected override void OnMount() {
            base.OnInitialize();
            int savedState = GetState();
            if (savedState < 0 || savedState >= states.Length) {
                Log.Important?.Error($"Invalid saved state ({savedState}). It's outside of array range 0-{states.Length - 1} for {UniqueID}. Resetting to 0.");
                _currentState = 0;
            } else {
                _currentState = savedState;
            }
            SetVolumeWeight(states[_currentState].targetWeight);
        }

        public void ChangeState(int state) {
            if (_currentState == state) {
                return;
            }
            if (state < 0 || state >= states.Length) {
                Log.Important?.Error($"Invalid state to set ({state}). It's outside of array range 0-{states.Length - 1} for {UniqueID}");
                return;
            }
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            LerpVolumeWeight(volume.weight, states[state].targetWeight, states[state].changeSpeed, states[state].easingTo.EnumAs<EasingType>()).Forget();
            SetState(state);
        }

        public void ChangeStateInstant(int state) {
            if (state < 0 || state >= states.Length) {
                Log.Important?.Error($"Invalid state to set ({state}). It's outside of array range 0-{states.Length - 1} for {UniqueID}");
                return;
            }
            if (_currentState == state) {
                if (_cts != null) {
                    _cts.Cancel();
                    _cts = null;
                    SetVolumeWeight(states[state].targetWeight);
                }
                return;
            }
            _cts?.Cancel();
            _cts = null;
            SetVolumeWeight(states[state].targetWeight);
            SetState(state);
        }

        void SetState(int state) {
            _currentState = state;
            World.Services.Get<GameplayMemory>().Context(nameof(ManualVolumeController)).Set<int>(UniqueID, state);
        }

        int GetState() {
            return World.Services.Get<GameplayMemory>().Context(nameof(ManualVolumeController)).Get<int>(UniqueID, 0);
        }
        
        async UniTaskVoid LerpVolumeWeight(float from, float to, float changeSpeed, EasingType easing) {
            float change = to - from;
            if (change == 0f) {
                return;
            }
            float duration = math.abs(change) / changeSpeed;
            float elapsed = 0f;
            while (true) {
                elapsed += Time.deltaTime / duration;
                if (elapsed > 1f) {
                    SetVolumeWeight(to);
                    _cts = null;
                    return;
                }
                SetVolumeWeight(from + change * easing.Calculate(elapsed));
                if (!await AsyncUtil.DelayFrame(this, 1, _cts.Token)) {
                    return;
                }
            }
        }
        
        void SetVolumeWeight(float value) {
            volume.weight = value;
        }
        
        protected override IBackgroundTask OnDiscard() {
            _cts?.Cancel();
            return base.OnDiscard();
        }

        [Serializable]
        public struct VolumeState {
            public float targetWeight;
            public float changeSpeed;
            [RichEnumExtends(typeof(EasingType))] public RichEnumReference easingTo;
        }
    }
}