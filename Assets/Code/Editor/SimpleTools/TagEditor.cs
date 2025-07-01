using System.Linq;
using Awaken.TG.Main.Heroes.Skills.Graphs;
using Awaken.TG.Main.Skills;
using Awaken.TG.Utility.Attributes.Tags;
using UnityEditor;

namespace Awaken.TG.Editor.SimpleTools {
    public class TagEditor : ScriptableWizard {
        [Tags(TagsCategory.Skill)] public string tag;

        [MenuItem("Tools/Tag Editor")]
        static void CreateWizard() {
            ScriptableWizard.DisplayWizard<TagEditor>("TagEditor", "Add");
        }

        void OnWizardCreate() {
            var skillTemplates = Selection.objects.Cast<SkillGraph>();
            foreach (var template in skillTemplates) {
                template.AddTags(new[] {tag});
            }
            AssetDatabase.SaveAssets();
        }
    }
}