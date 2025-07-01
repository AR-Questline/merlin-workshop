using System;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen {
    [SpawnsView(typeof(VTitlePopupUI))]
    public partial class TitlePopupUI : Model {
        public override Domain DefaultDomain => Domain.TitleScreen;
        public sealed override bool IsNotSaved => true;

        Action _onAccept;
        Action _onCancel;
        string _locTerm;
        public string MessageText => _locTerm.Translate();

        public TitlePopupUI(string locTerm, Action onAccept, Action onCancel = null) {
            _onAccept = onAccept;
            _onCancel = onCancel;
            _locTerm = locTerm;
        }

        public void AcceptClicked() {
            _onAccept();
            Discard();
        }

        public void CancelClicked() {
            _onCancel?.Invoke();
            Discard();
        }

        [UnityEngine.Scripting.Preserve]
        public void LinkClicked(string text) {
            Application.OpenURL(text);
        }
    }
}
