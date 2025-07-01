using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Paths {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Adds path to the location.")]
    public class PathAttachment : MonoBehaviour, IAttachmentSpec {
        public VertexPathSpec pathSpec;

        public VertexPath Path => new VertexPath(pathSpec);
        
        public Element SpawnElement() {
            return new LocationPath(Path);
        }

        public bool IsMine(Element element) {
            return element is LocationPath;
        }
    }
}