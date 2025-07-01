namespace Awaken.TG.Assets {
    public enum AddressableGroup {
        [UnityEngine.Scripting.Preserve] Default,
        [UnityEngine.Scripting.Preserve] Skills,
        [UnityEngine.Scripting.Preserve] Clothes,
        [UnityEngine.Scripting.Preserve] Items,
        [UnityEngine.Scripting.Preserve] Weapons,
        [UnityEngine.Scripting.Preserve] Locations,
        [UnityEngine.Scripting.Preserve] IntroVideo,
        [UnityEngine.Scripting.Preserve] NPCs,
        [UnityEngine.Scripting.Preserve] Audio,
        [UnityEngine.Scripting.Preserve] UI,
        [UnityEngine.Scripting.Preserve] Tutorial,
        [UnityEngine.Scripting.Preserve] Glossary,
        [UnityEngine.Scripting.Preserve] LODs,
        [UnityEngine.Scripting.Preserve] AnimatorOverrides,
        [UnityEngine.Scripting.Preserve] DroppableItems,
        [UnityEngine.Scripting.Preserve] Stories,
        [UnityEngine.Scripting.Preserve] Scenes,
        [UnityEngine.Scripting.Preserve] ScenesEditor,
        [UnityEngine.Scripting.Preserve] Animations,
        [UnityEngine.Scripting.Preserve] Terrain,
        [UnityEngine.Scripting.Preserve] VFX,
        [UnityEngine.Scripting.Preserve] ItemsIcons,
        [UnityEngine.Scripting.Preserve] StatusEffects,
        [UnityEngine.Scripting.Preserve] EnemyBehaviours,
        [UnityEngine.Scripting.Preserve] UIPresenters,
        [UnityEngine.Scripting.Preserve] Plants,
        [UnityEngine.Scripting.Preserve] CampaignMapsTextures,
        [UnityEngine.Scripting.Preserve] DrakeRenderer,
        [UnityEngine.Scripting.Preserve] Quests,
        [UnityEngine.Scripting.Preserve] UniqueTexturesArtbook,
    }

    public static class AddressableGroupExtensions {
        public static string NameOf(this AddressableGroup group) {
            if (group == AddressableGroup.Default) {
                return null;
            } else {
                return group.ToString();
            }
        }
    }
}