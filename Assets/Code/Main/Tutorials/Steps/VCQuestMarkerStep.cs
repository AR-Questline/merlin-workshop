using System;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Tutorials.Steps.Composer;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Tutorials.Steps {
    public class VCQuestMarkerStep : ViewComponent<Location>, ITutorialStep {
        public string key;
        VQuest3DMarker _marker;
        public string Key => key;
        TutorialMaster TutorialMaster => World.Only<TutorialMaster>();

        public bool CanBePerformed => true;

        protected override void OnAttach() {
            ModelUtils.DoForFirstModelOfType<TutorialMaster>(_ => TutorialMaster.AddTutorialStep(this), this);
        }

        protected override void OnDestroy() {
            if (!World.HasAny<TutorialMaster>()) return;
            TutorialMaster.RemoveTutorialStep(this);
            base.OnDestroy();
        }
    
        public TutorialContext Perform(Action onFinish) {
            _marker = World.SpawnView<VQuest3DMarker>(Target);
            ModelUtils.DoForFirstModelOfType<Quest>(() => {
                _marker.Discard();
                onFinish?.Invoke();
            }, this);

            return null;
        }

        public void Accompany(TutorialContext context) { }
    }
}
