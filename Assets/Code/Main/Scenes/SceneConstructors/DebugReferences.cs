using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Scenes.SceneConstructors {
    public class DebugReferences : MonoBehaviour, IService {

        // === Fields
        [FoldoutGroup("DebugTemplates")] [SerializeField, TemplateType(typeof(HeroTemplate))]
        TemplateReference heroTemplate;

        [FoldoutGroup("Cheats")] public string cheatCode;

        // === Properties
        public HeroTemplate HeroClass => HeroTemplateOverride?.Get<HeroTemplate>() ?? heroTemplate.Get<HeroTemplate>();
        public TemplateReference HeroTemplateOverride { get; set; }

        public DebugReferences Init() {
            ImmediateStory = Configuration.GetBool("immediate_story");
            return this;
        }

        // === Helpers

#if UNITY_EDITOR
        public static bool DebugStoryStart => UnityEditor.EditorPrefs.GetBool("debug.story.start", false);
        public static bool FastStory => UnityEditor.EditorPrefs.GetBool("fast.story", false);
        public static bool FastStart => UnityEditor.EditorPrefs.GetBool("fast.start", false);
        public static bool FastNotifications => UnityEditor.EditorPrefs.GetBool("fast.notifications", false);
        public static bool DisableCuanachtCutscene => UnityEditor.EditorPrefs.GetBool("cuanacht.cutscene.disable", false);
        public static bool SkillMachinesOnSeparateObjects => UnityEditor.EditorPrefs.GetBool("skill.machines.separate.objects", false);

        public static bool LogMovementUnsafeOverrides => UnityEditor.EditorPrefs.GetBool("log.movement.unsafe.changes", false);

        public static bool LogAnimancerFallbackState => UnityEditor.EditorPrefs.GetBool("log.animancer.fallback.state", false);
#else
        public static bool DebugStoryStart => false;
        public static bool FastStory => false;
        public static bool FastStart => false;
        public static bool FastNotifications => false;
        public static bool DisableCuanachtCutscene => false;
        public static bool SkillMachinesOnSeparateObjects => false;
        public static bool LogMovementUnsafeOverrides => false;
        public static bool LogAnimancerFallbackState => false;
#endif

        public static bool ImmediateStory { get; set; }

        public static DebugReferences Get {
            get {
                DebugReferences references = World.Services?.TryGet<DebugReferences>();
#if UNITY_EDITOR
                if (references == null) {
                    references = UnityEditor.AssetDatabase
                        .LoadAssetAtPath<GameObject>("Assets/Data/Settings/DebugReferences.prefab")
                        .GetComponent<DebugReferences>();
                }
#endif
                return references;
            }
        }
    }
}
