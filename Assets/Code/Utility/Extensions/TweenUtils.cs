using DG.Tweening;

namespace Awaken.Utility.Extensions {
    public static class TweenUtils {
        public static void KillWithoutCallback(this Tween t) {
            if (t != null) {
                t.OnKill(null);
                t.Kill();
            }
        }
    }
}