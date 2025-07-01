using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Templates {
    public interface ITemplate : INamed {
        string GUID { get; set; }
        TemplateMetadata Metadata { get; }
        PooledList<IAttachmentSpec> DirectAttachments => PooledList<IAttachmentSpec>.Empty;
        PooledList<ITemplate> DirectAbstracts => PooledList<ITemplate>.Empty;
        bool IsAbstract => false;
        TemplateType TemplateType => TemplateType.Regular;
    }
}