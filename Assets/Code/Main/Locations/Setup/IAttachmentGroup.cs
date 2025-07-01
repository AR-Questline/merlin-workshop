using System.Collections.Generic;
using Awaken.TG.Main.Locations.Attachments;

namespace Awaken.TG.Main.Locations.Setup {
    public interface IAttachmentGroup {
        string AttachGroupId { get; }
        bool StartEnabled { get; }
        IEnumerable<IAttachmentSpec> GetAttachments();
    }
}