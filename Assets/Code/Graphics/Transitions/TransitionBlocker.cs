using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using UnityEngine;

namespace Awaken.TG.Graphics.Transitions {
    public partial class TransitionBlocker : Model, IUIStateSource {
        public sealed override bool IsNotSaved => true;
        public override Domain DefaultDomain => Domain.Gameplay;
        public UIState UIState => UIState.BlockInput;
    }
}
