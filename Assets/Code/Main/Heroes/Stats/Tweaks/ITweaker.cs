using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Stats.Tweaks {
    public interface ITweaker {
        OperationType OperationType { get; }
        float TweakFn(float originalValue, Tweak tweak);
    }
}