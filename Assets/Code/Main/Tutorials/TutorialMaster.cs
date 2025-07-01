using System.Collections.Generic;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Tutorials.Steps;
using Awaken.TG.Main.Tutorials.TutorialPopups;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Main.Tutorials {
    public partial class TutorialMaster : Model {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        readonly Dictionary<string, List<ITutorialStep>> _uiSteps = new();

        public static bool DebugMode => SafeEditorPrefs.GetBool("debug.tutorial");
        static bool AutoReset => SafeEditorPrefs.GetBool("debug.reset.tutorial");
        static bool SkipTutorial => SafeEditorPrefs.GetBool("debug.skip.tutorial");
        public readonly List<TutorialText> tutorialTextBuffer = new();

        // === Initialization
        protected override void OnInitialize() {
            if (AutoReset) {
                ForgetAllKeys();
            }
            
            ModelUtils.DoForFirstModelOfType<Hero>(_ => {
                World.Only<ShowTutorials>().ListenTo(Setting.Events.SettingRefresh, InitSequences, this);
                InitSequences();
            }, this);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            TutorialSequence.ClearAll();
        }

        // === TutorialSteps
        public void AddTutorialStep(ITutorialStep step) {
            string key = step.Key.Capitalize();
            if (DebugMode) {
                Awaken.Utility.Debugging.Log.Critical?.Error($"TutorialMaster {key} - Add Step");
            }
            if (!_uiSteps.TryGetValue(key, out var queue)) {
                queue = new List<ITutorialStep>();
                _uiSteps.Add(key, queue);
            }
            queue.Add(step);
            this.Trigger(Events.StepAdded, key);
        }

        public void RemoveTutorialStep(ITutorialStep step) {
            string key = step.Key.Capitalize();
            if (_uiSteps.TryGetValue(key, out var queue)) {
                if (DebugMode) {
                    Awaken.Utility.Debugging.Log.Critical?.Error($"tutorialMaster {key} - Remove Step");
                }
                
                queue.Remove(step);
                if (queue.Count == 0) {
                    _uiSteps.Remove(key);
                }
            }
        }

        public new static class Events {
            public static readonly Event<TutorialMaster, string> StepAdded = new(nameof(StepAdded));
        }

        public static void Trigger(TutKeys trigger) {
            var step = new TutorialStep(TutorialKeys.FullKey(trigger));
            World.Only<TutorialMaster>().AddElement(step);
            step.OnPerform(() => step.Finish());
        }

        void ForgetAllKeys() {
            TutorialKeys.Clear();
        }
    }
}