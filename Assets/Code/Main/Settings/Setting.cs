using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Settings {
    /// <summary>
    /// Base class for all settings in the game.
    /// Setting consists of pref-options, that are atomic, stored in prefs, values.
    /// </summary>
    public abstract partial class Setting : Element<SettingsMaster>, ISetting {
        protected const string NumberWithPercentFormat = "{0:P0}";
        public abstract string SettingName { get; }
        public virtual IEnumerable<PrefOption> Options => Array.Empty<PrefOption>();
        public virtual bool RequiresRestart => false;
        public virtual bool IsVisible => true;
        /// <summary>
        /// Should OnApply be called when the game starts.
        /// </summary>
        protected virtual bool AutoApplyOnInit => true;

        IEnumerable<PrefOption> SafeOptions => Options ?? Enumerable.Empty<PrefOption>();

        /// <summary>
        /// SettingChanged is triggered only when Setting was really changed and only then.
        /// SettingRefresh is triggered much more often.
        /// </summary>
        public new static class Events {
            public static readonly Event<Setting, Setting> SettingChanged = new(nameof(SettingChanged));
            public static readonly Event<Setting, Setting> SettingRefresh = new(nameof(SettingRefresh));
        }
        
        public void InitialApply() {
            foreach (var option in SafeOptions) {
                option.ForceChange();
            }

            Apply(AutoApplyOnInit, out _);
        }
        
        public void Apply(out bool needRestart) {
            Apply(false, out needRestart);
        }

        void Apply(bool initialApply, out bool needRestart) {
            bool wasChanged = false;
            foreach (var option in SafeOptions) {
                wasChanged = wasChanged || option.WasChanged;
                option.Apply();
            }

            if (wasChanged || initialApply) {
                OnApply();
            }

            needRestart = wasChanged && RequiresRestart;
            if (wasChanged) {
                this.Trigger(Events.SettingChanged, this);
                this.Trigger(Events.SettingRefresh, this);
            } else if (initialApply) {
                this.Trigger(Events.SettingRefresh, this);
            }
        }

        /// <summary>
        /// This method is called once when the game starts (if AutoApplyOnInit is true) and after that only when this exact setting was changed and applied by player. 
        /// </summary>
        protected virtual void OnApply() { }

        public void Cancel() {
            foreach (var option in SafeOptions) {
                if (option.WasChanged) {
                    option.Cancel();
                }
            }
        }

        public virtual void RestoreDefault() {
            foreach (var option in SafeOptions) {
                option.RestoreDefault();
            }
        }

        public virtual void PerformOnSceneChange() { }
    }
}