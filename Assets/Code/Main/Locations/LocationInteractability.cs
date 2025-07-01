using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Locations {
    public class LocationInteractability : RichEnum {
        [UnityEngine.Scripting.Preserve] public readonly bool visible;
        public readonly bool interactable;

        public static readonly LocationInteractability Hidden = new LocationInteractability(nameof(Hidden), false, false);
        public static readonly LocationInteractability Inactive = new LocationInteractability(nameof(Inactive), true, false);
        public static readonly LocationInteractability Active = new LocationInteractability(nameof(Active), true, true);

        protected LocationInteractability(string enumName, bool visible, bool interactable) : base(enumName) {
            this.visible = visible;
            this.interactable = interactable;
        }
    }
}