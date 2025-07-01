using System;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.UI.UITooltips;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using UnityEngine;
using static Awaken.TG.Main.UI.UITooltips.TextLinkHandler;

namespace Awaken.TG.Main.UI.TitleScreen.PatchNotes {
    [SpawnsView(typeof(VPatchNotesUI))]
    public partial class PatchNotesUI : Model {
        public override Domain DefaultDomain => Domain.TitleScreen;
        public sealed override bool IsNotSaved => true;

        public PatchNote Note { get; private set; }

        public PatchNotesUI(PatchNote note) {
            Note = note;
        }

        protected override void OnInitialize() {
            this.ListenTo(TextLinkHandler.Events.LinkClicked, OnLinkClicked, this);
        }

        void OnLinkClicked(Link link) {
            string url = link.data;
            bool isUrl = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (isUrl) {
                Application.OpenURL(url);
            }
        }
    }
}