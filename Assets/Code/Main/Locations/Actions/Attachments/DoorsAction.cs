using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using FMODUnity;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    public partial class DoorsAction : AbstractLocationAction, IRefreshedByAttachment<DoorsAttachment>, ILogicReceiverElement {
        public override ushort TypeForSerialization => SavedModels.DoorsAction;

        static readonly int Closed = Animator.StringToHash("Closed");
        static readonly int OpenLeft = Animator.StringToHash("OpenLeft");
        static readonly int OpenRight = Animator.StringToHash("OpenRight");
        [Saved] bool _isClosed, _openLeft, _openRight;
        bool _useReceiverEvenIfDisabled;
        CloseAtTime _closeAtTime;
        Animator _animator;
        Vector3 _doorsForward;
        EventReference _openSound, _closeSound;
        TemplateReference _storyOnInteract;
        public override InfoFrame ActionFrame => new(_isClosed ? LocTerms.Open.Translate() : LocTerms.Close.Translate(), HeroHasRequiredItem());

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public DoorsAction() {}
    
        public DoorsAction(bool openByDefault) {
            _isClosed = !openByDefault;
        }

        public void InitFromAttachment(DoorsAttachment spec, bool isRestored) {
            _openSound = spec.openSound;
            _closeSound = spec.closeSound;
            _closeAtTime = spec.closeAtTime;
            _useReceiverEvenIfDisabled = spec.useReceiverEvenIfDisabled;
            _storyOnInteract = spec.storyOnInteract;
        }

        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(OnVisualLoaded);
        }

        void OnVisualLoaded(Transform parentTransform) {
            _animator = parentTransform.GetComponentInChildren<Animator>();
            _doorsForward = _animator.transform.forward;
            if (!_openLeft && !_openRight) {
                _openLeft = true;
            }
            
            var time = World.Only<GameRealTime>();
            if (_closeAtTime is CloseAtTime.CloseAtNightBegin or CloseAtTime.CloseAtNightChange) {
                time.ListenTo(GameRealTime.Events.DayBegan, Close, this);
            } else if (_closeAtTime is CloseAtTime.CloseAtNightEnd or CloseAtTime.CloseAtNightChange) {
                time.ListenTo(GameRealTime.Events.NightBegan, Close, this);
            }
            
            UpdateAnimator();
        }

        void Close() {
            _isClosed = true;
            _openRight = false;
            _openLeft = false;
            UpdateAnimator();
        }
    
        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            _isClosed = !_isClosed;
            if (!_isClosed) {
                DetermineOpenDirection(hero, interactable);
            } else {
                _openLeft = false;
                _openRight = false;
            }
            UpdateAnimator();
            PlayAudio(!_isClosed);
            ParentModel.TriggerChange();
            
            if (_storyOnInteract != null && StoryBookmark.ToInitialChapter(_storyOnInteract, out var story)) {
                Story.StartStory(StoryConfig.Base(story, null));
            }
        }

        void DetermineOpenDirection(Hero hero, IInteractableWithHero location) {
            if (hero == null) {
                _openLeft = true;
                _openRight = false;
                return;
            }
            Vector3 direction = hero.Coords - location.InteractionPosition;
            float dot = Vector3.Dot(direction, _doorsForward);
            _openLeft = dot < 0;
            _openRight = dot >= 0;
        }

        void UpdateAnimator() {
            _animator.SetBool(Closed, _isClosed);
            _animator.SetBool(OpenLeft, _openLeft);
            _animator.SetBool(OpenRight, _openRight);
        }

        void PlayAudio(bool isBeingOpen) {
            if (isBeingOpen && !_openSound.IsNull) {
                //RuntimeManager.PlayOneShotAttached(_openSound, _animator.gameObject, _animator);
            } else if (!isBeingOpen && !_closeSound.IsNull) {
                //RuntimeManager.PlayOneShotAttached(_closeSound, _animator.gameObject, _animator);
            }
        }
        
        public void OnLogicReceiverStateSetup(bool state) => OnLogicReceiverStateChanged(state);

        public void OnLogicReceiverStateChanged(bool state) {
            if (Disabled && !_useReceiverEvenIfDisabled) {
                return;
            }
            // if state: false than it should be closed (_isClosed)
            // if true, than open (!_isClosed)
            // so if state is equal to _isClosed, then it _isClosed should change
            if (state == _isClosed) {
                OnStart(Hero.Current, this.ParentModel);
            }
        }
    }
}