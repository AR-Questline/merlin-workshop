using System;
using Awaken.TG.Main.Settings.FirstTime;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Settings.Options {
    /// <summary>
    /// Base class for options. This class hierarchy takes care of managing a simple value (like float, bool).
    /// It takes care of storing it in Prefs, applying, cancelling. 
    /// </summary>
    public abstract class PrefOption {
        Func<bool> _isInteractable = static () => true;
        Func<string> _tooltipConstructor;
        public abstract Type ViewType { get; }
        
        public Func<string> TooltipConstructor => World.Any<FirstTimeSettings>() ? null : _tooltipConstructor;
        public string ID { get; }
        public string DisplayName { get; }
        public bool Synchronize { get; }
        public bool Interactable => _isInteractable();

        protected string PrefKey => ID;

        protected PrefOption(string id, string displayName, bool synchronize) {
            ID = id;
            DisplayName = displayName;
            Synchronize = synchronize;
        }

        public abstract void ForceChange();
        public abstract void Apply();
        public abstract void Cancel();
        public abstract void RestoreDefault();
        public abstract bool WasChanged { get; }

        public void AddTooltip(Func<string> constructor) {
            _tooltipConstructor = constructor;
        }

        public void SetInteractabilityFunction(Func<bool> interactabilityFunction) {
            _isInteractable = interactabilityFunction;
        }
    }
}