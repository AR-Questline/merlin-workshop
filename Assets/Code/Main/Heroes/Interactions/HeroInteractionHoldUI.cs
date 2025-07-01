using System;
using Awaken.TG.Main.Locations.Actions.Lockpicking;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Interactions {
    [SpawnsView(typeof(VHeroInteractionUI))]
    public partial class HeroInteractionHoldUI : HeroInteractionUI, IUniqueKeyProvider {
        public override KeyIcon.Data UniqueKey => new(KeyBindings.Gameplay.Interact, true);
        
        public virtual float HoldTime { get; }
        public bool FancyHoldGraphic { get; }

        public float HoldPercent {
            [UnityEngine.Scripting.Preserve] get => _holdPercent;
            private set {
                _holdPercent = value;
                foreach (KeyIcon icon in _registerKeyIcons) {
                    if (icon != null) {
                        icon.SetHoldPercent(value);
                    }
                }
                
                if (FancyHoldGraphic && _holdSequence != null) {
                    _holdSequence.SetHoldPercent(value);
                }
            }
        }

        public virtual bool HeldButton {
            get => _heldButton;
            set {
                _heldButton = value;
            }
        }

        Action<Hero> _onHoldAction;
        float _holdStartTime;
        protected bool _heldButton;
        float _holdPercent;
        readonly KeyIcon[] _registerKeyIcons = new KeyIcon[4];
        VInteractionHoldSequence _holdSequence;

        [UnityEngine.Scripting.Preserve]
        public HeroInteractionHoldUI(IInteractableWithHero interactable, float holdTime, Action<Hero> onHoldAction = null) : base(interactable) {
            HoldTime = holdTime;
            _onHoldAction = onHoldAction;
        }
        
        public HeroInteractionHoldUI(IInteractableWithHero interactable, float holdTime, bool fancyHoldGraphic, Action<Hero> onHoldAction = null) : base(interactable) {
            HoldTime = holdTime;
            _onHoldAction = onHoldAction;
            FancyHoldGraphic = fancyHoldGraphic;
        }

        protected HeroInteractionHoldUI(IInteractableWithHero interactable) : base(interactable) { }

        protected override void OnFullyInitialized() {
            if (FancyHoldGraphic) {
                _holdSequence = World.SpawnView<VInteractionHoldSequence>(this, forcedParent: MainView.transform);
            }
        }

        void IUniqueKeyProvider.RegisterForHold(KeyIcon keyIcon) {
            for (int i = 0; i < _registerKeyIcons.Length; i++) {
                if (_registerKeyIcons[i] == null) {
                    _registerKeyIcons[i] = keyIcon;
                    return;
                }
            }
            Log.Important?.Error("Not supported extreme case, increase " + nameof(_registerKeyIcons) + " arraySize to accomodate");
        }

        public override UIResult Handle(UIEvent action) {
            // Interaction begun
            if (action is UIKeyDownAction) {
                // Skip hold when toolbox overrides are active
                if (ParentModel.HasElement<ToolboxOverridesMarker>()) {
                    if (HeroInteraction.StartInteraction(ParentModel, Interactable, out var newAction)) {
                        EndAction();
                        _usedAction = newAction;
                        RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
                        return UIResult.Accept;
                    }
                }
                // Start hold
                if (!HeldButton) {
                    HeldButton = true;
                    _holdStartTime = Time.unscaledTime;
                    return UIResult.Accept;
                }
                return UIResult.Ignore;
            }

            // Interaction in progress check for hold duration
            if (action is UIKeyHeldAction) {
                if (HeldButton) {
                    float holdTime = Time.unscaledTime - _holdStartTime;
                    if (holdTime <= HoldTime) {
                        HoldPercent = holdTime / HoldTime;
                        RewiredHelper.VibrateLowFreq(VibrationStrength.VeryLow, VibrationDuration.Continuous);
                        _onHoldAction?.Invoke(ParentModel);
                    } else if (HeroInteraction.StartInteraction(ParentModel, Interactable, out var newAction)) {
                        // Hold complete: Interaction was performed
                        EndAction();
                        _usedAction = newAction;
                        HeldButton = false;
                        RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
                    }
                    return UIResult.Accept;
                }
                return UIResult.Ignore;
            }

            // Interaction ended
            if (action is UIKeyUpAction) {
                if (HeldButton) {
                    float holdTime = Time.unscaledTime - _holdStartTime;
                    if (holdTime < HoldTime) {
                        if (HasAction) {
                            EndAction();
                        }
                    } else {
                        // Interaction was performed
                        HeroInteraction.StartInteraction(ParentModel, Interactable, out var newAction);
                        EndAction();
                        _usedAction = newAction;
                        RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
                    }
                        
                    // Reset state
                    HoldPercent = 0;
                    HeldButton = false;
                    return UIResult.Accept;
                }
            }

            return UIResult.Ignore;
        }
    }
}