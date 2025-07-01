using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Universal;
using Unity.Collections;

namespace Awaken.TG.Main.UI.Popup {
    /// <summary>
    /// Generic class for in-game context popups.
    /// </summary>
    [SpawnsView(typeof(VModalBlocker), isMainView = false)]
    [SpawnsView(typeof(VContextPopupUI))]
    public partial class ContextPopupUI : Model {
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

        // === Fields & Properties

        Model _owner;
        List<ContextPopupOption> _options;

        public IEnumerable<ContextPopupOption> Options => _options;

        // === Constructor

        ContextPopupUI(Model owner, IEnumerable<ContextPopupOption> options) {
            _owner = owner;
            _options = options.ToList();
            _options.Sort((a,b) => a.sortingOrder.CompareTo(b.sortingOrder));
        }

        [UnityEngine.Scripting.Preserve]
        public static ContextPopupUI CreatePopup(Model owner, IEnumerable<ContextPopupOption> options) {
            foreach (ContextPopupUI popupToDiscard in World.All<ContextPopupUI>().ToArraySlow()) {
                popupToDiscard.Discard();
            }

            ContextPopupUI popup = new ContextPopupUI(owner, options);
            World.Add(popup);
            return popup;
        }

        // === Initialization

        protected override void OnInitialize() {
            _owner.ListenTo(Events.BeforeDiscarded, Discard, this);
            this.ListenTo(VModalBlocker.Events.ModalDismissed, Discard, this);
        }

        // === Operations

        public void ChooseOption(ContextPopupOption option) {
            Discard();
            option.callback();
        }
    }
}
