using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.MVC.UI.Handlers.Tooltips {
    /// <summary>
    /// Represents a tooltip.
    /// </summary>
    public partial class Tooltip : Model, ITooltip {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;

        // === State
        bool _visible;
        
        public ITooltip Parent { get; }
        public bool MoveWithMouse => !RewiredHelper.IsGamepad;
        public TooltipConstructor Constructor { get; }
        public Vector2 ScreenTargetPosition => World.Only<GameUI>().MousePosition.screen;
        public Vector2 TargetPosition => Constructor.StaticPositioning?.position ?? ScreenTargetPosition;
        bool _focusChanged;

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
        public float Scale => Constructor.StaticPositioning?.scale ?? 1f;
        public bool Visible => View<VTooltip>() != null;

        Tooltip ChildTooltip => World.Any<Tooltip>(t => t.Parent == this); 

        // === Constructors
        Tooltip(TooltipConstructor constructor, Tooltip parent = null) {
            Constructor = new TooltipConstructor(constructor);
            Parent = parent;
        }

        protected override void OnInitialize() {
            Parent?.ListenTo(Events.BeforeDiscarded, Discard, this);
            World.Only<Focus>().ListenTo(Focus.Events.FocusChanged, () => _focusChanged = true);
        }

        protected override void OnFullyInitialized() {
            AsyncOnFullyInitialized().Forget();
        }

        async UniTaskVoid AsyncOnFullyInitialized() {
            if (Constructor.WithDelay) {
                int delay = (int)(Services.Get<GameConstants>().tooltipDelay * 1000f);
                await UniTask.Delay(delay);
                if (!WasDiscarded) {
                    SpawnVTooltip();
                }
            } else {
                SpawnVTooltip();
            }
        }

        void SpawnVTooltip() {
            World.SpawnView<VTooltip>(this, true);
            ShowSubTooltip();
        }

        public void ShowSubTooltip() {
            if (Constructor.SubTooltip != null) {
                World.Add(new Tooltip(Constructor.SubTooltip, this));
            }
        }

        [UnityEngine.Scripting.Preserve]
        public void HideSubTooltip() {
            ChildTooltip?.Discard();
        }

        // === Spawning/despawning the single instance
        public static void ShowTooltip(TooltipConstructor tooltipConstructor) {
            Tooltip existing = AnyMainTooltip();
            if (existing != null) {
                if (existing.Constructor == tooltipConstructor && !existing._focusChanged) {
                    return;
                }
                existing.Discard();
            }
            World.Add(new Tooltip(tooltipConstructor));
        }

        public static void HideTooltip() {
            Tooltip existing = AnyMainTooltip();
            existing?.Discard();
        }

        static Tooltip AnyMainTooltip() {
            return World.Any<Tooltip>(t => t.Parent == null);
        }
    }
}
