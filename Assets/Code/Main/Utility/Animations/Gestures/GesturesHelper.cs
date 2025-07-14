using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Utility.Animations.Gestures {
    public static class GesturesHelper {
        static readonly int[] GesturesPreloaded = new int[2];
        static CommonReferences CommonReferences => World.Services.Get<CommonReferences>();
        
        public static GestureData? TryToGetDefaultGesture(string gestureKey, Gender gender) {
            return CommonReferences.GenderGestures.TryGetValue(gender, out GestureOverridesTemplate gestureOverrides)
                ? gestureOverrides.TryToGetAnimationClipRef(gestureKey)
                : null;
        }

        public static GestureOverrides GetGenderSpecificGestures(Gender gender) {
            return CommonReferences.GenderGestures.TryGetValue(gender, out GestureOverridesTemplate template)
                ? template.gestureOverrides
                : null;
        }

        public static void PreloadDefaultGestures() {
            for (int i = 0; i < GesturesPreloaded.Length; i++) {
                ref int preloadCounter = ref GesturesPreloaded[i];
                if (preloadCounter > 0) {
                    preloadCounter++;
                    continue;
                }
                
                var gestures = GetGenderSpecificGestures((Gender)i + 1);
                if (gestures == null) {
                    continue;
                }

                gestures.Preload();
                preloadCounter++;
            }
        }

        public static void ReleaseDefaultGestures() {
            for (int i = 0; i < GesturesPreloaded.Length; i++) {
                ref int preloadCounter = ref GesturesPreloaded[i];
                preloadCounter--;
                if (preloadCounter <= 0) {
                    GetGenderSpecificGestures((Gender)i + 1)?.Unload();
                    preloadCounter = 0;
                }
            }
        }
    }
}