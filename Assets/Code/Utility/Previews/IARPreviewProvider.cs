using System.Collections.Generic;

namespace Awaken.Utility.Previews {
    public interface IARPreviewProvider {
        public IEnumerable<IARRendererPreview> GetPreviews();
    }
}