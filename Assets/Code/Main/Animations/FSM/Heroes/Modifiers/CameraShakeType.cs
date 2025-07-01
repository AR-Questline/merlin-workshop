using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Modifiers {
    public class CameraShakeType : RichEnum {
        public HeroStateType AnimatorStateType { get; }

        protected CameraShakeType(string enumName, HeroStateType animatorStateType, string inspectorCategory = "") : base(enumName, inspectorCategory) {
            AnimatorStateType = animatorStateType;
        }

        [UnityEngine.Scripting.Preserve]
        public static readonly CameraShakeType
            LiteHorizontal = new(nameof(LiteHorizontal), HeroStateType.ShakeLight),
            LiteVertical = new(nameof(LiteVertical), HeroStateType.ShakeLight),
            LiteAllDirection = new(nameof(LiteAllDirection), HeroStateType.ShakeLight),
            MediumHorizontal = new(nameof(MediumHorizontal), HeroStateType.ShakeMedium),
            MediumVertical = new(nameof(MediumVertical), HeroStateType.ShakeMedium),
            MediumAllDirection = new(nameof(MediumAllDirection), HeroStateType.ShakeMedium),
            StrongHorizontal = new(nameof(StrongHorizontal), HeroStateType.ShakeStrong),
            StrongVertical = new(nameof(StrongVertical), HeroStateType.ShakeStrong),
            StrongAllDirection = new(nameof(StrongAllDirection), HeroStateType.ShakeStrong);
    }
}