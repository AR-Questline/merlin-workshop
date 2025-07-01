using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Paths {
    public partial class LocationPath : Element<Location>, IRefreshedByAttachment<PathAttachment> {
        public override ushort TypeForSerialization => SavedModels.LocationPath;

        [Saved] public VertexPath Path { get; set; }

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        LocationPath() { }

        public LocationPath(VertexPath path) {
            Path = path;
        }

        public void InitFromAttachment(PathAttachment spec, bool isRestored) {
            Path = spec.Path;
        }
    }
}