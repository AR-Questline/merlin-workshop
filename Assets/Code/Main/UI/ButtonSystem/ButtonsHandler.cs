using System;
using System.Collections.Generic;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.Utility;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.UI.ButtonSystem {
    public class ButtonsHandler {
        public const float HoldTime = 0.37f;
        const float TapTime = 0.2f;
        const int TapFrames = 3;
        
        float _holdStartTime;
        int _holdStartFrame;
        IModel _heldButton;

        public UIResult HandleMouse(IButton button, UIMouseButtonEvent mouseButtonEvent) {
            return button.ButtonPressType switch {
                IButton.PressType.Tap => HandleTap(button, mouseButtonEvent),
                IButton.PressType.Hold => HandleHold(button, mouseButtonEvent),
                _ => UIResult.Ignore
            };
        }

        public UIResult Handle(ModelsSet<Prompt> prompts, UIKeyAction action) {
            if (_heldButton is { HasBeenDiscarded: true }) {
                _heldButton = null;
            }
            
            if (_heldButton is IButton button && !button.Accept(action)) {
                if (button.ActionMatches(action)) {
                    _heldButton = null;
                    _holdStartTime = Time.unscaledTime;
                    _holdStartFrame = Time.frameCount;
                    button.OnHoldInterrupted();
                }
                return UIResult.Ignore;
            }

            IButton button0 = null, button1 = null;
            
            foreach (var prompt in prompts) {
                if (prompt.Accept(action)) {
                    if (button0 == null) {
                        button0 = prompt;
                    } else if (button1 == null) {
                        button1 = prompt;
                    } else {
                        throw new ButtonConflict(button0, button1, prompt);
                    }
                }
            }
            
            return (button0, button1) switch {
                (null, _) => UIResult.Ignore,
                ({ Tap: true }  tap,  null) => HandleTap(tap, action),
                ({ Hold: true } hold, null) => HandleHold(hold, action),
                ({ Tap: true }  tap,  { Hold: true } hold) => HandleTapAndHold(tap, hold, action),
                ({ Hold: true } hold, { Tap: true } tap) => HandleTapAndHold(tap, hold, action),
                _ => throw new ButtonConflict(button0, button1)
            };
        }

        public UIResult HandleTap([NotNull] IButtonTap prompt, UIEvent action) {
            if (_heldButton == null && action is UIKeyDownAction or UIEMouseDown) {
                prompt.OnTap();
                prompt.Invoke();
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }

        public UIResult HandleHold([NotNull] IButtonHold prompt, UIEvent action) {
            if (action is UIKeyDownAction or UIEMouseDown) {
                if (_heldButton == null) {
                    _heldButton = prompt;
                    _holdStartTime = Time.unscaledTime;
                    _holdStartFrame = Time.frameCount;
                    prompt.OnKeyDown();
                    return UIResult.Accept;
                }
            } else if (action is UIKeyHeldAction or UIEMouseHeld) {
                if (_heldButton == prompt) {
                    float holdTime = Time.unscaledTime - _holdStartTime;
                    if (holdTime <= prompt.HoldTime) {
                        prompt.OnKeyHeld(holdTime / prompt.HoldTime);
                    } else {
                        prompt.OnKeyUp(true);
                        prompt.Invoke();
                        _heldButton = null;
                    }
                    return UIResult.Accept;
                }
            } else if (action is UIKeyUpAction or UIEMouseUp) {
                if (_heldButton == prompt) {
                    float holdTime = Time.unscaledTime - _holdStartTime;
                    if (holdTime < prompt.HoldTime) {
                        prompt.OnKeyUp();
                    } else {
                        prompt.OnKeyUp(completed: true);
                        prompt.Invoke();
                    }
                    _heldButton = null;
                    return UIResult.Accept;
                }
            }
            
            return UIResult.Ignore;
        }

        public UIResult HandleTapAndHold([NotNull] IButtonTap tap, [NotNull] IButtonHold hold, UIKeyAction action) {
            if (action is UIKeyDownAction) {
                if (_heldButton == null) {
                    _heldButton = hold;
                    _holdStartTime = Time.unscaledTime;
                    _holdStartFrame = Time.frameCount;
                    hold.OnKeyDown();
                    return UIResult.Accept;
                }
            } else if (action is UIKeyHeldAction) {
                if (_heldButton == hold) {
                    float holdTime = Time.unscaledTime - _holdStartTime;
                    if (holdTime <= hold.HoldTime) {
                        hold.OnKeyHeld((holdTime / hold.HoldTime).Remap(TapTime, 1, 0, 1, true));
                    } else {
                        hold.OnKeyUp();
                        hold.Invoke();
                        _heldButton = null;
                    }
                    return UIResult.Accept;
                }
            } else if (action is UIKeyUpAction) {
                if (_heldButton == hold) {
                    float holdTime = Time.unscaledTime - _holdStartTime;
                    int tapFrames = Time.frameCount - _holdStartFrame;
                    
                    if (holdTime < TapTime || tapFrames < TapFrames) {
                        tap.OnTap();
                        tap.Invoke();
                    } 
                    
                    if (holdTime < hold.HoldTime) {
                        hold.OnKeyUp();
                    } else {
                        hold.OnKeyUp();
                        hold.Invoke();
                    }
                    
                    _heldButton = null;
                    return UIResult.Accept;
                }
            }
            
            return UIResult.Ignore;
        }
        
        class ButtonConflict : Exception {
            public ButtonConflict(params IButton[] prompts) : base(Message(prompts)) { }

            new static string Message(IEnumerable<IButton> prompts) {
                return $"There is unresolvable prompt conflict between: {string.Join(", ", prompts)}";
            }
        }
    }
}