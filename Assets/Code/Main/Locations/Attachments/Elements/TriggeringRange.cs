using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.TG.Utility.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class TriggeringRange : Element<Location>, IRefreshedByAttachment<TriggeringRangeAttachment> {
        public override ushort TypeForSerialization => SavedModels.TriggeringRange;

        StoryBookmark _storyToRun;
        [Saved] bool _disabled;
        bool _triggered;
        bool _triggerOnEnter;
        bool _triggerOnExit;
        bool _onlyOnce;
        TriggerVolume _triggerVolume;
        
        // === Initialization
        protected override void OnInitialize() {
            ParentModel.ListenTo(TriggerVolume.Events.TriggerVolumeEntered, Enter, this);
            ParentModel.ListenTo(TriggerVolume.Events.TriggerVolumeExited, Exit, this);
            ParentModel.OnVisualLoaded(OnVisualLoaded);
        }

        public void InitFromAttachment(TriggeringRangeAttachment spec, bool isRestored) {
            _storyToRun = StoryBookmark.ToInitialChapter(spec.storyToRun);
            _triggerOnEnter = spec.triggerOnEnter;
            _triggerOnExit = spec.triggerOnExit;
            _onlyOnce = spec.onlyOnce;
        }

        void OnVisualLoaded(Transform transform) {
            _triggerVolume = transform.GetComponentInChildren<TriggerVolume>(true);
            if (_disabled && _triggerVolume != null) {
                _triggerVolume.gameObject.SetActive(false);
            }
        }
        
        // === Trigger
        void Enter(Collider _) {
            if (_disabled || _triggered) {
                return;
            }
            _triggered = true;            
            if (_triggerOnEnter) {
                Trigger();
            }
        }

        void Exit(Collider _) {
            if (_disabled || !_triggered) {
                return;
            }
            _triggered = false;            
            if (_triggerOnExit) {
                Trigger();
            }
        }

        void Trigger() {
            Hero hero = Hero.Current;
            if (_storyToRun != null) {
                Story.StartStory(StoryConfig.Base(_storyToRun, null));
            } else {
                ParentModel.DefaultAction(hero)?.StartInteraction(hero, ParentModel);
            }

            if (_onlyOnce) {
                _disabled = true;
                if (_triggerVolume != null) {
                    _triggerVolume.gameObject.SetActive(false);
                }
            }
        }
    }
}