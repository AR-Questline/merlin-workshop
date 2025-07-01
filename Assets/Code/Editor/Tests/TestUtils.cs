using System.Collections;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;

namespace Awaken.TG.Code.Editor.Tests{
    public static class TestUtils {
        const int AfterChangeFramesWait = 5;
        public static IEnumerator WaitForFrames(int count) {
            for (int i = 0; i < count; i++) {
                yield return null;
            }
        }
        public static IEnumerator WaitForDiscard(Model model) {
            while (!model.WasDiscarded) {
                yield return null;
            }
        }
        public static IEnumerator WaitForWorld() {
            while (World.Services == null) {
                yield return null;
            }
        }

        public static IEnumerator WaitForHero() {
            while (Hero.Current == null) {
                yield return null;
            }
        }
        
        public static IEnumerator WaitAfterChange(int? framesToWait = null) {
            yield return WaitForFrames(framesToWait ?? AfterChangeFramesWait);
        }
    }
}
