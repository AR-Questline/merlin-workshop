using System;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using DG.Tweening;

namespace Awaken.TG.Main.Tutorials.Utility {
    public static class TutorialEventUtils {
        public static void DiscardAfterTime(IModel target, float time) {
            DOTween.Sequence().SetDelay(time).AppendCallback(target.Discard);
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void DiscardAfterTime(IView target, float time) {
            DOTween.Sequence().SetDelay(time).AppendCallback(target.Discard);
        }

        [UnityEngine.Scripting.Preserve]
        public static void DiscardAfterHeroWalkDistance(View target, float distance, Action onDiscard = null) {
            var hero = Hero.Current;
            var position = Ground.CoordsToWorld(hero.Coords);
            var moved = 0f;

            void Callback() {
                var pos = Ground.CoordsToWorld(hero.Coords);
                moved += (pos - position).magnitude;
                if (moved >= distance) {
                    if (target != null) {
                        target.Discard();
                    }
                    onDiscard?.Invoke();
                }
                position = pos;
            }
            
            hero.ListenTo(GroundedEvents.AfterMoved, Callback, target);
        }
    }
}