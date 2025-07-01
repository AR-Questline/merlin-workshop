using System;
using Awaken.TG.Main.Settings.Options.Views;

namespace Awaken.TG.Main.Settings.Options {
    /// <summary>
    /// Not a real option, used to display not-an-option in settings panel
    /// </summary>
    public class LinkOption : PrefOption {
        public override Type ViewType => typeof(VLinkOption);
        public override bool WasChanged => false;
        
        public Action Callback { get; }

        public LinkOption(string displayName, Action callback) : base("", displayName, false) {
            Callback = callback;
        }

        public override void ForceChange() {
        }

        public override void Apply() {
        }

        public override void Cancel() {
        }

        public override void RestoreDefault() {
        }
    }
}