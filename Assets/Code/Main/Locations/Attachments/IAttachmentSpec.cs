using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Attachments {
    /// <summary>
    /// Attachment specs are mono behaviors that create an element to be attached to some parent object,
    /// usually also created from a spec.
    /// For example, a WyrdlightAttachment added to a LocationSpec will spawn a configured Wyrdlight for a Location, which
    /// the Location will attach at initialization time.
    /// </summary>
    public interface IAttachmentSpec {
        Element SpawnElement();
        bool IsMine(Element element);
        bool IsValidOwner(IModel owner) => true;
    }
}
