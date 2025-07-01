using Awaken.TG.Main.Animations;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public readonly struct StoryInteractionToggleData {
        public bool IsEntering { get; }
        public bool InstantExit { get; }
        public SpineRotationType RotationType { get; }
        
        StoryInteractionToggleData(bool isEntering, bool instantExit, SpineRotationType rotationType) {
            IsEntering = isEntering;
            InstantExit = instantExit;
            RotationType = rotationType;
        }

        public static StoryInteractionToggleData Enter(SpineRotationType rotationType) => new(true, false, rotationType);
        public static StoryInteractionToggleData Exit(bool instant) => new(false, instant, SpineRotationType.FullRotation);
    }
}