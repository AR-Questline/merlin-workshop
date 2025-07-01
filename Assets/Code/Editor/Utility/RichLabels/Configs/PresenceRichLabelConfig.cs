using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Actors;

namespace Awaken.TG.Editor.Utility.RichLabels.Configs {
    public class PresenceRichLabelConfig : RichLabelConfig {
        const string ActorsCategoryGuid = "00000000-0000-0000-0000-000000000001";
        const string ScenesCategoryGuid = "00000000-0000-0000-0000-000000000002";
        
        public override IEnumerable<RichLabelCategory> GetPossibleCategories() {
            yield return new RichLabelCategory("Actor", guid: ActorsCategoryGuid, singleChoice: true, immutable: true, entries: Actors);
            yield return new RichLabelCategory("Scene", guid: ScenesCategoryGuid, singleChoice: true, immutable: true, entries: Scenes);
            foreach (var category in RichLabelCategories) {
                yield return category;
            }
        }
        
        static List<RichLabel> Actors => ActorsRegister.Get.AllActors.Select(a => new RichLabel(a.name, a.Guid)).ToList();
        static List<RichLabel> Scenes => CommonReferences.Get.SceneConfigs.AllScenes.Select(s => new RichLabel(s.sceneName, s.GUID)).ToList();
    }
}
