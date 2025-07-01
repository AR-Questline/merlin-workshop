using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Sets faction for the location.")]
    public class FactionProviderAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] FactionTemplate factionTemplate;
        public FactionTemplate FactionTemplate => factionTemplate;
        
        public Element SpawnElement() => new SimpleFactionProvider();

        public bool IsMine(Element element) {
            return element is SimpleFactionProvider simpleFactionProvider && simpleFactionProvider.Faction.Template == factionTemplate;
        }
    }
}