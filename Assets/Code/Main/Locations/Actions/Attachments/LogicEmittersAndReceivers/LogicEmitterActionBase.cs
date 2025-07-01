using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    public abstract partial class LogicEmitterActionBase<T> : AbstractLocationAction, IRefreshedByAttachment<T> where T : LogicEmitterAttachmentBase {
        const float InactivityTime = 0.5f;
        protected static readonly int TriggerHash = Animator.StringToHash("Trigger");
        protected static readonly int ActiveHash = Animator.StringToHash("Active");
        
        protected T _attachment;
        protected Animator _animator;
        
        Location[] _locations;
        string _interactLabel;

        public override string DefaultActionName => !string.IsNullOrWhiteSpace(_interactLabel) ? _interactLabel : base.DefaultActionName;
        protected IEnumerable<Location> Locations => _locations ??= _attachment.Locations.ToArray();

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public LogicEmitterActionBase() {}
        
        public virtual void InitFromAttachment(T attachment, bool isRestored) {
            _attachment = attachment;
            _interactLabel = attachment.customInteractLabel.ToString();
        }
        
        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(OnVisualLoaded);
        }
        
        protected abstract bool IsActive();
        protected abstract void SendInteractEventsToLocation(Location location, bool active);
        protected virtual void OnLateInit() { }
        protected virtual void OnAnimatorSetup() => OnAnimatorUpdate(true);
        protected virtual void OnAnimatorUpdate(bool toggleState) { }
        
        protected virtual void OnVisualLoaded(Transform parentTransform) {
            _animator = parentTransform.GetComponentInChildren<Animator>();
            LateInit().Forget();
        }

        async UniTaskVoid LateInit() { //Necessary to wait because OnVisualLoaded is too fast for Static Locations
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }

            if (!Locations.Any()) {
                Log.Important?.Error($"Logic Emitter {_attachment.GetInstanceID()} has no Locations");
            }
            
            OnLateInit();
        }
        
        void SendInteractEvents(bool active) {
            foreach (var location in Locations) {
                SendInteractEventsToLocation(location, active);
            }
        }

        void PlayAudio(bool active) {
            if (active || _attachment.inactiveInteractionSound.IsNull) {
                if (!_attachment.interactionSound.IsNull) {
                    //RuntimeManager.PlayOneShotAttached(_attachment.interactionSound, _animator != null ? _animator.gameObject : ParentModel.ViewParent.gameObject, _attachment);
                }
            } else {
                if (!_attachment.inactiveInteractionSound.IsNull) {
                    //RuntimeManager.PlayOneShotAttached(_attachment.inactiveInteractionSound, _animator != null ? _animator.gameObject : ParentModel.ViewParent.gameObject, _attachment);
                }
            }
        }

        void Deactivate(bool active) {
            if (_attachment.onlyOnce && (active || _attachment.onlyOnceEvenIfInactive)) {
                ParentModel.SetInteractability(LocationInteractability.Inactive);
                return;
            }
            ParentModel.AddElement(new TemporarilyInactive(InactivityTime));
        }
        
        protected virtual void Interact(bool active, bool? forcedState = null) {
            if (StoryBookmark.ToInitialChapter(_attachment.StoryOnInteract, out var story) && active) {
                Story.StartStory(StoryConfig.Base(story, null));
            }
            
            if (!Locations.Any()) {
                if (story == null) {
                    Log.Important?.Error($"Logic Emitter {ParentModel.ID} has no Locations and no Story", ParentModel.ViewParent);
                }
                return;
            }
            
            SendInteractEvents(active);
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            ChangeState();
        }

        protected void UpdateAnimator(bool toggleState) {
            if (_animator == null) {
                return;
            }
            
            OnAnimatorUpdate(toggleState);
        }

        public void ChangeState(bool? forcedState = null) {
            bool active = forcedState.HasValue || IsActive();
            Interact(active, forcedState);
            UpdateAnimator(active);
            PlayAudio(active);
            Deactivate(active);
        }
    }
}