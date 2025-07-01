using System;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Tutorials.Steps.Composer;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Tutorials.Steps {
    public partial class TutorialStep : Element<Model>, ITutorialStep {
        public sealed override bool IsNotSaved => true;

        public string Key { get; }
        Action _onFinish;
        bool _isActive;
        Action _onPerform;

        public bool CanBePerformed => true;

        public TutorialStep(string key) {
            Key = key;
        }

        protected override void OnInitialize() {
            ModelUtils.DoForFirstModelOfType<TutorialMaster>(tutorial => tutorial.AddTutorialStep(this), this);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            World.Any<TutorialMaster>()?.RemoveTutorialStep(this);
        }
        
        public TutorialContext Perform(Action onFinish) {
            this._onFinish = onFinish;
            _isActive = true;
            _onPerform?.Invoke();
            return null;
        }

        public void Accompany(TutorialContext context) { }

        public void Finish() {
            if (_isActive) {
                _onFinish?.Invoke();
                _onFinish = null;
                _isActive = false;
                Discard();
            }
        }

        public void OnPerform(Action action) {
            if (_isActive) {
                action?.Invoke();
            } else {
                _onPerform += action;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public void AddOnFinishAction(Action action) {
            _onFinish += action;
        }
    }
}