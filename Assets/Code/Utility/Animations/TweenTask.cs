using Awaken.Utility.Animations;
using DG.Tweening;

namespace Awaken.TG.Utility.Animations
{
    public class TweenTask : IBackgroundTask {
        public bool Done { get; private set; } = false;

        public TweenTask(Tween tween) {
            tween.OnComplete(() => Done = true);
        }
    }
}
