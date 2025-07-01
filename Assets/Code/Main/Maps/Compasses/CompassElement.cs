using Awaken.TG.Assets;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Maps.Compasses {
    public abstract partial class CompassElement : Element<Compass> {
        bool _ignoreAngleRequirementInternal;
        bool _ignoreAngleRequirementExternal;
        
        public VCompassElement TemporaryView { get; private set; }
        public bool IsDisplayed => TemporaryView != null;
        public bool IgnoreAngleRequirement => _ignoreAngleRequirementInternal | _ignoreAngleRequirementExternal;
        protected bool Enabled { get; private set; }

        public abstract bool ShouldBeDisplayed { get; }
        public abstract ShareableSpriteReference Icon { get; }
        public abstract string TopText { get; }
        public abstract int OrderNumber { get; }
        public abstract bool IsNumberVisible { get; }
        
        protected bool IgnoreDistanceRequirement { get; }

        protected CompassElement(bool enabled, bool ignoreDistanceRequirement, bool ignoreAngleRequirement) {
            Enabled = enabled;
            IgnoreDistanceRequirement = ignoreDistanceRequirement;
            _ignoreAngleRequirementExternal = ignoreAngleRequirement;
        }

        public new static class Events {
            public static readonly Event<CompassElement, bool> IconUpdated = new(nameof(IconUpdated));
            public static readonly Event<CompassElement, CompassElement> StateChanged = new(nameof(StateChanged));
        }

        public abstract Vector3 Direction(Vector3 from);
        public abstract AlphaValue CalculateAlpha(Vector3 observer);

        public void SetupView(VCompassElement vCompassElement) {
            vCompassElement.Setup(this);
            TemporaryView = vCompassElement;
            World.Services.Get<UnityUpdateProvider>().RegisterVCompassElement(vCompassElement);
        }
        
        public void CleanupView() {
            World.Services.Get<UnityUpdateProvider>().UnregisterVCompassElement(TemporaryView);
            TemporaryView.CleanUp();
            TemporaryView = null;
        }

        protected void SetAlwaysVisibleExternal(bool alwaysVisible) {
            _ignoreAngleRequirementExternal = alwaysVisible;
        }

        protected override void OnBeforeDiscard() {
            base.OnBeforeDiscard();
            ParentModel.TryReleaseCompassElementView(this);
        }

        public readonly struct AlphaValue {
            public readonly AlphaValueType type;
            public readonly float value;

            AlphaValue(AlphaValueType type, float value) {
                this.type = type;
                this.value = value;
            }
            
            public static AlphaValue FullyOpaque => new(AlphaValueType.FullyOpaque, 1);
            public static AlphaValue FullyTransparent => new(AlphaValueType.FullyTransparent, 0);
            public static AlphaValue Blended(float value) => new(AlphaValueType.Blended, value);
        }
        
        public enum AlphaValueType {
            FullyOpaque,
            FullyTransparent,
            Blended,
        }
    }
}