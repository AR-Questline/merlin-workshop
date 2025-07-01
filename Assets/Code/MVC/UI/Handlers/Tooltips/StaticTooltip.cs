using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.MVC.UI.Handlers.Tooltips {
    /// <summary>
    /// Represents a static tooltip.
    /// </summary>
    [SpawnsView(typeof(VTooltip))]
    [UnityEngine.Scripting.Preserve]
    public partial class StaticTooltip : Model, ITooltip {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        // === State
        public bool MoveWithMouse => false;

        public Vector2 TargetPosition => ScreenTargetPosition;

        public Vector2 TargetPivot {
            get {
                if (Constructor.StaticPositioning != null) {
                    return Constructor.StaticPositioning.pivot;
                }
                // calculate pivots to fit on the screen
                float xPivot = 0f;
                float yPivot = 1f;
            
                float xInputOnScreen = ScreenTargetPosition.x / Screen.width;
                float yInputOnScreen = ScreenTargetPosition.y / Screen.height;

                if (xInputOnScreen > 0.8f) {
                    xPivot = 1f;
                }

                if (yInputOnScreen < 0.2f) {
                    yPivot = 0f;
                }

                return new Vector2(xPivot, yPivot);
            }
        }

        public float Scale => 1f;

        public TooltipConstructor Constructor { get; set; }

        public ITooltip Parent => _parent;

        Vector2 ScreenTargetPosition => Constructor.StaticPositioning?.position ??
                                        _parent?.View<VTooltip>()?.subTooltipParent.position ??
                                        World.Only<GameUI>().MousePosition.screen;

        StaticTooltip _parent;

        public bool Visible => true;
        
        // === Constructors

        public StaticTooltip(TooltipConstructor constructor, StaticTooltip parent = null) {
            Constructor = constructor;
            _parent = parent;
        }

        protected override void OnInitialize() {
            _parent?.ListenTo(Events.BeforeDiscarded, Discard, this);
        }
    }
}