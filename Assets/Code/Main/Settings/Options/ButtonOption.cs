using System;
using Awaken.TG.Main.Settings.Options.Views;

namespace Awaken.TG.Main.Settings.Options {
    public class ButtonOption : PrefOption {
    
        public override Type ViewType => typeof(VButtonOption);
        public override bool WasChanged => false;
        
        public Action OnClick { get; private set; }
        
        public ButtonOption(string id, string displayName, Action onClick) : base(id, displayName, false) {
            OnClick = onClick;
        }

        public override void ForceChange() {}

        public override void Apply() {}

        public override void Cancel() {}

        public override void RestoreDefault() {}
    }
}