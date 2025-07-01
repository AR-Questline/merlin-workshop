using Awaken.TG.Main.Timing.ARTime;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD.Bars {
    public class GlowingBar : Bar {
        [SerializeField] bool disableNoGlowState;
        [SerializeField] float delay;
        [SerializeField] float dropGlowPercent = 0.025f;
        [SerializeField] float fillSpeed;
        [SerializeField] float glowSpeed;
        [SerializeField] float glowAppearingSpeed;
        [SerializeField] float glowDisappearingSpeed;
        [SerializeField, CanBeNull] RectTransform indicator;

        [SerializeField] Bar filled;
        [SerializeField] Bar glow;

        [SerializeField] bool unscaledTime;

        State _state;
        float _changeTime;
        
        DelayedValue _fillPercent;
        DelayedValue _glowPercent;
        float _glowAlpha;

        Hero _hero;
        Hero Hero => _hero ??= Hero.Current;
        bool IsStable => _fillPercent.IsStable && _glowPercent.IsStable;
        bool GlowHidden => _state is State.NoChange or State.ChangeNoGlow;
        float DeltaTime => unscaledTime ? Time.unscaledDeltaTime : Hero.GetDeltaTime();
        
        void Update() {
            if (_state == State.ForceMaxedOut) {
                UpdateGlowAlpha(1f, glowDisappearingSpeed);
                indicator.TrySetActiveOptimized(true);
            } else if (_state == State.NoChange) {
                UpdateGlowAlpha(0, glowDisappearingSpeed);
                indicator.TrySetActiveOptimized(false);
            } else if (_state == State.ChangeNoGlow) {
                UpdateGlowAlpha(0, glowDisappearingSpeed);
                indicator.TrySetActiveOptimized(false);
                _fillPercent.Update(DeltaTime, fillSpeed);
                if (disableNoGlowState) {
                    _glowPercent.Update(DeltaTime, glowSpeed);
                } else {
                    _glowPercent.SetInstant(_fillPercent.Value);
                }
            } else {
                UpdateGlowAlpha(1, glowAppearingSpeed);
                indicator.TrySetActiveOptimized(true);
                _fillPercent.Update(DeltaTime, fillSpeed);

                if (_state == State.GlowDelay) {
                    if (_changeTime <= 0) {
                        _state = State.ChangeGlow;
                    }
                    _changeTime -= DeltaTime;
                } 
                if (_state == State.ChangeGlow) {
                    _glowPercent.Update(DeltaTime, glowSpeed);
                }

                if (IsStable) {
                    _state = State.NoChange;
                }
            }
            
            filled.SetPercent(_fillPercent.Value);
            glow.SetPercent(_glowPercent.Value);
            glow.Color = glow.Color.WithAlpha(_glowAlpha);
        }

        void UpdateGlowAlpha(float target, float speed) {
            _glowAlpha = Mathf.MoveTowards(_glowAlpha, target, DeltaTime * speed);
        }

        public override void SetPercent(float percent) {
            if (_state == State.ForceMaxedOut) {
                return;
            }
            
            if (percent != _fillPercent.Target) {
                if (percent < _fillPercent.Value - dropGlowPercent) {
                    if (GlowHidden) {
                        _state = State.GlowDelay;
                        _changeTime = delay;
                    }
                } else if (GlowHidden) {
                    _state = State.ChangeNoGlow;
                }
            }

            _fillPercent.Set(percent);
            _glowPercent.Set(percent);
        }
        
        public void ForceMaxedOut(bool maxedOut) {
            if (maxedOut) {
                _state = State.ForceMaxedOut;
                _fillPercent.SetInstant(1);
                _glowPercent.SetInstant(1f);
            } else {
                _state = State.NoChange;
            }
        }

        public override void SetPercentInstant(float percent) {
            if (_state == State.ForceMaxedOut) {
                return;
            }
            
            _fillPercent.SetInstant(percent);
            _glowPercent.SetInstant(percent);
            _state = State.NoChange;
        }

        public override void SetPrediction(float percent) {
            filled.SetPrediction(percent);
        }

        public override Color Color {
            get => filled.Color;
            set => filled.Color = value;
        }

        enum State {
            NoChange,
            GlowDelay,
            ChangeGlow,
            ChangeNoGlow,
            ForceMaxedOut
        }
    }
}