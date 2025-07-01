using System.Collections.Generic;

namespace Awaken.TG.Main.Stories.Tags {
    /// <summary>
    /// General interface for templates and specs that are tagged, which makes
    /// them usable for example in random pools.
    /// </summary>
    public interface ITagged {
        ICollection<string> Tags { get; }
    }
}
